using System;
using System.Drawing;
using System.Numerics;

namespace CGA_FIRST.modules
{
    public static class MatrixRotater
    {
        private const double DELTA_ANGLE = 0.01;
        static double angleY, angleX, angleZ;
        public static Matrix4x4 rotationMatrixX = new Matrix4x4(
                1, 0, 0, 0,
                0, 1, 0, 0,
                0, 0, 1, 0,
                0, 0, 0, 1
            );

        public static Matrix4x4 rotationMatrixY = new Matrix4x4(
                1, 0, 0, 0,
                0, 1, 0, 0,
                0, 0, 1, 0,
                0, 0, 0, 1
            );

        public static Matrix4x4 rotationMatrixZ = new Matrix4x4(
                1, 0, 0, 0,
                0, 1, 0, 0,
                0, 0, 1, 0,
                0, 0, 0, 1
            );
        public static void Rotate(Point path)
        {
            angleX += path.Y * DELTA_ANGLE;
            angleY += path.X * DELTA_ANGLE;

            rotationMatrixX.M22 = (float)Math.Cos(angleX);
            rotationMatrixX.M23 = -(float)Math.Sin(angleX);
            rotationMatrixX.M32 = (float)Math.Sin(angleX);
            rotationMatrixX.M33 = (float)Math.Cos(angleX);

            rotationMatrixY.M11 = (float)Math.Cos(angleY);
            rotationMatrixY.M13 = (float)Math.Sin(angleY);
            rotationMatrixY.M31 = -(float)Math.Sin(angleY);
            rotationMatrixY.M33 = (float)Math.Cos(angleY);
        }

        public static void Rotate(Double deltaX, Double deltaY, Double deltaZ)
        {
            angleX += deltaX;
            angleY += deltaY;
            angleZ += deltaZ;

            rotationMatrixX.M22 = (float)Math.Cos(angleX);
            rotationMatrixX.M23 = -(float)Math.Sin(angleX);
            rotationMatrixX.M32 = (float)Math.Sin(angleX);
            rotationMatrixX.M33 = (float)Math.Cos(angleX);

            rotationMatrixY.M11 = (float)Math.Cos(angleY);
            rotationMatrixY.M13 = (float)Math.Sin(angleY);
            rotationMatrixY.M31 = -(float)Math.Sin(angleY);
            rotationMatrixY.M33 = (float)Math.Cos(angleY);

            rotationMatrixZ.M11 = (float)Math.Cos(angleZ);
            rotationMatrixZ.M12 = -(float)Math.Sin(angleZ);
            rotationMatrixZ.M21 = (float)Math.Sin(angleZ);
            rotationMatrixZ.M22 = (float)Math.Cos(angleZ);
        }
    }
}
