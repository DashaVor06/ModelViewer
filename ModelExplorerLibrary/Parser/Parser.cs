using System.Globalization;
using System.Numerics;
using ModelExplorerLibrary.Models;

namespace ModelExplorerLibrary.Parser
{
    public class Parser
    {
        private ModelClass _model = new();

        private void ParseVertex(string[] parts)
        {
            float x = float.Parse(parts[1], CultureInfo.InvariantCulture);
            float y = float.Parse(parts[2], CultureInfo.InvariantCulture);
            float z = float.Parse(parts[3], CultureInfo.InvariantCulture);

            if (parts.Length > 4)
            {
                var w = float.Parse(parts[4], CultureInfo.InvariantCulture);
                x /= w;
                y /= w;
                z /= w;              
            }

            _model.Vertices.Add(new Vector3(x, y, z));
        }

        private void ParseFace(string[] parts)
        {
            var vertices = new List<int>();
            var textureCoordinates = new List<int>();
            var normals = new List<int>();

            for (int i = 1; i < parts.Length; i++)
            {
                var indices = parts[i].Split('/');

                vertices.Add(int.Parse(indices[0]) - 1);

                if (indices.Length > 1 && indices[1] != "")
                    textureCoordinates.Add(int.Parse(indices[1]) - 1);
                else
                    textureCoordinates.Add(-1);

                if (indices.Length > 2 && indices[2] != "")
                    normals.Add(int.Parse(indices[2]) - 1);
                else
                    normals.Add(-1);
            }

            for (int i = 1; i < vertices.Count - 1; i++)
            {
                var face = new FaceClass
                {
                    VertexIndices = new int[] { vertices[0], vertices[i], vertices[i + 1] },
                    TexCoordIndices = new int[] { textureCoordinates[0], textureCoordinates[i], textureCoordinates[i + 1] },
                    NormalIndices = new[] { normals[0], normals[i], normals[i + 1] }
                };

                _model.Faces.Add(face);
            }
        }

        private void ParseNormal(string[] parts)
        {
            float x = float.Parse(parts[1], CultureInfo.InvariantCulture);
            float y = float.Parse(parts[2], CultureInfo.InvariantCulture);
            float z = float.Parse(parts[3], CultureInfo.InvariantCulture);

            _model.Normals.Add(new Vector3(x, y, z));
        }

        private void ParseTextureCoordinates(string[] parts)
        {
            float u = float.Parse(parts[1], CultureInfo.InvariantCulture);
            float v = float.Parse(parts[2], CultureInfo.InvariantCulture);

            _model.TextureCoordinates.Add(new Vector2(u, v));
        }

        private void ParseLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                return;

            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            switch (parts[0])
            {
                case "v":
                    ParseVertex(parts);
                    break;
                case "f":
                    ParseFace(parts);
                    break;
                case "vn":
                    ParseNormal(parts);
                    break;
                case "vt":
                    ParseTextureCoordinates(parts);
                    break;
            }
        }

        public ModelClass Load(string filePath)
        {
            _model = new ModelClass();

            foreach (var line in File.ReadLines(filePath))
            {
                ParseLine(line);
            }
            return _model;
        }
    }
}
