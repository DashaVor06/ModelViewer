using System.Numerics;
using System.Runtime.CompilerServices;
using ModelExplorerLibrary.Models;

namespace ModelExplorerLibrary.Matrix4x4
{
    public static class CoordinateSystems
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix ToWorld(SettingsClass settings)
        {
            Matrix model = Transfomations.Scale(settings.ScaleX, settings.ScaleY, settings.ScaleZ);
            Matrix rotation = Rotations.RotateZ(settings.RotZ) *
                      Rotations.RotateY(settings.RotX) *
                      Rotations.RotateX(settings.RotY);
            Matrix translation = Transfomations.Translate(settings.X, settings.Y, settings.Z);
            return  model * rotation * translation;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix WorldToView(Vector3 eye, Vector3 target, Vector3 up)
        {
            Vector3 zAxis = Vector3.Normalize(eye - target);
            Vector3 xAxis = Vector3.Normalize(Vector3.Cross(up, zAxis));
            Vector3 yAxis = Vector3.Cross(zAxis, xAxis);

            return new Matrix(
                new Vector4(xAxis.X, xAxis.Y, xAxis.Z, -Vector3.Dot(xAxis, eye)),
                new Vector4(yAxis.X, yAxis.Y, yAxis.Z, -Vector3.Dot(yAxis, eye)),
                new Vector4(zAxis.X, zAxis.Y, zAxis.Z, -Vector3.Dot(zAxis, eye)),
                new Vector4(0, 0, 0, 1)
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix ViewToPerspective(float width, float height, float zNear, float zFar)
        {
            float range = zFar - zNear;

            return new Matrix(
                new Vector4(2 * zNear / width, 0, 0, 0),
                new Vector4(0, 2 * zNear / height, 0, 0),
                new Vector4(0, 0, -(zFar + zNear) / range, -(2 * zFar * zNear) / range),
                new Vector4(0, 0, -1, 0)
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix PerspectiveToWindowOfView(float x, float y, float width, float height)
        {
            return new Matrix(
                new Vector4(width / 2, 0, 0, x + width / 2),
                new Vector4(0, -height / 2, 0, y + height / 2),
                new Vector4(0, 0, 1, 0),
                new Vector4(0, 0, 0, 1)
            );
        }
    }
}
