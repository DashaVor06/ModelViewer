using System.Numerics;
using System.Runtime.CompilerServices;

namespace ModelExplorerLibrary.Matrix4x4
{
    public partial struct Matrix
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 operator *(Matrix m, Vector4 v)
        {
            return new Vector4(
                Vector4.Dot(m._row1, v),
                Vector4.Dot(m._row2, v),
                Vector4.Dot(m._row3, v),
                Vector4.Dot(m._row4, v)
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix operator *(Matrix a, Matrix b)
        {
            var bCol1 = new Vector4(b._row1.X, b._row2.X, b._row3.X, b._row4.X);
            var bCol2 = new Vector4(b._row1.Y, b._row2.Y, b._row3.Y, b._row4.Y);
            var bCol3 = new Vector4(b._row1.Z, b._row2.Z, b._row3.Z, b._row4.Z);
            var bCol4 = new Vector4(b._row1.W, b._row2.W, b._row3.W, b._row4.W);

            return new Matrix
            {
                _row1 = new Vector4(
                    Vector4.Dot(a._row1, bCol1),
                    Vector4.Dot(a._row1, bCol2),
                    Vector4.Dot(a._row1, bCol3),
                    Vector4.Dot(a._row1, bCol4)
                ),
                _row2 = new Vector4(
                    Vector4.Dot(a._row2, bCol1),
                    Vector4.Dot(a._row2, bCol2),
                    Vector4.Dot(a._row2, bCol3),
                    Vector4.Dot(a._row2, bCol4)
                ),
                _row3 = new Vector4(
                    Vector4.Dot(a._row3, bCol1),
                    Vector4.Dot(a._row3, bCol2),
                    Vector4.Dot(a._row3, bCol3),
                    Vector4.Dot(a._row3, bCol4)
                ),
                _row4 = new Vector4(
                    Vector4.Dot(a._row4, bCol1),
                    Vector4.Dot(a._row4, bCol2),
                    Vector4.Dot(a._row4, bCol3),
                    Vector4.Dot(a._row4, bCol4)
                )
            };
        }
    }
}
