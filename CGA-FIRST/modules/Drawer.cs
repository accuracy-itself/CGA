using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Numerics;
using System.Windows.Forms;


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
        //private Color lineColour = Color.Blue;
        private Color lineColour = Color.FromArgb(255, 255, 100, 100);
        private Color lightColor = Color.White;
        private Color backgroundColour = Color.White;

        private Vector3 eye = new Vector3(0, 0, 10);
        private Vector3 up = new Vector3(0, 1, 0);
        private Vector3 target = new Vector3(0, 0, 0);

        private Vector3 lightDirection = new Vector3(-1, -1, 1);
        //public static Vector3 light = new Vector3(0f, 0f, 1f);
        private float lightIntensity = 5000f;
        private float ambientLightIntensity = 1/5f;
        private float diffuseLightIntensity = 2f;
        private float specularFactor = 10f;
        private float glossFactor = 24f;

        private List<Vector4> vertexes_changeable;
        private List<Vector4> vertexes_start;
        private List<Vector4> vertexes_view;
        private List<Vector4> vertexes_world;
        private List<double[]> vertexes;
        private List<List<List<int>>> faces;
        private List<Vector3> normals;
        private List<Vector3> normals_changeable;

        private Matrix4x4 worldToViewMatrix;
        private Matrix4x4 viewToProjectionMatrix;
        private Matrix4x4 projectionToScreenMatrix;
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

        public Drawer(int width, int height, List<double[]> vertexes, List<List<List<int>>> faces, List<Vector3> normals)
        {
            lightDirection = Vector3.Normalize(lightDirection);
            window_width = width;
            window_height = height;
            //zNear = window_width / 2;
            this.faces = faces;
            this.vertexes = vertexes;
            this.normals = normals;
            float aspect = (float)window_width / window_height;
            viewToProjectionMatrix = new Matrix4x4(
                    (float)(1 / (aspect * Math.Tan(Math.PI/8))), 0, 0, 0,
                    0, (float)(1 / Math.Tan(Math.PI / 8)), 0, 0,
                    0, 0, (float)(zFar / (zNear - zFar)), (float)(zNear * zFar / (zNear - zFar)),
                    0, 0, -1, 0
                );

            projectionToScreenMatrix = new Matrix4x4(
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
            vertexes_world = new List<Vector4>();
            normals_changeable = new List<Vector3>();
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
            vertexes_world.Clear();
            normals_changeable.Clear();

            for (int i = 0; i < vertexes_start.Count; i++)
            {
                //from model
                //to world
                vertexes_changeable.Add(vertexes_start[i]);
                vertexes_changeable[i] = vertexes_changeable[i].ApplyMatrix(scaleMatrix);
                vertexes_changeable[i] = vertexes_changeable[i].ApplyMatrix(scaleMatrix);

                vertexes_changeable[i] = vertexes_changeable[i].ApplyMatrix(translationMatrix);
                vertexes_changeable[i] = vertexes_changeable[i].ApplyMatrix(MatrixRotater.rotationMatrixX);
                vertexes_changeable[i] = vertexes_changeable[i].ApplyMatrix(MatrixRotater.rotationMatrixY);
                vertexes_changeable[i] = vertexes_changeable[i].ApplyMatrix(MatrixRotater.rotationMatrixZ);
                vertexes_world.Add(vertexes_changeable[i]);
                
                //to observer
                vertexes_changeable[i] = vertexes_changeable[i].ApplyMatrix(worldToViewMatrix);
                vertexes_view.Add(vertexes_changeable[i]);
                
                //to projection
                vertexes_changeable[i] = vertexes_changeable[i].ApplyMatrix(viewToProjectionMatrix);
                vertexes_changeable[i] = Vector4.Divide(vertexes_changeable[i], vertexes_changeable[i].W);

                //to screen
                vertexes_changeable[i] = vertexes_changeable[i].ApplyMatrix(projectionToScreenMatrix);
            }

            for (int i = 0; i < normals.Count; i++) {
                normals_changeable.Add(normals[i]);
                normals_changeable[i] = normals_changeable[i].ApplyMatrix(translationMatrix);
                normals_changeable[i] = normals_changeable[i].ApplyMatrix(MatrixRotater.rotationMatrixX);
                normals_changeable[i] = normals_changeable[i].ApplyMatrix(MatrixRotater.rotationMatrixY);
                normals_changeable[i] = normals_changeable[i].ApplyMatrix(MatrixRotater.rotationMatrixZ);
                normals_changeable[i] = normals_changeable[i].ApplyMatrix(worldToViewMatrix);
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

            for (int j = 0; j < faces.Count; j++)
            {
                List<List<int>> face = faces[j];
                if (!IsBackFace(face))
                {
                    //Vector3 normal = CalculateNormal(face);
                    //float lightIntensity = CalculateLightIntensity(normal, lightDirection);

                    FillTriangle(face, bData, bitsPerPixel, bmp, scan0);
                }
            }

            //DrawTriangles();
            bmp.UnlockBits(bData);
            return bmp;
        }

        public unsafe void FillTriangle(List<List<int>> face, BitmapData bData, int bitsPerPixel, Bitmap bmp, byte* scan0)
        {
            Vector3[] worldTriangle = { MatrixSolver.createFromVector4(vertexes_world[face[0][0] - 1]), 
                                        MatrixSolver.createFromVector4(vertexes_world[face[1][0] - 1]), 
                                        MatrixSolver.createFromVector4(vertexes_world[face[2][0] - 1])};

            //screen
            Vector4 a = vertexes_changeable[face[0][0] - 1];
            Vector4 b = vertexes_changeable[face[1][0] - 1];
            Vector4 c = vertexes_changeable[face[2][0] - 1];

            /*
            Vector3 vertexNormal0 = Vector3.Normalize(normals[face[0][2] - 1]);
            Vector3 vertexNormal1 = Vector3.Normalize(normals[face[1][2] - 1]);
            Vector3 vertexNormal2 = Vector3.Normalize(normals[face[2][2] - 1]);
            */
            Vector3 vertexNormal0 = Vector3.Normalize(normals_changeable[face[0][2] - 1]);
            Vector3 vertexNormal1 = Vector3.Normalize(normals_changeable[face[1][2] - 1]);
            Vector3 vertexNormal2 = Vector3.Normalize(normals_changeable[face[2][2] - 1]);


            if (a.Y > c.Y) {
                (a, c) = (c, a);
                (vertexNormal0, vertexNormal2) = (vertexNormal2, vertexNormal0);
                (worldTriangle[0], worldTriangle[2]) = (worldTriangle[2], worldTriangle[0]);
            }

            if (a.Y > b.Y)
            {
                (a, b) = (b, a);
                (vertexNormal0, vertexNormal1) = (vertexNormal1, vertexNormal0);
                (worldTriangle[0], worldTriangle[1]) = (worldTriangle[1], worldTriangle[0]);
            }

            if (b.Y > c.Y)
            {
                (b, c) = (c, b);
                (vertexNormal1, vertexNormal2) = (vertexNormal2, vertexNormal1);
                (worldTriangle[1], worldTriangle[2]) = (worldTriangle[2], worldTriangle[1]);
            }

            Vector4 k1 = (c - a) / (c.Y - a.Y);
            Vector4 screenKoeff02 = (c - a) / (c.Y - a.Y);
            Vector3 vertexNormalKoeff02 = (vertexNormal2 - vertexNormal0) / (c.Y - a.Y);
            Vector3 worldKoeff02 = (worldTriangle[2] - worldTriangle[0]) / (c.Y - a.Y);

            Vector4 k2 = (b - a) / (b.Y - a.Y);
            Vector4 screenKoeff01 = (b - a) / (b.Y - a.Y);
            Vector3 vertexNormalKoeff01 = (vertexNormal1 - vertexNormal0) / (b.Y - a.Y);
            Vector3 worldKoeff01 = (worldTriangle[1] - worldTriangle[0]) / (b.Y - a.Y);

            Vector4 k3 = (c - b) / (c.Y - b.Y);
            Vector4 screenKoeff03 = (c - b) / (c.Y - b.Y);
            Vector3 vertexNormalKoeff03 = (vertexNormal2 - vertexNormal1) / (c.Y - b.Y);
            Vector3 worldKoeff03 = (worldTriangle[2] - worldTriangle[1]) / (c.Y - b.Y);

            int top = Math.Max(0, (int)Math.Ceiling(a.Y));
            int bottom = Math.Min(window_height, (int)Math.Ceiling(c.Y));

            for (int y = top; y < bottom; y++) {
                Vector4 l = a + (y - a.Y) * k1;
                Vector4 r = (y < b.Y) ? a + (y - a.Y) * k2 : b + (y - b.Y) * k3;

                Vector3 worldL = y < b.Y ? worldTriangle[0] + (y - a.Y) * worldKoeff01 :
                                           worldTriangle[1] + (y - b.Y) * worldKoeff03;
                Vector3 worldR = worldTriangle[0] + (y - a.Y) * worldKoeff02;


                // Нахождение нормали для левого и правого Y.
                Vector3 normalL = y < b.Y ? vertexNormal0 + (y - a.Y) * vertexNormalKoeff01 :
                                                            vertexNormal1 + (y - b.Y) * vertexNormalKoeff03;
                Vector3 normalR = vertexNormal0 + (y - a.Y) * vertexNormalKoeff02;

                if (l.X > r.X) {
                    (l, r) = (r, l);
                    (normalL, normalR) = (normalR, normalL);
                    (worldL, worldR) = (worldR, worldL);
                }

                Vector4 k = (r - l) / (r.X - l.X);
                Vector3 normalKoeff = (normalR - normalL) / (r.X - l.X);
                Vector3 worldKoeff = (worldR - worldL) / (r.X - l.X);

                int left = Math.Max(0, (int) Math.Ceiling(l.X));
                int right = Math.Min(window_width, (int)Math.Ceiling(r.X));


                for (int x = left; x < right; x++) {
                    Vector4 p = l + (x - l.X) * k;
                    Vector3 pWorld = worldL + (x - l.X) * worldKoeff;

                    int index = (int)y * window_width + (int)x;
                    if (p.Z < zBuffer[index])
                    {
                        // Нахождение обратного вектора направления света.
                        //Vector3 light = Vector3.Normalize( lightDirection - pWorld);

                        Vector3 normal = normalL + (x - l.X) * normalKoeff;
                        normal = Vector3.Normalize(normal);

                        // Нахождение дистанции до источника света.
                        //float distance = (lightDirection - pWorld).LengthSquared();

                        // Затенение объекта в зависимости от дистанции света до модели.
                        //float attenuation = 1 / Math.Max(distance, 0.01f);

                        // Получение затененности каждой точки.
                        //float intensity = Math.Max(Vector3.Dot(normal, lightDirection), 0);
                        //float intensity = Math.Max(CalculateLightIntensity(normal, lightDirection), 0);

                        float[] ambientValues = AmbientLightning();

                        float[] diffuseValues = DiffuseLightning(normal, lightDirection);

                        float[] specularValues = SpecularLightning(Vector3.Normalize(eye - pWorld), lightDirection, normal);


                        zBuffer[index] = p.Z;
                        byte* data = scan0 + (int)y * bData.Stride + (int)x * bitsPerPixel / 8;

                        byte B = (byte)(Math.Min(lineColour.B * (ambientValues[2]), 255));
                        byte G = (byte)(lineColour.G * (ambientValues[1]));
                        byte R = (byte)(lineColour.R * (ambientValues[0]));

                        data[0] = (byte) Math.Min(lineColour.B * (ambientValues[2] + diffuseValues[2] + specularValues[2]), 255);
                        data[1] = (byte) Math.Min(lineColour.G * (ambientValues[1] + diffuseValues[1] + specularValues[1]), 255);
                        data[2] = (byte) Math.Min(lineColour.R * (ambientValues[0] + diffuseValues[0] + specularValues[0]), 255);

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

        private float[] AmbientLightning()
        {
            float[] values = new float[3];

            values[0] = (float)(lightColor.R / 255 * ambientLightIntensity);
            values[1] = (float)(lightColor.G / 255 * ambientLightIntensity);
            values[2] = (float)(lightColor.B / 255 * ambientLightIntensity);

            return values;

        }


        private float[] DiffuseLightning(Vector3 normal, Vector3 lightDirection)
        {
            float[] values = new float[3];
            float scalar = Math.Max(CalculateLightIntensity(normal, lightDirection), 0) * diffuseLightIntensity;
            values[0] = (float)(lightColor.R / 255 * scalar);
            values[1] = (float)(lightColor.G / 255 * scalar);
            values[2] = (float)(lightColor.B / 255 * scalar);
            return values;
        }

        private float[] SpecularLightning(Vector3 View, Vector3 lightDirection, Vector3 normal)
        {
            Vector3 reflection = Vector3.Normalize(Vector3.Reflect(-lightDirection, normal));
            float RV = Math.Max(Vector3.Dot(reflection, View), 0);

            float[] values = new float[3];
            float temp = (float)Math.Pow(RV, glossFactor);

            values[0] = (specularFactor * temp);
            values[1] = (specularFactor * temp);
            values[2] = (specularFactor * temp);

            return values;
        }
    }
}
