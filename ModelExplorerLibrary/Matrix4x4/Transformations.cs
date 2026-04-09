using System.Numerics;
using System.Runtime.CompilerServices;

namespace ModelExplorerLibrary.Matrix4x4
{
    public static class Transfomations
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix Translate(float x, float y, float z)
        {
            return new Matrix(
                new Vector4(1, 0, 0, x),
                new Vector4(0, 1, 0, y),
                new Vector4(0, 0, 1, z),
                new Vector4(0, 0, 0, 1)
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix Scale(float x, float y, float z)
        {
            return new Matrix(
                new Vector4(x, 0, 0, 0),
                new Vector4(0, y, 0, 0),
                new Vector4(0, 0, z, 0),
                new Vector4(0, 0, 0, 1)
            );
        }
    }
}

