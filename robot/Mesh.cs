using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;

namespace robot
{
    struct Normalized
    {
        public static readonly int SizeInBytes = 2 * Vector3.SizeInBytes;
        public Vector3 vertex;
        public Vector3 normal;
    }

    struct Neighbour
    {
        public uint firstVertex;
        public uint secondVertex;
        public uint firstTriangle;
        public uint secondTriangle;
    }

    class Mesh
    {
        public Matrix4 ResultMatrix;

        public uint[] IndexBuffer { get; set; }
        public Vector3[] VertexBuffer { get; set; }
        public Normalized[] NormalizedVertexBuffer { get; set; }
        public Neighbour[] Neighbourhood { get; set; }

        public Mesh()
        {
            ResultMatrix = Matrix4.Identity;
        }
    }
}
