using System.Drawing;
using System.Numerics;
using ModelExplorerLibrary.Matrix4x4;
using ModelExplorerLibrary.Models;
using System.Drawing.Imaging;

namespace ModelExplorerLibrary.Render
{
    public class Render
    {
        private int _bgColor = Color.White.ToArgb();
        private Vector3 _lightDir = Vector3.Normalize(new Vector3(-0.5f, 0.5f, 1.0f));

        //Lambert
        private unsafe void DrawHorizontalLineLambert(int* pBase, float[] zBuffer, int width, int height, int stride, int x1, int x2, int y, float z1, float z2, int color)
        {
            if (y < 0 || y >= height) return;
            if (x1 > x2) { (x1, x2) = (x2, x1); (z1, z2) = (z2, z1); }

            int startX = Math.Max(0, x1);
            int endX = Math.Min(width - 1, x2);

            for (int x = startX; x <= endX; x++)
            {
                float t = (x1 == x2) ? 0 : (float)(x - x1) / (x2 - x1);
                float currentZ = z1 + (z2 - z1) * t;

                int idx = y * width + x;

                if (currentZ < zBuffer[idx])
                {
                    zBuffer[idx] = currentZ;
                    int offset = y * (stride / 4) + x;
                    *(pBase + offset) = color;
                }
            }
        }

        private unsafe void FillTriangleScanlineLambert(int* pBase, float[] zBuffer, int width, int height, int stride, Vector4 v0, Vector4 v1, Vector4 v2, int color)
        {
            if (v1.Y < v0.Y) (v0, v1) = (v1, v0);
            if (v2.Y < v0.Y) (v0, v2) = (v2, v0);
            if (v2.Y < v1.Y) (v1, v2) = (v2, v1);

            float totalHeight = v2.Y - v0.Y;
            if (totalHeight == 0) return;

            for (int y = (int)Math.Ceiling(v0.Y); y <= (int)Math.Floor(v2.Y); y++)
            {
                bool secondHalf = y > v1.Y || v1.Y == v0.Y;
                float segmentHeight = secondHalf ? v2.Y - v1.Y : v1.Y - v0.Y;
                if (segmentHeight == 0) continue;

                float alpha = (y - v0.Y) / totalHeight;
                float beta = secondHalf ? (y - v1.Y) / (v2.Y - v1.Y) : (y - v0.Y) / (v1.Y - v0.Y);

                Vector4 A = v0 + (v2 - v0) * alpha;
                Vector4 B = secondHalf ? v1 + (v2 - v1) * beta : v0 + (v1 - v0) * beta;

                DrawHorizontalLineLambert(pBase, zBuffer, width, height, stride, (int)Math.Round(A.X), (int)Math.Round(B.X), y, A.Z, B.Z, color);
            }
        }

        //Phong
        struct VertexData
        {
            public Vector4 ScreenPos;
            public Vector3 WorldPos;
            public Vector3 Normal;
        }

        private Vector3[] ComputeVertexNormals(ModelClass model)
        {
            Vector3[] normals = new Vector3[model.Vertices.Count];
            foreach (var face in model.Faces)
            {
                Vector3 v0 = model.Vertices[face.VertexIndices[0]];
                Vector3 v1 = model.Vertices[face.VertexIndices[1]];
                Vector3 v2 = model.Vertices[face.VertexIndices[2]];

                Vector3 faceNormal = Vector3.Normalize(Vector3.Cross(v2 - v0, v1 - v0));

                normals[face.VertexIndices[0]] += faceNormal;
                normals[face.VertexIndices[1]] += faceNormal;
                normals[face.VertexIndices[2]] += faceNormal;
            }

            for (int i = 0; i < normals.Length; i++)
                normals[i] = Vector3.Normalize(normals[i]);

            return normals;
        }

        private VertexData InterpolateVertex(VertexData v1, VertexData v2, float t)
        {
            return new VertexData
            {
                ScreenPos = v1.ScreenPos + (v2.ScreenPos - v1.ScreenPos) * t,
                WorldPos = Vector3.Lerp(v1.WorldPos, v2.WorldPos, t),
                Normal = Vector3.Normalize(Vector3.Lerp(v1.Normal, v2.Normal, t))
            };
        }

