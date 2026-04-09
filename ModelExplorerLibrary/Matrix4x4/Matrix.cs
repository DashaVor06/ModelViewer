using System.Numerics;

namespace ModelExplorerLibrary.Matrix4x4
{
    public partial struct Matrix
    {
        private Vector4 _row1;
        private Vector4 _row2;
        private Vector4 _row3;
        private Vector4 _row4;

        public Matrix(Vector4 row1, Vector4 row2, Vector4 row3, Vector4 row4)
        {
            _row1 = row1;
            _row2 = row2;
            _row3 = row3;
            _row4 = row4;
        }
    }
}

