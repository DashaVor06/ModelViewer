using System.Numerics;

namespace ModelExplorerLibrary.Models
{
    public class SettingsClass
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public float ScaleX { get; set; }
        public float ScaleY { get; set; }
        public float ScaleZ { get; set; }
        public float RotX { get; set; }
        public float RotY { get; set; }
        public float RotZ { get; set; }
        public Vector3 AmbientColor { get; set; }
        public float SpecularStrength {  get; set; }
        public int Shininess {  get; set; }
    }
}
