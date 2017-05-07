using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using OpenTK;
using System.Globalization;

namespace robot
{
    class MeshLoader
    {
        Mesh[] mesh;

        public MeshLoader()
        {
            mesh = new Mesh[6];
        }

        internal Mesh[] GetRobotMesh()
        {
            for (int i = 0; i < 6; i++)
            {
                string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"mesh\mesh" + (i + 1).ToString() + ".txt");
                mesh[i] = LoadMesh(path);
            }
            return mesh;
        }

        public Mesh GetDoubleSidedRectangleMesh(float width, float height, Vector4 surfaceColor)
        {
            Vector3[] vertices = new Vector3[]
            {
                new Vector3(-width/2.0f, -height/2.0f, 0.0f),
                new Vector3(-width/2.0f, height/2.0f, 0.0f),
                new Vector3(width/2.0f, -height/2.0f, 0.0f),
                new Vector3(width/2.0f, height/2.0f, 0.0f)
            };
            Vector3 faceNormal = Vector3.UnitZ;
            Normalized[] normalized = new Normalized[]
            {
                //front face
                new Normalized() {normal = faceNormal, vertex = vertices[0]},
                new Normalized() {normal = faceNormal, vertex = vertices[1]},
                new Normalized() {normal = faceNormal, vertex = vertices[2]},
                new Normalized() {normal = faceNormal, vertex = vertices[2]},
                new Normalized() {normal = faceNormal, vertex = vertices[1]},
                new Normalized() {normal = faceNormal, vertex = vertices[3]},

                //back face
                new Normalized() {normal = -faceNormal, vertex = vertices[2]},
                new Normalized() {normal = -faceNormal, vertex = vertices[1]},
                new Normalized() {normal = -faceNormal, vertex = vertices[0]},
                new Normalized() {normal = -faceNormal, vertex = vertices[3]},
                new Normalized() {normal = -faceNormal, vertex = vertices[1]},
                new Normalized() {normal = -faceNormal, vertex = vertices[2]}
            };
            uint[] indices = new uint[normalized.Length];
            for (uint i = 0; i < indices.Length; i++)
                indices[i] = i;

            Mesh m = new Mesh(vertices, normalized, indices, null);
            m.surfaceColor = surfaceColor;
            m.materialDiffuseSpecularColor = surfaceColor.Xyz;
            return m;
        }

        public Mesh GetShadowQuadMesh(Vector3 vert1, Vector3 vert2, Vector3 vert3, Vector3 vert4)
        {
            Vector3[] vertices = new Vector3[]
            {
                vert1, vert2, vert3, vert4
            };
            Vector3 faceNormal = Vector3.UnitZ; //not used for shadow quads
            Normalized[] normalized = new Normalized[4];
            for (int i = 0; i < 4; i++)
                normalized[i] = new Normalized() {normal = faceNormal, vertex = vertices[i]};
            uint[] indices = new uint[]
            {
                0, 1, 2, 2, 1, 3
            };

            Mesh m = new Mesh(vertices, normalized, indices, null);
            return m;
        }

        public Mesh GetCylinderMesh(float radius, float height, Vector4 surfaceColor, int divisions)
        {
            List<Vector3> verticesList = new List<Vector3>();
            List<uint> indicesList = new List<uint>();
            uint currentInd = 0;

            float angleStep = (float) (Math.PI*2.0f/divisions);
            for (int i = 0; i <= divisions; i++)
            {
                float angle = i*angleStep;
                float x = (float) (radius*Math.Cos(angle));
                float z = (float) (radius*Math.Sin(angle));

                if (i != divisions)
                {
                    verticesList.Add(new Vector3(x, -height/2.0f, z));
                    verticesList.Add(new Vector3(x, height/2.0f, z));
                }

                if (i == divisions)
                {
                    indicesList.Add(currentInd);
                    indicesList.Add(currentInd + 1);
                    indicesList.Add(0);
                    indicesList.Add(0);
                    indicesList.Add(currentInd + 1);
                    indicesList.Add(1);
                }
                else if (i > 0)
                {
                    indicesList.Add(currentInd);
                    indicesList.Add(currentInd+1);
                    indicesList.Add(currentInd+2);
                    indicesList.Add(currentInd+2);
                    indicesList.Add(currentInd+1);
                    indicesList.Add(currentInd+3);
                    currentInd += 2;
                }
            }
            Vector3[] normals = CalculateNormals(verticesList.ToArray(), indicesList.ToArray());
            Normalized[] normalized = new Normalized[indicesList.Count];
            for(int i = 0; i < indicesList.Count; i++)
                normalized[i] = new Normalized() {normal = normals[indicesList[i]], vertex = verticesList[(int) indicesList[i]]};

            uint[] indices = new uint[normalized.Length];
            for (uint i = 0; i < indices.Length; i++)
                indices[i] = i;
            Mesh m = new Mesh(verticesList.ToArray(), normalized, indices, null);
            m.surfaceColor = surfaceColor;
            m.materialDiffuseSpecularColor = surfaceColor.Xyz;
            return m;
        }

