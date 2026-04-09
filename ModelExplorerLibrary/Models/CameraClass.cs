using System.Numerics;

namespace ModelExplorerLibrary.Models
{
    public class CameraClass
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public float Fov { get; set; }
        public float Near { get; set; }
        public float Far { get; set; }
        public Vector3 Eye { get; set; }
        public Vector3 Target { get; set; }
        public Vector3 Up { get; set; }
    }
}
