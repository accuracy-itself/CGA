using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;

namespace CGA_FIRST.modules
{
    public class Drawer
    {
        public float x = 0, y = 0, z = 0;
        private int window_width;
        private int window_height;
        private const int scale = 1;
        private const int zoom_number = 40;
        private float zFar = 1000000, zNear = 0.1F;
        private Color lineColour = Color.Blue;
        private Color lightColor = Color.Blue;
        private Color backgroundColour = Color.White;

        private Vector3 eye = new Vector3(0, 0, 10);
        private Vector3 up = new Vector3(0, 1, 0);
        private Vector3 target = new Vector3(0, 0, 0);
        private Vector3 lightDirection = new Vector3(1, 0, -1);
        private List<Vector4> vertexes_changeable;
        private List<Vector4> vertexes_start;
        private List<Vector4> vertexes_view;
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

        private float[] zBuffer;

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

            zBuffer = new float[window_width * window_height];
            cleanZBuffer();

            Vector4 temp;
            vertexes_start = new List<Vector4>();
            vertexes_view = new List<Vector4>();
            foreach (double[] vertex in vertexes)
            {
                if (vertex.Length == 3)
                    temp = new Vector4((float)vertex[0], (float)vertex[1], (float)vertex[2], 1);
                else
                    temp = new Vector4((float)vertex[0], (float)vertex[1], (float)vertex[2], (float)vertex[3]);

                vertexes_start.Add(temp);
            }
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
            vertexes_changeable.Clear();
            vertexes_view.Clear();
            

            for (int i = 0; i < vertexes_start.Count; i++)
            {
                vertexes_changeable.Add(vertexes_start[i]);
                vertexes_changeable[i] = vertexes_changeable[i].ApplyMatrix(scaleMatrix);
                vertexes_changeable[i] = vertexes_changeable[i].ApplyMatrix(scaleMatrix);

                vertexes_changeable[i] = vertexes_changeable[i].ApplyMatrix(translationMatrix);
                vertexes_changeable[i] = vertexes_changeable[i].ApplyMatrix(MatrixRotater.rotationMatrixX);
                vertexes_changeable[i] = vertexes_changeable[i].ApplyMatrix(MatrixRotater.rotationMatrixY);
                vertexes_changeable[i] = vertexes_changeable[i].ApplyMatrix(MatrixRotater.rotationMatrixZ);
                vertexes_changeable[i] = vertexes_changeable[i].ApplyMatrix(worldToViewMatrix);
                vertexes_view.Add(vertexes_changeable[i]);
                vertexes_changeable[i] = vertexes_changeable[i].ApplyMatrix(viewToProjectionMatrix);
                vertexes_changeable[i] = Vector4.Divide(vertexes_changeable[i], vertexes_changeable[i].W);
                vertexes_changeable[i] = vertexes_changeable[i].ApplyMatrix(projectionToViewMatrix);
            }
        }

        private void cleanZBuffer() {
            for (int i = 0; i < zBuffer.Length; i++)
            {
                zBuffer[i] = float.PositiveInfinity;
            }
        }