        private unsafe void FillTriangleScanlinePhong(int* pBase, float[] zBuffer, int width, int height, int stride, VertexData v0, VertexData v1, VertexData v2, Vector3 cameraPos, SettingsClass settings)
        {
            if (v1.ScreenPos.Y < v0.ScreenPos.Y) (v0, v1) = (v1, v0);
            if (v2.ScreenPos.Y < v0.ScreenPos.Y) (v0, v2) = (v2, v0);
            if (v2.ScreenPos.Y < v1.ScreenPos.Y) (v1, v2) = (v2, v1);

            float totalHeight = v2.ScreenPos.Y - v0.ScreenPos.Y;
            if (totalHeight == 0) return;

            for (int y = (int)Math.Ceiling(v0.ScreenPos.Y); y <= (int)Math.Floor(v2.ScreenPos.Y); y++)
            {
                bool secondHalf = y > v1.ScreenPos.Y || v1.ScreenPos.Y == v0.ScreenPos.Y;
                float segmentHeight = secondHalf ? v2.ScreenPos.Y - v1.ScreenPos.Y : v1.ScreenPos.Y - v0.ScreenPos.Y;
                if (segmentHeight == 0) continue;

                float alpha = (y - v0.ScreenPos.Y) / totalHeight;
                float beta = secondHalf ? (y - v1.ScreenPos.Y) / (v2.ScreenPos.Y - v1.ScreenPos.Y) : (y - v0.ScreenPos.Y) / (v1.ScreenPos.Y - v0.ScreenPos.Y);

                VertexData A = InterpolateVertex(v0, v2, alpha);
                VertexData B = secondHalf ? InterpolateVertex(v1, v2, beta) : InterpolateVertex(v0, v1, beta);

                DrawHorizontalLinePhong(pBase, zBuffer, width, height, stride, A, B, y, cameraPos, settings);
            }
        }

        private unsafe void DrawHorizontalLinePhong(int* pBase, float[] zBuffer, int width, int height, int stride, VertexData a, VertexData b, int y, Vector3 cameraPos, SettingsClass settings)
        {
            if (y < 0 || y >= height) return;
            if (a.ScreenPos.X > b.ScreenPos.X) (a, b) = (b, a);

            int startX = (int)Math.Max(0, Math.Ceiling(a.ScreenPos.X));
            int endX = (int)Math.Min(width - 1, Math.Floor(b.ScreenPos.X));

            for (int x = startX; x <= endX; x++)
            {
                float t = (Math.Abs(b.ScreenPos.X - a.ScreenPos.X) < 1e-6) ? 0 : (x - a.ScreenPos.X) / (b.ScreenPos.X - a.ScreenPos.X);
                float currentZ = a.ScreenPos.Z + (b.ScreenPos.Z - a.ScreenPos.Z) * t;

                int idx = y * width + x;
                if (currentZ < zBuffer[idx])
                {
                    zBuffer[idx] = currentZ;

                    Vector3 pixelNormal = Vector3.Normalize(Vector3.Lerp(a.Normal, b.Normal, t));
                    Vector3 pixelWorldPos = Vector3.Lerp(a.WorldPos, b.WorldPos, t);

                    Vector3 N = Vector3.Normalize(pixelNormal);
                    Vector3 V = Vector3.Normalize(-cameraPos + pixelWorldPos);
                    Vector3 L = Vector3.Normalize(-_lightDir);
                    Vector3 R = Vector3.Reflect(_lightDir, N);

                    Vector3 ambient = settings.AmbientColor;

                    float dotNL = Vector3.Dot(N, L);
                    float diff = Math.Max(dotNL, 0.0f);
                    Vector3 diffuse = diff * new Vector3(0.8f, 0.8f, 0.8f);

                    float dotRV = Vector3.Dot(R, V);
                    float spec = MathF.Pow(Math.Max(dotRV, 0.0f), settings.Shininess);
                    Vector3 specular = settings.SpecularStrength * spec * Vector3.One;

                    Vector3 finalColor = ambient + diffuse + specular;

                    int r = (int)(Math.Min(1.0f, finalColor.X) * 255);
                    int g = (int)(Math.Min(1.0f, finalColor.Y) * 255);
                    int b_col = (int)(Math.Min(1.0f, finalColor.Z) * 255);

                    int color = (255 << 24) | (r << 16) | (g << 8) | b_col;
                    *(pBase + y * (stride / 4) + x) = color;
                }
            }
        }

        private Vector3 TransformNormal(Vector3 normal, Matrix m)
        {
            Vector4 res = m * new Vector4(normal, 0);
            return Vector3.Normalize(new Vector3(res.X, res.Y, res.Z));
        }

