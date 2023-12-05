using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Windows.Forms;


namespace CGA_FIRST.modules
{
    public class Drawer
    {
        public float x = 0, y = 0, z = 0;
        private int window_width;
        private int window_height;
        private const int scale = 1;
        private const int zoom_number = 1;
        private float zFar = 1000000, zNear = 0.1F;
        //private Color lineColour = Color.Blue;
        private Color lineColour = Color.FromArgb(255, 255, 100, 100);
        private Color lightColor = Color.White;
        private Color backgroundColour = Color.White;

        private Vector3 eye = new Vector3(0, 0, 7);
        private Vector3 up = new Vector3(0, 1, 0);
        private Vector3 target = new Vector3(0, 0, 0);

        private Vector3 lightDirection = new Vector3(-1, -1, -1);
        //public static Vector3 light = new Vector3(0f, 0f, 1f);
        private float lightIntensity = 1f;
        //private float ambientLightIntensity = 1 / 5f;
        private float ambientLightIntensity = 0.05f;
        //private float diffuseLightIntensity = 2f;
        private float diffuseLightIntensity = 1f;
        //private float specularFactor = 10f;
        private float specularLightIntensity = 1f;
        private float glossFactor = 16f;

        private List<Vector4> verteces_changeable;
        private List<Vector4> verteces_start;
        private List<Vector4> verteces_view;
        private List<Vector4> verteces_world;
        private List<double[]> verteces;
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
            this.verteces = vertexes;
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
            verteces_start = new List<Vector4>();
            verteces_view = new List<Vector4>();
            verteces_world = new List<Vector4>();
            normals_changeable = new List<Vector3>();
            foreach (double[] vertex in vertexes)
            {
                if (vertex.Length == 3)
                    temp = new Vector4((float)vertex[0], (float)vertex[1], (float)vertex[2], 1);
                else
                    temp = new Vector4((float)vertex[0], (float)vertex[1], (float)vertex[2], (float)vertex[3]);

                verteces_start.Add(temp);
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
            verteces_changeable.Clear();
            verteces_view.Clear();
            verteces_world.Clear();
            normals_changeable.Clear();

            for (int i = 0; i < verteces_start.Count; i++)
            {
                //from model
                //to world
                verteces_changeable.Add(verteces_start[i]);
                verteces_changeable[i] = verteces_changeable[i].ApplyMatrix(scaleMatrix);
                verteces_changeable[i] = verteces_changeable[i].ApplyMatrix(scaleMatrix);

                verteces_changeable[i] = verteces_changeable[i].ApplyMatrix(translationMatrix);
                verteces_changeable[i] = verteces_changeable[i].ApplyMatrix(MatrixRotater.rotationMatrixX);
                verteces_changeable[i] = verteces_changeable[i].ApplyMatrix(MatrixRotater.rotationMatrixY);
                verteces_changeable[i] = verteces_changeable[i].ApplyMatrix(MatrixRotater.rotationMatrixZ);
                float W = 1 / verteces_changeable[i].W;
                verteces_world.Add(verteces_changeable[i]);
                verteces_world[i] = new Vector4(verteces_changeable[i].X,
                    verteces_changeable[i].Y,
                    verteces_changeable[i].Z,
                    W);

                //to observer
                verteces_changeable[i] = verteces_changeable[i].ApplyMatrix(worldToViewMatrix);
                verteces_view.Add(verteces_changeable[i]);
                
                //to projection
                verteces_changeable[i] = verteces_changeable[i].ApplyMatrix(viewToProjectionMatrix);
                W = 1 / verteces_changeable[i].W;
                verteces_changeable[i] = Vector4.Divide(verteces_changeable[i], verteces_changeable[i].W);

                //to screen
                verteces_changeable[i] = verteces_changeable[i].ApplyMatrix(projectionToScreenMatrix);
                verteces_changeable[i] = new Vector4(verteces_changeable[i].X,
                    verteces_changeable[i].Y,
                    verteces_changeable[i].Z,
                    W);
                
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
            //world
            Vector4 aw = (verteces_world[face[0][0] - 1]);
            Vector4 bw = (verteces_world[face[1][0] - 1]);
            Vector4 cw = (verteces_world[face[2][0] - 1]);
            //aw *= aw.W; bw *= bw.W; cw *= cw.W;

            //screen
            Vector4 a = verteces_changeable[face[0][0] - 1];
            Vector4 b = verteces_changeable[face[1][0] - 1];
            Vector4 c = verteces_changeable[face[2][0] - 1];
            //a *= a.W; b *= b.W; c *= c.W;

            Vector3 vertexNormalA = Vector3.Normalize(normals_changeable[face[0][2] - 1]);
            Vector3 vertexNormalB = Vector3.Normalize(normals_changeable[face[1][2] - 1]);
            Vector3 vertexNormalC = Vector3.Normalize(normals_changeable[face[2][2] - 1]);

            // Поиск текстурной координаты по вершине
            Vector2 textureA = ObjParser.textures[face[0][1] - 1];/*/ screenTriangle[0].Z;*/
            Vector2 textureB = ObjParser.textures[face[1][1] - 1];/*/ screenTriangle[1].Z;*/
            Vector2 textureC = ObjParser.textures[face[2][1] - 1];/*/ screenTriangle[2].Z;*/
            textureA *= a.W; textureB *= b.W; textureC *= c.W;
            //textureA *= aw.W; textureB *= bw.W; textureC *= cw.W;
            //делим всё вначале на W


            if (a.Y > c.Y) {
                (a, c) = (c, a);
                (vertexNormalA, vertexNormalC) = (vertexNormalC, vertexNormalA);
                (textureA, textureC) = (textureC, textureA);
                (aw, cw) = (cw, aw);
            }

            if (a.Y > b.Y)
            {
                (a, b) = (b, a);
                (vertexNormalA, vertexNormalB) = (vertexNormalB, vertexNormalA);
                (textureA, textureB) = (textureB, textureA);
                (aw, bw) = (bw, aw);
            }

            if (b.Y > c.Y)
            {
                (b, c) = (c, b);
                (vertexNormalB, vertexNormalC) = (vertexNormalC, vertexNormalB);
                (textureB, textureC) = (textureC, textureB);
                (bw, cw) = (cw, bw);
            }



            Vector4 k1 = (c - a) / (c.Y - a.Y);
            Vector3 vertexNormalKoeff1 = (vertexNormalC - vertexNormalA) / (c.Y - a.Y);
            Vector4 worldKoeff1 = (cw - aw) / (c.Y - a.Y);
            Vector2 textureKoeff1 = (textureC - textureA) / (c.Y - a.Y);

            Vector4 k2 = (b - a) / (b.Y - a.Y);
            Vector3 vertexNormalKoeff2 = (vertexNormalB - vertexNormalA) / (b.Y - a.Y);
            Vector4 worldKoeff2 = (bw - aw) / (b.Y - a.Y);
            Vector2 textureKoeff2 = (textureB - textureA) / (b.Y - a.Y);

            Vector4 k3 = (c - b) / (c.Y - b.Y);
            Vector3 vertexNormalKoeff3 = (vertexNormalC - vertexNormalB) / (c.Y - b.Y);
            Vector4 worldKoeff3 = (cw - bw) / (c.Y - b.Y);
            Vector2 textureKoeff3 = (textureC - textureB) / (c.Y - b.Y);

            //добавить по коэффу (3 штуки) как на доске после к1 (не надо)
            //надо в w записать обратное
            //

            int top = Math.Max(0, (int)Math.Ceiling(a.Y));
            int bottom = Math.Min(window_height, (int)Math.Ceiling(c.Y));

            for (int y = top; y < bottom; y++) {
                Vector4 l = a + (y - a.Y) * k1;
                Vector4 r = (y < b.Y) ? a + (y - a.Y) * k2 : b + (y - b.Y) * k3;

                Vector4 worldL = aw + (y - a.Y) * worldKoeff1;
                Vector4 worldR = y < b.Y ? aw + (y - a.Y) * worldKoeff2 :
                                           bw + (y - b.Y) * worldKoeff3;


                // Нахождение нормали для левого и правого Y.
                Vector3 normalL = vertexNormalA + (y - a.Y) * vertexNormalKoeff1;
                Vector3 normalR = y < b.Y ? vertexNormalA + (y - a.Y) * vertexNormalKoeff2 :
                                                            vertexNormalB + (y - b.Y) * vertexNormalKoeff3;

                Vector2 textureL = textureA + (y - a.Y) * textureKoeff1;
                Vector2 textureR = y < b.Y ? textureA + (y - a.Y) * textureKoeff2 :
                                                            textureB + (y - b.Y) * textureKoeff3;

                if (l.X > r.X) {
                    (l, r) = (r, l);
                    (normalL, normalR) = (normalR, normalL);
                    (worldL, worldR) = (worldR, worldL);
                    (textureL, textureR) = (textureR, textureL);
                }

                Vector4 k = (r - l) / (r.X - l.X);
                Vector3 normalKoeff = (normalR - normalL) / (r.X - l.X);
                Vector4 worldKoeff = (worldR - worldL) / (r.X - l.X);
                Vector2 textureKoeff = (textureR - textureL) / (r.X - l.X);

                int left = Math.Max(0, (int) Math.Ceiling(l.X));
                int right = Math.Min(window_width, (int)Math.Ceiling(r.X));

                //надо что-то делить на w (всё)
                //
                //
                //
                //
                //

                Vector4 pz0 = l + (left - l.X) * k;
                float z0 = pz0.W;
                Vector2 uv0 = new Vector2(left, top);
                for (int x = left; x < right; x++) {
                    Vector4 p = l + (x - l.X) * k;
                    Vector4 pWorld = worldL + (x - l.X) * worldKoeff;

                    float t;
                    if (x == left || y == top)
                    {
                        t = 0;
                    }
                    else
                    {
                        t = (x - left) / (y - top);
                    }

                    float z1 = p.W;
                    /*Vector2 uv1 = new Vector2(x, y);

                    Vector2 uv2 = ((1 - t) * uv0 / z0 + t * uv1 / z1)
                                   / ((1 - t) * 1 / z0 + t * 1 / z1);
                    */

                    int index = (int)y * window_width + (int)x;
                    if (p.Z < zBuffer[index])
                    {
                        Vector3 normal = normalL + (x - l.X) * normalKoeff;
                        normal = Vector3.Normalize(normal);

                        Vector2 texture = (textureL + (x - l.X) * textureKoeff) / p.W;
                        //Vector2 texture = (textureL + (x - l.X) * textureKoeff) / pWorld.W;
                        //здесь потом поделить на w
                        // Цвет объекта.
                        Vector3 color = new Vector3(235, 163, 9);
                        if (ObjParser.diffuseMap != null)
                        {
                            System.Drawing.Color objColor = ObjParser.diffuseMap.GetPixel(
                                Convert.ToInt32(texture.X * (ObjParser.diffuseMap.Width - 1)), 
                                Convert.ToInt32((1 - texture.Y) * (ObjParser.diffuseMap.Height - 1)));
                            /*System.Drawing.Color objColor = ObjParser.diffuseMap.GetPixel(
                                Convert.ToInt32(textureNew.X * (ObjParser.diffuseMap.Width - 1)),
                                Convert.ToInt32((1 - textureNew.Y) * (ObjParser.diffuseMap.Height - 1)));*/
                            color = new Vector3(objColor.R, objColor.G, objColor.B);
                        }

                        // Цвет отражения.
                        Vector3 specular = new Vector3(212, 21, 21);
                        if (ObjParser.mirrorMap != null)
                        {
                            System.Drawing.Color spcColor = ObjParser.mirrorMap.GetPixel(
                                Convert.ToInt32(texture.X * (ObjParser.mirrorMap.Width - 1)), 
                                Convert.ToInt32((1 - texture.Y) * (ObjParser.mirrorMap.Height - 1)));
                            specular = new Vector3(spcColor.R, spcColor.G, spcColor.B);
                        }

                        // Нормаль
                        /*System.Drawing.Color normalColor = ObjParser.normalMap.GetPixel(
                                Convert.ToInt32(texture.X * (ObjParser.normalMap.Width - 1)),
                                Convert.ToInt32((1 - texture.Y) * (ObjParser.normalMap.Height - 1)));
                        normal = new Vector3(normalColor.R, normalColor.G, normalColor.B);
                        normal = Vector3.Normalize(normal * 2 - Vector3.One);*/

                        float[] ambientValues = AmbientLightning(color);

                        float[] diffuseValues = DiffuseLightning(color, normal, -lightDirection);

                        float[] specularValues = SpecularLightning(specular, 
                            Vector3.Normalize(eye - MatrixSolver.createFromVector4(pWorld)), -lightDirection, normal);

                        zBuffer[index] = p.Z;
                        byte* data = scan0 + (int)y * bData.Stride + (int)x * bitsPerPixel / 8;

                        byte B = (byte)(Math.Min(lineColour.B * (ambientValues[2]), 255));
                        byte G = (byte)(lineColour.G * (ambientValues[1]));
                        byte R = (byte)(lineColour.R * (ambientValues[0]));

                        data[0] = (byte) Math.Min((ambientValues[2] + diffuseValues[2] + specularValues[2]), 255);
                        data[1] = (byte) Math.Min((ambientValues[1] + diffuseValues[1] + specularValues[1]), 255);
                        data[2] = (byte) Math.Min((ambientValues[0] + diffuseValues[0] + specularValues[0]), 255);

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
            verteces_changeable = new List<Vector4>();
            return Draw();
        }

        private bool IsBackFace(List<List<int>> face)
        {
            Vector3 viewVector = ToVector3(verteces_view[face[0][0] - 1]) - eye;

            return Vector3.Dot(CalculateNormal(face), viewVector) <= 0;
        }
        
        private Vector3 ToVector3(Vector4 vector)
        {
            return new Vector3(vector.X, vector.Y, vector.Z);
        }

        private Vector3 CalculateNormal(List<List<int>> face)
        {
            Vector3 v1 = Vector3.Normalize(ToVector3(verteces_view[face[1][0] - 1]) - ToVector3(verteces_view[face[0][0] - 1]));
            Vector3 v2 = Vector3.Normalize(ToVector3(verteces_view[face[2][0] - 1]) - ToVector3(verteces_view[face[0][0] - 1]));
            return Vector3.Normalize(Vector3.Cross(v2, v1));
        }

        private float CalculateLightIntensity(Vector3 normal, Vector3 lightDirection)
        {
            float scalar = Vector3.Dot(normal * -1, lightDirection * -1);
            if (scalar - 1 > 0) return 1;
            return Math.Max(scalar, 0);
        }

        private float[] AmbientLightning(Vector3 lightColor)
        {
            float[] values = new float[3];

            values[0] = (float)(lightColor.X * ambientLightIntensity);
            values[1] = (float)(lightColor.Y * ambientLightIntensity);
            values[2] = (float)(lightColor.Z * ambientLightIntensity);

            return values;

        }


        private float[] DiffuseLightning(Vector3 lightColor, Vector3 normal, Vector3 lightDirection)
        {
            float[] values = new float[3];
            float scalar = Math.Max(CalculateLightIntensity(normal, lightDirection), 0) * diffuseLightIntensity;
            values[0] = (float)(lightColor.X * scalar);
            values[1] = (float)(lightColor.Y * scalar);
            values[2] = (float)(lightColor.Z * scalar);
            return values;
        }

        private float[] SpecularLightning(Vector3 specularColor, Vector3 View, Vector3 lightDirection, Vector3 normal)
        {
            Vector3 reflection = Vector3.Normalize(Vector3.Reflect(-lightDirection, normal));
            float RV = Math.Max(Vector3.Dot(reflection, View), 0);

            float[] values = new float[3];
            float temp = (float)Math.Pow(RV, glossFactor);

            //values[0] = (specularFactor * temp);
            //values[1] = (specularFactor * temp);
            //values[2] = (specularFactor * temp);
            values[0] = (int)(specularLightIntensity * temp * specularColor.X);
            values[1] = (int)(specularLightIntensity * temp * specularColor.Y);
            values[2] = (int)(specularLightIntensity * temp * specularColor.Z);

            return values;
        }
    }
}
