using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;

namespace CGA_FIRST.modules
{
    public class Drawer
    {
        public double x = 0, y = 0, z = 0;
        private int window_width;
        private int window_height;
        private const int scale = 1;
        private const int zoom_number = 15;
        private double zFar = 1000000, zNear = 0.1;
        private Color lineColour = Color.Blue;
        private Color backgroundColour = Color.White;

        private Vector3 eye = new Vector3(0, 0, 100);
        private Vector3 up = new Vector3(0, 1, 0);
        private Vector3 target = new Vector3(0, 0, 0);
        private List<Vector4> vertexes_changeable;
        private List<double[]> vertexes;
        private List<List<List<int>>> faces;

        private Matrix4x4 worldToViewMatrix;
        private Matrix4x4 viewToProjectionMatrix;
        private Matrix4x4 projectionToViewMatrix;
        private Matrix4x4 translationMatrix = new Matrix4x4(
                1, 0, 0, 0,
                0, 1, 0, 0,
                0, 0, 1, 0,
                0, 0, 0, 1
            );

        private Matrix4x4 scaleMatrix = new Matrix4x4(
                scale, 0, 0, 0,
                0, scale, 0, 0,
                0, 0, scale, 0,
                0, 0, 0, 1
            );

        
        public Drawer(int width, int height, List<double[]> vertexes, List<List<List<int>>> faces)
        {
            window_width = width;
            window_height = height;
            //zNear = window_width / 2;
            this.faces = faces;
            this.vertexes = vertexes;
            float aspect = (float)window_width / window_height;
            viewToProjectionMatrix = new Matrix4x4(
                    (float)(1 / (aspect * Math.Tan(Math.PI/8))), 0, 0, 0,
                    0, (float)(1 / Math.Tan(Math.PI / 8)), 0, 0,
                    0, 0, (float)(zFar / (zNear - zFar)), (float)(zNear * zFar / (zNear - zFar)),
                    0, 0, -1, 0
                );

            projectionToViewMatrix = new Matrix4x4(
                    (float)(window_width / 2), 0, 0, (float)(window_width / 2),
                    0, -(float)(window_height / 2), 0, (float)(window_height / 2),
                    0, 0, 1, 0,
                    0, 0, 0, 1
                );
        }

        public void changeTranslationMatrix(float dx, float dy, float dz) {
            x += dx;
            y += dy;
            z += dz;
            translationMatrix.M14 += dx;
            translationMatrix.M24 += dy;
            translationMatrix.M34 += dz;
        }

        public void changeVertexes() {
            Vector4 temp;
            vertexes_changeable.Clear();
            foreach (double[] vertex in vertexes)
            {
                if (vertex.Length == 3)
                    temp = new Vector4((float)vertex[0], (float)vertex[1], (float)vertex[2], 1);
                else
                    temp = new Vector4((float)vertex[0], (float)vertex[1], (float)vertex[2], (float)vertex[3]);

                vertexes_changeable.Add(temp);
            }

            for (int i = 0; i < vertexes_changeable.Count; i++)
            {
                vertexes_changeable[i] = vertexes_changeable[i].ApplyMatrix(scaleMatrix);

                vertexes_changeable[i] = vertexes_changeable[i].ApplyMatrix(MatrixRotater.rotationMatrixX);
                vertexes_changeable[i] = vertexes_changeable[i].ApplyMatrix(MatrixRotater.rotationMatrixY);
                vertexes_changeable[i] = vertexes_changeable[i].ApplyMatrix(MatrixRotater.rotationMatrixZ);
                vertexes_changeable[i] = vertexes_changeable[i].ApplyMatrix(translationMatrix);
                vertexes_changeable[i] = vertexes_changeable[i].ApplyMatrix(worldToViewMatrix);
                vertexes_changeable[i] = vertexes_changeable[i].ApplyMatrix(viewToProjectionMatrix);
                vertexes_changeable[i] = Vector4.Divide(vertexes_changeable[i], vertexes_changeable[i].W);
                vertexes_changeable[i] = vertexes_changeable[i].ApplyMatrix(projectionToViewMatrix);
            }
        }

