using System.Numerics;

namespace ModelExplorerLibrary.Models
{
    public class ModelClass
    {
        public List<Vector3> Vertices { get; private set; } = new();
        public List<Vector3> Normals { get; private set; } = new();
        public List<Vector2> TextureCoordinates { get; private set; } = new();
        public List<FaceClass> Faces { get; private set; } = new();
    }
}