        public unsafe Bitmap Draw()
        {
            changeVertexes();
            cleanZBuffer();

            Bitmap bmp = new Bitmap(window_width, window_height);
            Graphics gfx = Graphics.FromImage(bmp);
            SolidBrush brush = new SolidBrush(backgroundColour);

            gfx.FillRectangle(brush, 0, 0, window_width, window_height);

            BitmapData bData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height),
                ImageLockMode.ReadWrite, bmp.PixelFormat);
            byte bitsPerPixel = (byte)System.Drawing.Bitmap.GetPixelFormatSize(bData.PixelFormat);
            byte* scan0 = (byte*)bData.Scan0.ToPointer();

            int x1, x2, y1, y2, z1, z2;
            for (int j = 0; j < faces.Count; j++)
            {
                List<List<int>> face = faces[j];
                if (!IsBackFace(face))
                {
                    Vector3 normal = CalculateNormal(face);
                    float lightIntensity = CalculateLightIntensity(normal, lightDirection);
                    Color color = CalculateColor(lightIntensity, lightColor);
                    for (int i = 0; i < face.Count; i++)
                    {
                        List<int> temp = face[i];
                        if (temp[0] - 1 < 0)
                            continue;
                        x1 = (int)vertexes_changeable[temp[0] - 1].X;
                        y1 = (int)vertexes_changeable[temp[0] - 1].Y;
                        z1 = (int)vertexes_changeable[temp[0] - 1].Z;
                        if (i == face.Count - 1)
                        {
                            x2 = (int)vertexes_changeable[face[0][0] - 1].X;
                            y2 = (int)vertexes_changeable[face[0][0] - 1].Y;
                            z2 = (int)vertexes_changeable[face[0][0] - 1].Z;
                        }
                        else
                        {
                            if (face[i + 1][0] - 1 < 0)
                                continue;
                            
                            x2 = (int)vertexes_changeable[face[i + 1][0] - 1].X;
                            y2 = (int)vertexes_changeable[face[i + 1][0] - 1].Y;
                            z2 = (int)vertexes_changeable[face[i + 1][0] - 1].Z;
                            
                        }

                        DrawLine(x1, x2, y1, y2, z1, z2, bData, bitsPerPixel, bmp, scan0, color);
                    }

                    FillTriangle(face, bData, bitsPerPixel, bmp, scan0, color);
                }
            }

            //DrawTriangles();
            bmp.UnlockBits(bData);
            return bmp;
        }

        public unsafe void FillTriangle(List<List<int>> face, BitmapData bData, int bitsPerPixel, Bitmap bmp, byte* scan0, Color color)
        {
            Vector4 a = vertexes_changeable[face[0][0] - 1];
            Vector4 b = vertexes_changeable[face[1][0] - 1];
            Vector4 c = vertexes_changeable[face[2][0] - 1];
             
            if (a.Y > c.Y) {
                (a, c) = (c, a);   
            }

            if (a.Y > b.Y)
            {
                (a, b) = (b, a);
            }

            if (b.Y > c.Y)
            {
                (b, c) = (c, b);
            }

            Vector4 k1 = (c - a) / (c.Y - a.Y);
            Vector4 k2 = (b - a) / (b.Y - a.Y);
            Vector4 k3 = (c - b) / (c.Y - b.Y);

            int top = Math.Max(0, (int)Math.Ceiling(a.Y));
            int bottom = Math.Min(window_height, (int)Math.Ceiling(c.Y));

            for (int y = top; y < bottom; y++) {
                Vector4 l = a + (y - a.Y) * k1;
                Vector4 r = (y < b.Y) ? a + (y - a.Y) * k2 : b + (y - b.Y) * k3;

                if (l.X > r.X) {
                    (l, r) = (r, l);
                }

                Vector4 k = (r - l) / (r.X - l.X);
                
                int left = Math.Max(0, (int) Math.Ceiling(l.X));
                int right = Math.Min(window_width, (int)Math.Ceiling(r.X));

                for (int x = left; x < right; x++) {
                    Vector4 p = l + (x - l.X) * k;
                    
                    int index = (int)y * window_width + (int)x;
                    if (p.Z < zBuffer[index])
                    {
                        zBuffer[index] = p.Z;
                        byte* data = scan0 + (int)y * bData.Stride + (int)x * bitsPerPixel / 8;

                        data[0] = color.B;
                        data[1] = color.G;
                        data[2] = color.R;
                    }

                }
            }

            /*

            int x1 = (int)vertexes_changeable[face[0][0] - 1].X;
            int y1 = (int)vertexes_changeable[face[0][0] - 1].Y;
            int z1 = (int)vertexes_changeable[face[0][0] - 1].Z;
            int x2 = (int)vertexes_changeable[face[1][0] - 1].X;
            int y2 = (int)vertexes_changeable[face[1][0] - 1].Y;
            int z2 = (int)vertexes_changeable[face[1][0] - 1].Z;
            int x3 = (int)vertexes_changeable[face[2][0] - 1].X;
            int y3 = (int)vertexes_changeable[face[2][0] - 1].Y;
            int z3 = (int)vertexes_changeable[face[2][0] - 1].Z;

            int steps = Math.Max(Math.Max(Math.Abs(x3 - x1), Math.Abs(y3 - y1)),
                                 Math.Max(Math.Abs(x3 - x2), Math.Abs(y3 - y2)));
            float dx1 = (float)(x3 - x1) / steps;
            float dx2 = (float)(x3 - x2) / steps;
            float dy1 = (float)(y3 - y1) / steps;
            float dy2 = (float)(y3 - y2) / steps;
            float dz1 = (float)(z3 - z1) / steps;
            float dz2 = (float)(z3 - z2) / steps;

            float x11 = x1;
            float y11 = y1;
            float z11 = z1;
            float x12 = x2;
            float y12 = y2;
            float z12 = z2;
            for (int i = 0; i < steps; i++)
            {
                x11 += dx1;
                y11 += dy1;
                z11 += dz1;
                x12 += dx2;
                y12 += dy2;
                z12 += dz2;

                DrawLine((int)x11, (int)x12, (int)y11, (int)y12, (int)z11, (int)z12, bData, bitsPerPixel, bmp, scan0, color);
            }
            */

        }

        private unsafe void  DrawLine(int x1, int x2, int y1, int y2, int z1, int z2, BitmapData bData, int bitsPerPixel, Bitmap bmp, byte* scan0, Color color) {
            int steps = Math.Max(Math.Abs(x2 - x1), Math.Abs(y2 - y1));
            float dx = (float)(x2 - x1) / steps;
            float dy = (float)(y2 - y1) / steps;
            float dz = (float)(z2 - z1) / steps;

            float x = x1;
            float y = y1;
            float z = z1;
            if ((x < window_width) && (x > 0) && (y < window_height) && (y > 0))
            {
                int index = (int)y * window_width + (int)x;
                if (z > zBuffer[index])
                {
                    zBuffer[index] = z;
                    byte* data = scan0 + (int)y * bData.Stride + (int)x * bitsPerPixel / 8;

                    data[0] = color.B;
                    data[1] = color.G;
                    data[2] = color.R;
                }
            }

            for (int k = 0; k < steps; k++)
            {
                x += dx;
                y += dy;
                z += dz;

                if ((x < bmp.Width) && (x > 0) && (y < bmp.Height) && (y > 0))
                {
                    int index = (int)y * window_width + (int)x;
                    if (z > zBuffer[index])
                    {
                        zBuffer[index] = z;
                        byte* data = scan0 + (int)y * bData.Stride + (int)x * bitsPerPixel / 8;

                        data[0] = color.B;
                        data[1] = color.G;
                        data[2] = color.R;
                    }
                }
            }
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
            vertexes_changeable = new List<Vector4>();
            return Draw();
        }

        private bool IsBackFace(List<List<int>> face)
        {
            Vector3 viewVector = ToVector3(vertexes_view[face[0][0] - 1]) - eye;

            return Vector3.Dot(CalculateNormal(face), viewVector) <= 0;
        }
        
        private Vector3 ToVector3(Vector4 vector)
        {
            return new Vector3(vector.X, vector.Y, vector.Z);
        }

        private Vector3 CalculateNormal(List<List<int>> face)
        {
            Vector3 v1 = Vector3.Normalize(ToVector3(vertexes_view[face[1][0] - 1]) - ToVector3(vertexes_view[face[0][0] - 1]));
            Vector3 v2 = Vector3.Normalize(ToVector3(vertexes_view[face[2][0] - 1]) - ToVector3(vertexes_view[face[0][0] - 1]));
            return Vector3.Normalize(Vector3.Cross(v2, v1));
        }

        private float CalculateLightIntensity(Vector3 normal, Vector3 lightDirection)
        {
            float scalar = Vector3.Dot(normal * -1, lightDirection * -1);
            if (scalar - 1 > 0) return 1;
            return Math.Max(scalar, 0);
        }

        private Color CalculateColor(float lightIntensity, Color lightColor)
        {
            return Color.FromArgb(
                (int)(lightIntensity * lightColor.R),
                (int)(lightIntensity * lightColor.G),
                (int)(lightIntensity * lightColor.B));
        }
    }
}
