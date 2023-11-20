using System.Numerics;

namespace CGA_FIRST.modules
{
    public static class MatrixSolver
    {
        public static Vector4 ApplyMatrix(this Vector4 self, Matrix4x4 matrix)
        {
            return new Vector4(
                matrix.M11 * self.X + matrix.M12 * self.Y + matrix.M13 * self.Z + matrix.M14 * self.W,
                matrix.M21 * self.X + matrix.M22 * self.Y + matrix.M23 * self.Z + matrix.M24 * self.W,
                matrix.M31 * self.X + matrix.M32 * self.Y + matrix.M33 * self.Z + matrix.M34 * self.W,
                matrix.M41 * self.X + matrix.M42 * self.Y + matrix.M43 * self.Z + matrix.M44 * self.W
            );
        }

        public static Vector3 ApplyMatrix(this Vector3 self, Matrix4x4 matrix)
        {
            return new Vector3(
                matrix.M11 * self.X + matrix.M12 * self.Y + matrix.M13 * self.Z,
                matrix.M21 * self.X + matrix.M22 * self.Y + matrix.M23 * self.Z,
                matrix.M31 * self.X + matrix.M32 * self.Y + matrix.M33 * self.Z
            );
        }

        public static Vector3 createFromVector4(Vector4 vector) {
            return new Vector3(vector.X, vector.Y, vector.Z);
        }
    }
}
