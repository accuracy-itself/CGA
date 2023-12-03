using CGA_FIRST.modules;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace CGA_FIRST
{
    public partial class Form1 : Form
    {
        private ObjParser objParser;
        private Drawer drawer;
        private bool mousePressed = false;
        private Point mouseInitPos;
        private const double ROTATION_INDEX = 0.1;
        

        public Form1()
        {
            InitializeComponent();

            objectPB.MouseWheel += new MouseEventHandler(objectPB_MouseWheel);

            objParser = new ObjParser();
            //objParser.parseFile("images/cube.obj");
            //objParser.parseFile("images/ball.obj");
            //objParser.parseFile("images/dog.obj");
            //objParser.parseFile("images/mancubus.obj");
            //objParser.parseFile("images/doomslayer.obj");
            //objParser.parseFile("images/ShovelKnight/Model.obj");
            //objParser.parseTextures("images/ShovelKnight/textures/diffuse.png", 
            //                      "images/ShovelKnight/textures/specular.png", 
            //                    "images/ShovelKnight/textures/normal.png");
            objParser.parseFile("images/head/Head.obj");
            objParser.parseTextures("images/head/diffuse.png",
                                    "images/head/specular.png",
                                    "images/head/normal.png");
            drawer = new Drawer(objectPB.Width, objectPB.Height, objParser.verteces, objParser.faces, objParser.normals);

            objectPB.Image = drawer.SetUpCamera();
        }

        private void objectPB_MouseWheel(object sender, MouseEventArgs e)
        {
            if (e.Delta > 0)
            {
                drawer.ZoomIn();
                objectPB.Image = drawer.Draw();
            }
            else
            {
                drawer.ZoomOut();
                objectPB.Image = drawer.Draw();
            }
        }

        private void objectPB_MouseDown(object sender, MouseEventArgs e)
        {
            mousePressed = true;
            mouseInitPos = e.Location;
        }

        private void objectPB_MouseUp(object sender, MouseEventArgs e)
        {
            mousePressed = false;
        }

        private void objectPB_MouseMove(object sender, MouseEventArgs e)
        {
            if (mousePressed)
            {
                Point path = new Point(e.Location.X - mouseInitPos.X, e.Location.Y - mouseInitPos.Y);
                MatrixRotater.Rotate(path);
                objectPB.Image = drawer.Draw();
            }
            mouseInitPos = e.Location;
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            int coef = 5;
            switch (e.KeyCode)
            {
                case Keys.W:
                    MatrixRotater.Rotate(ROTATION_INDEX, 0, 0);
                    break;
                case Keys.S:
                    MatrixRotater.Rotate(-ROTATION_INDEX, 0, 0);
                    break;

                case Keys.A:
                    MatrixRotater.Rotate(0, ROTATION_INDEX, 0);
                    break;
                case Keys.D:
                    MatrixRotater.Rotate(0, -ROTATION_INDEX, 0);
                    break;

                case Keys.Q:
                    MatrixRotater.Rotate(0, 0, ROTATION_INDEX);
                    break;
                case Keys.E:
                    MatrixRotater.Rotate(0, 0, -ROTATION_INDEX);
                    break;

                case Keys.F:
                    drawer.changeTranslationMatrix((float)ROTATION_INDEX*coef, 0, 0);
                    break;
                case Keys.H:
                    drawer.changeTranslationMatrix(-(float)ROTATION_INDEX * coef, 0, 0);
                    break;

                case Keys.T:
                    drawer.changeTranslationMatrix(0, -(float)ROTATION_INDEX * coef, 0);
                    break;
                case Keys.G:
                    drawer.changeTranslationMatrix(0, (float)ROTATION_INDEX * coef, 0);
                    break;

                case Keys.R:
                    drawer.changeTranslationMatrix(0, 0, -(float)ROTATION_INDEX * coef);
                    break;
                case Keys.Y:
                    drawer.changeTranslationMatrix(0, 0, (float)ROTATION_INDEX * coef);
                    break;
            }

            objectPB.Image = drawer.Draw();
        }
    }
}
