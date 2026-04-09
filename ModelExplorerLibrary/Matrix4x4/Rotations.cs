using System.Numerics;
using System.Runtime.CompilerServices;

namespace ModelExplorerLibrary.Matrix4x4
{
    public static class Rotations
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix RotateX(float angle)
        {
            var cos = MathF.Cos(angle);
            var sin = MathF.Sin(angle);

            return new Matrix(
                new Vector4(1, 0, 0, 0),
                new Vector4(0, cos, -sin, 0),
                new Vector4(0, sin, cos, 0),
                new Vector4(0, 0, 0, 1)
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix RotateY(float angle)
        {
            var cos = MathF.Cos(angle);
            var sin = MathF.Sin(angle);

            return new Matrix(
                new Vector4(cos, 0, sin, 0),
                new Vector4(0, 1, 0, 0),
                new Vector4(-sin, 0, cos, 0),
                new Vector4(0, 0, 0, 1)
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix RotateZ(float angle)
        {
            var cos = MathF.Cos(angle);
            var sin = MathF.Sin(angle);

            return new Matrix(
                new Vector4(cos, -sin, 0, 0),
                new Vector4(sin, cos, 0, 0),
                new Vector4(0, 0, 1, 0),
                new Vector4(0, 0, 0, 1)
            );
        }
    }
}
