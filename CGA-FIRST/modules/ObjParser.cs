using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;

namespace CGA_FIRST.modules
{
    public class ObjParser
    {
        public List<double[]> vertexes = new List<double[]>();
        public List<double[]> textures = new List<double[]>();
        public List<Vector3> normals = new List<Vector3>();
        public List<List<List<int>>> faces = new List<List<List<int>>>();        

        public void parseFile(string path)
        {
            string[] lines = File.ReadAllLines(path);
            var fmt = new NumberFormatInfo
            {
                NegativeSign = "-"
            };
            foreach (string line in lines)
            {

                string[] literals = Regex.Split(line, @"\s+");
                switch (literals[0])
                {
                    case "v":
                        if (literals.Length == 4)
                        {
                            vertexes.Add(new double[] { double.Parse(literals[1], fmt), double.Parse(literals[2],fmt),
                                                     double.Parse(literals[3], fmt), 1});
                        }
                        else
                        {
                            vertexes.Add(new double[] { double.Parse(literals[1]), double.Parse(literals[2]),
                                                     double.Parse(literals[3]), double.Parse(literals[4])});
                        }
                        break;
                    case "f":
                        if (line.Contains('/'))
                        {
                            List<List<int>> topLevelList = new List<List<int>>();
                            for (int i = 1; i < literals.Length; i++)
                            {
                                string[] numbers = literals[i].Split('/');
                                List<int> tempList = new List<int>();
                                foreach (string numb in numbers)
                                {
                                    if (numb.Length != 0)
                                        tempList.Add(int.Parse(numb));
                                    else
                                        tempList.Add(0);
                                }
                                topLevelList.Add(tempList);
                            }
                            faces.Add(topLevelList);
                        }
                        else
                        {
                            List<List<int>> topLevelList = new List<List<int>>();
                            for (int i = 1; i < literals.Length; i++)
                            {
                                List<int> tempList = new List<int>
                                {
                                    int.Parse(literals[i])
                                };
                                topLevelList.Add(tempList);
                            }
                            faces.Add(topLevelList);
                        }
                        break;
                    case "vn":
                        {
                            normals.Add(new Vector3 ((float)double.Parse(literals[1], fmt), 
                                                     (float)double.Parse(literals[2], fmt),
                                                     (float)double.Parse(literals[3], fmt)));
                        }
                        break;
                }
            }
        } 
    }
}
