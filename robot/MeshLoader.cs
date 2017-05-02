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

        public Mesh GetDoubleSidedRectangleMesh(float width, float height, Vector3 surfaceColor)
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
            return m;
        }

        private Mesh LoadMesh(string path)
        {
            List<Vector3> vertexes = new List<Vector3>();
            List<Normalized> normalized = new List<Normalized>();
            List<uint> indexes = new List<uint>();
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
                    for (int j = 0; j < 3; j++)
                    {
                        uint a = uint.Parse(abc[j]);
                        indexes.Add(a);
                    }
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

            return new Mesh(vertexes.ToArray(), normalized.ToArray(), indexes.ToArray(), neighbours.ToArray());
        }
    }
}
