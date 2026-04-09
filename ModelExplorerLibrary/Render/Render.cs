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
        private Vector3 _lightDir = Vector3.Normalize(new Vector3(-1.0f, -1.0f, 1.0f));

        private unsafe void DrawHorizontalLine(int* pBase, float[] zBuffer, int width, int height, int stride, int x1, int x2, int y, float z1, float z2, int color)
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

                if (currentZ > zBuffer[idx])
                {
                    zBuffer[idx] = currentZ;
                    int offset = y * (stride / 4) + x;
                    *(pBase + offset) = color;
                }
            }
        }

        private unsafe void FillTriangleScanline(int* pBase, float[] zBuffer, int width, int height, int stride, Vector4 v0, Vector4 v1, Vector4 v2, int color)
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

                DrawHorizontalLine(pBase, zBuffer, width, height, stride, (int)Math.Round(A.X), (int)Math.Round(B.X), y, A.Z, B.Z, color);
            }
        }

        public unsafe void RenderModel(Bitmap bmp, ModelClass model, SettingsClass settings, CameraClass camera)
        {
            int width = bmp.Width;
            int height = bmp.Height;

            BitmapData data = bmp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format32bppPArgb);

            try
            {
                int* pBase = (int*)data.Scan0;
                int stride = data.Stride;

                float[] zBuffer = new float[width * height];
                for (int i = 0; i < zBuffer.Length; i++) zBuffer[i] = float.MinValue;
                for (int i = 0; i < width * height; i++) pBase[i] = _bgColor;

                Matrix modelMatrix = CoordinateSystems.ToWorld(settings);
                Matrix viewMatrix = CoordinateSystems.WorldToView(camera.Eye, camera.Target, camera.Up);
                float aspect = (float)width / height;
                float vWidth = 2 * camera.Near * (float)Math.Tan(camera.Fov / 2);
                float vHeight = vWidth / aspect;
                Matrix projectionMatrix = CoordinateSystems.ViewToPerspective(vWidth, vHeight, camera.Near, camera.Far);
                Matrix viewportMatrix = CoordinateSystems.PerspectiveToWindowOfView(0, 0, width, height);

                Matrix mvp = viewportMatrix * projectionMatrix * viewMatrix * modelMatrix;

                foreach (var face in model.Faces)
                {
                    Vector4 v0_w4 = modelMatrix * new Vector4(model.Vertices[face.VertexIndices[0]], 1);
                    Vector4 v1_w4 = modelMatrix * new Vector4(model.Vertices[face.VertexIndices[1]], 1);
                    Vector4 v2_w4 = modelMatrix * new Vector4(model.Vertices[face.VertexIndices[2]], 1);

                    Vector3 v0_w = new Vector3(v0_w4.X, v0_w4.Y, v0_w4.Z);
                    Vector3 v1_w = new Vector3(v1_w4.X, v1_w4.Y, v1_w4.Z);
                    Vector3 v2_w = new Vector3(v2_w4.X, v2_w4.Y, v2_w4.Z);

                    Vector3 edge1 = v1_w - v0_w;
                    Vector3 edge2 = v2_w - v0_w;

                    Vector3 normalRaw = Vector3.Cross(edge1, edge2);
                    float lengthSquared = normalRaw.LengthSquared();

                    if (lengthSquared < 1e-10f)
                    {
                        continue;
                    }

                    Vector3 normal = Vector3.Normalize(Vector3.Cross(v1_w - v0_w, v2_w - v0_w));

                    float intensity = Math.Max(0.1f, Vector3.Dot(normal, _lightDir));
                    int c = (int)(200 * intensity);
                    int faceColor = Color.FromArgb(255, c, c, c).ToArgb();

                    Vector4 v0_c = mvp * new Vector4(model.Vertices[face.VertexIndices[0]], 1);
                    Vector4 v1_c = mvp * new Vector4(model.Vertices[face.VertexIndices[1]], 1);
                    Vector4 v2_c = mvp * new Vector4(model.Vertices[face.VertexIndices[2]], 1);

                    if (v0_c.W > 0 || v1_c.W > 0 || v2_c.W > 0) continue;

                    Vector4 p0 = v0_c / v0_c.W;
                    Vector4 p1 = v1_c / v1_c.W;
                    Vector4 p2 = v2_c / v2_c.W;

                    float cross = (p1.X - p0.X) * (p2.Y - p0.Y) - (p1.Y - p0.Y) * (p2.X - p0.X);
                    if (cross <= 0) continue;

                    FillTriangleScanline(pBase, zBuffer, width, height, stride,
                        new Vector4(p0.X, p0.Y, v0_c.W, 1),
                        new Vector4(p1.X, p1.Y, v1_c.W, 1),
                        new Vector4(p2.X, p2.Y, v2_c.W, 1),
                        faceColor);
                }
            }
            finally { bmp.UnlockBits(data); }
        }
    }
}