        protected Vector3[] CalculateNormals(Vector3[] verts, uint[] indices)
        {
            Vector3[] result = new Vector3[verts.Length];
            int j = 0;
            Dictionary<uint, Tuple<uint, Vector3>> normals = new Dictionary<uint, Tuple<uint, Vector3>>();
            for (int i = 0; i < indices.Length; i++)
            {
                if (i != 0 && i % 3 == 0)
                    j += 3;
                Vector3 vec1 = new Vector3(verts[indices[j + 1]] - verts[indices[j + 2]]);
                Vector3 vec2 = new Vector3(verts[indices[j + 1]] - verts[indices[j]]);
                Vector3 averageNormal = Vector3.Cross(vec1, vec2);
                if (!normals.ContainsKey(indices[i]))
                    normals.Add(indices[i], new Tuple<uint, Vector3>(1, averageNormal));
                else
                {
                    Tuple<uint, Vector3> old = normals[indices[i]];
                    normals[indices[i]] = new Tuple<uint, Vector3>(old.Item1 + 1, old.Item2 + averageNormal);
                }
            }
            for (uint i = 0; i < normals.Count; i++)
            {
                Tuple<uint, Vector3> norm = normals[i];
                result[i] = norm.Item2 / norm.Item1;
                result[i].Normalize();
            }

            return result;
        }

        private Mesh LoadMesh(string path)
        {
            List<Vector3> vertexes = new List<Vector3>();
            List<Normalized> normalized = new List<Normalized>();
            List<uint> indexes = new List<uint>();
            List<Triangle> triangles = new List<Triangle>();
            List<Neighbour> neighbours = new List<Neighbour>();

            NumberFormatInfo nfi = new NumberFormatInfo();
            nfi.NegativeSign = "-";

            using (FileStream fileStream = File.OpenRead(path))
            using (StreamReader streamReader = new StreamReader(fileStream))
            {
                string line = streamReader.ReadLine();
                uint vertexCount = uint.Parse(line);
                for (int i = 0; i < vertexCount; i++)
                {
                    line = streamReader.ReadLine();
                    string[] xyz = line.Split(' ');
                    float x = float.Parse(xyz[0], nfi);
                    float y = float.Parse(xyz[1], nfi);
                    float z = float.Parse(xyz[2], nfi);
                    vertexes.Add(new Vector3(x, y, z));
                }

                line = streamReader.ReadLine();
                uint vertexWithNormalCount = uint.Parse(line);
                for (int i = 0; i < vertexWithNormalCount; i++)
                {
                    line = streamReader.ReadLine();
                    string[] nxyz = line.Split(' ');
                    int n = int.Parse(nxyz[0]);
                    float x = float.Parse(nxyz[1], nfi);
                    float y = float.Parse(nxyz[2], nfi);
                    float z = float.Parse(nxyz[3], nfi);
                    normalized.Add(new Normalized
                    {
                        normal = new Vector3(x, y, z),
                        vertex = vertexes[n]
                    });
                }

                line = streamReader.ReadLine();
                uint trainglesCount = uint.Parse(line);
                for (int i = 0; i < trainglesCount; i++)
                {
                    line = streamReader.ReadLine();
                    string[] abc = line.Split(' ');
                    Triangle tri = new Triangle()
                    {
                        firstVertex = uint.Parse(abc[0]),
                        secondVertex = uint.Parse(abc[1]),
                        thirdVertex = uint.Parse(abc[2])
                    };
                    triangles.Add(tri);
                    indexes.Add(tri.firstVertex);
                    indexes.Add(tri.secondVertex);
                    indexes.Add(tri.thirdVertex);
                }

                line = streamReader.ReadLine();
                uint neighboursCount = uint.Parse(line);
                for (int i = 0; i < neighboursCount; i++)
                {

                    line = streamReader.ReadLine();
                    string[] vvtt = line.Split(' ');
                    uint v1 = uint.Parse(vvtt[0]);
                    uint v2 = uint.Parse(vvtt[1]);
                    uint t1 = uint.Parse(vvtt[2]);
                    uint t2 = uint.Parse(vvtt[3]);
                    neighbours.Add(new Neighbour
                    {
                        firstVertex = v1,
                        secondVertex = v2,
                        firstTriangle = t1,
                        secondTriangle = t2
                    });
                }
            }

            return new Mesh(vertexes.ToArray(), normalized.ToArray(), indexes.ToArray(), neighbours.ToArray(), triangles.ToArray());
        }
    }
}