        //Render
        public unsafe void RenderModel(Bitmap bmp, ModelClass model, SettingsClass settings, CameraClass camera, bool isPhong = true)
        {
            int width = bmp.Width;
            int height = bmp.Height;
            BitmapData data = bmp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format32bppPArgb);

            try
            {
                int* pBase = (int*)data.Scan0;
                int stride = data.Stride;
                float[] zBuffer = new float[width * height];
                Array.Fill(zBuffer, float.MaxValue);
                for (int i = 0; i < width * height; i++) pBase[i] = _bgColor;

                Matrix modelMatrix = CoordinateSystems.ToWorld(settings);
                Matrix viewMatrix = CoordinateSystems.WorldToView(camera.Eye, camera.Target, camera.Up);
                float aspect = (float)width / height;
                float vWidth = 2 * camera.Near * (float)Math.Tan(camera.Fov / 2);
                Matrix projectionMatrix = CoordinateSystems.ViewToPerspective(vWidth, vWidth / aspect, camera.Near, camera.Far);
                Matrix viewportMatrix = CoordinateSystems.PerspectiveToWindowOfView(0, 0, width, height);
                Matrix mvp = viewportMatrix * projectionMatrix * viewMatrix * modelMatrix;

                Vector3[] vertexNormals = isPhong ? ComputeVertexNormals(model) : null;

                foreach (var face in model.Faces)
                {
                    int i0 = face.VertexIndices[0], i1 = face.VertexIndices[1], i2 = face.VertexIndices[2];

                    Vector4 v0_w = modelMatrix * new Vector4(model.Vertices[i0], 1);
                    Vector4 v1_w = modelMatrix * new Vector4(model.Vertices[i1], 1);
                    Vector4 v2_w = modelMatrix * new Vector4(model.Vertices[i2], 1);
                    Vector3 normal = Vector3.Normalize(
                        Vector3.Cross(
                            new Vector3(v2_w.X - v0_w.X, v2_w.Y - v0_w.Y, v2_w.Z - v0_w.Z),
                            new Vector3(v1_w.X - v0_w.X, v1_w.Y - v0_w.Y, v1_w.Z - v0_w.Z)));

                    Vector4 c0 = mvp * new Vector4(model.Vertices[i0], 1);
                    Vector4 c1 = mvp * new Vector4(model.Vertices[i1], 1);
                    Vector4 c2 = mvp * new Vector4(model.Vertices[i2], 1);

                    if (c0.W <= 0 || c1.W <= 0 || c2.W <= 0) continue;

                    Vector4 p0 = c0 / c0.W; Vector4 p1 = c1 / c1.W; Vector4 p2 = c2 / c2.W;

                    if ((p1.X - p0.X) * (p2.Y - p0.Y) - (p1.Y - p0.Y) * (p2.X - p0.X) >= 0) continue;

                    if (isPhong)
                    {
                        FillTriangleScanlinePhong(pBase, zBuffer, width, height, stride,
                            new VertexData
                            {
                                ScreenPos = new Vector4(p0.X, p0.Y, c0.W, 1),
                                WorldPos = new Vector3(v0_w.X, v0_w.Y, v0_w.Z),
                                Normal = TransformNormal(vertexNormals[i0], modelMatrix)
                            },
                            new VertexData
                            {
                                ScreenPos = new Vector4(p1.X, p1.Y, c1.W, 1),
                                WorldPos = new Vector3(v1_w.X, v1_w.Y, v1_w.Z),
                                Normal = TransformNormal(vertexNormals[i1], modelMatrix)
                            },
                            new VertexData
                            {
                                ScreenPos = new Vector4(p2.X, p2.Y, c2.W, 1),
                                WorldPos = new Vector3(v2_w.X, v2_w.Y, v2_w.Z),
                                Normal = TransformNormal(vertexNormals[i2], modelMatrix)
                            },
                            camera.Eye,
                            settings
                        );
                    }
                    else
                    {
                        float intensity = Math.Max(0.1f, Vector3.Dot(normal, -_lightDir));
                        int c = (int)(200 * intensity);
                        int faceColor = (255 << 24) | (c << 16) | (c << 8) | c;

                        FillTriangleScanlineLambert(pBase, zBuffer, width, height, stride,
                            new Vector4(p0.X, p0.Y, c0.W, 1), new Vector4(p1.X, p1.Y, c1.W, 1), new Vector4(p2.X, p2.Y, c2.W, 1), faceColor);
                    }
                }
            }
            finally { bmp.UnlockBits(data); }
        }
    }
}