        public unsafe Bitmap Draw()
        {
            changeVertexes();

            Bitmap bmp = new Bitmap(window_width, window_height);
            Graphics gfx = Graphics.FromImage(bmp);
            SolidBrush brush = new SolidBrush(backgroundColour);

            gfx.FillRectangle(brush, 0, 0, window_width, window_height);

            BitmapData bData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height),
                ImageLockMode.ReadWrite, bmp.PixelFormat);
            byte bitsPerPixel = (byte)System.Drawing.Bitmap.GetPixelFormatSize(bData.PixelFormat);
            byte* scan0 = (byte*)bData.Scan0.ToPointer();

            int x1, x2, y1, y2;
            for (int j = 0; j < faces.Count; j++)
            {
                List<List<int>> array = faces[j];
                for (int i = 0; i < array.Count; i++)
                {
                    List<int> temp = array[i];
                    x1 = (int)vertexes_changeable[temp[0] - 1].X;
                    y1 = (int)vertexes_changeable[temp[0] - 1].Y;
                    if (i == array.Count - 1)
                    {
                        x2 = (int)vertexes_changeable[array[0][0] - 1].X;
                        y2 = (int)vertexes_changeable[array[0][0] - 1].Y;
                    }
                    else
                    {
                        x2 = (int)vertexes_changeable[array[i + 1][0] - 1].X;
                        y2 = (int)vertexes_changeable[array[i + 1][0] - 1].Y;
                    }

                    int steps = Math.Max(Math.Abs(x2 - x1), Math.Abs(y2 - y1));
                    float dx = (float)(x2 - x1) / steps;
                    float dy = (float)(y2 - y1) / steps;

                    float x = x1;
                    float y = y1;
                    if ((x < window_width) && (x > 0) && (y < window_height) && (y > 0))
                    {
                        byte* data = scan0 + (int)y * bData.Stride + (int)x * bitsPerPixel / 8;

                        data[0] = lineColour.B;
                        data[1] = lineColour.G;
                        data[2] = lineColour.R;
                    }
                    for (int k = 0; k < steps; k++)
                    {
                        x += dx;
                        y += dy;

                        if ((x < bmp.Width) && (x > 0) && (y < bmp.Height) && (y > 0))
                        {
                            byte* data = scan0 + (int)y * bData.Stride + (int)x * bitsPerPixel / 8;

                            data[0] = lineColour.B;
                            data[1] = lineColour.G;
                            data[2] = lineColour.R;
                        }
                    }
                }
            }
            bmp.UnlockBits(bData);
            return bmp;
        }

        
        public void ZoomIn()
        {
            eye.Z -= zoom_number;
            SetWorldToViewMatrix();
        }

        public void ZoomOut()
        {
            eye.Z += zoom_number;
            SetWorldToViewMatrix();
        }

        private void SetWorldToViewMatrix()
        {
            Vector3 axisZ = Vector3.Normalize(Vector3.Subtract(eye, target));
            Vector3 axisX = Vector3.Normalize(Vector3.Cross(up, axisZ));
            Vector3 axisY = Vector3.Normalize(Vector3.Cross(axisZ, axisX));
            worldToViewMatrix = new Matrix4x4(
                    axisX.X, axisX.Y, axisX.Z, -Vector3.Dot(axisX, eye),
                    axisY.X, axisY.Y, axisY.Z, -Vector3.Dot(axisY, eye),
                    axisZ.X, axisZ.Y, axisZ.Z, -Vector3.Dot(axisZ, eye),
                    0, 0, 0, 1
                    );
        }

        public Bitmap SetUpCamera()
        {
            SetWorldToViewMatrix();
            Vector4 temp;

            vertexes_changeable = new List<Vector4>();

            changeVertexes();

            Bitmap bmp = new Bitmap(window_width, window_height);
            Graphics gfx = Graphics.FromImage(bmp);
            SolidBrush brush = new SolidBrush(backgroundColour);

            gfx.FillRectangle(brush, 0, 0, window_width, window_height);
            //add locks (with bitmap через УКАЗАТЕЛИ с unsafe кодом, fast works with bitmaps in C#)
            return Draw();
        }
    }
}
