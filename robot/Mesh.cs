using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace robot
{
    public struct Normalized
    {
        public Vector3 vertex;
        public Vector3 normal;
    }

    public struct Neighbour
    {
        public uint firstVertex;
        public uint secondVertex;
        public uint firstTriangle;
        public uint secondTriangle;
    }

    public class Mesh
    {
        public Matrix4 ResultMatrix;

        public uint[] IndexBuffer { get; set; }
        public Vector3[] VertexBuffer { get; set; }
        public Normalized[] NormalizedVertexBuffer { get; set; }
        public Neighbour[] Neighbourhood { get; set; }

        private int VAOId;
        private int positionVbo;
        private int normalsVbo;
        private int indicesVbo;

        public Vector3 surfaceColor = new Vector3(0.5f, 0.5f, 0.5f);
        public float materialSpecExponent = 64f;
        public Vector3 materialSpecularColor = new Vector3(1.0f, 0.0f, 0.0f);

        public Mesh(Vector3[] vertices, Normalized[] normalized, uint[] indices, Neighbour[] neighbours)
        {
            this.VertexBuffer = vertices;
            this.NormalizedVertexBuffer = normalized;
            this.IndexBuffer = indices;
            this.Neighbourhood = neighbours;
            ResultMatrix = Matrix4.Identity;

            InitializeVAO();
        }

        public void BindVAO()
        {
            GL.BindVertexArray(VAOId);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, indicesVbo);
        }

        private void InitializeVAO()
        {
            CreateVbos();
            CreateElementBufferObject();
            
            GL.GenVertexArrays(1, out VAOId);
            GL.BindVertexArray(VAOId);

            //element array buffer jest wyjatkiem - nie trzeba podawac layoutu - wystarczy zbindowac buffer przy bindowaniu stanu do danego VAO

            GL.BindBuffer(BufferTarget.ArrayBuffer, positionVbo);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 0, 0);

            GL.BindBuffer(BufferTarget.ArrayBuffer, normalsVbo);
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 0, 0);

            GL.EnableVertexAttribArray(0);
            GL.EnableVertexAttribArray(1);

            GL.BindVertexArray(0);
        }

        private void CreateVbos()
        {
            positionVbo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, positionVbo);
            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(Vector3.SizeInBytes * NormalizedVertexBuffer.Length),
                NormalizedVertexBuffer.Select((n) => n.vertex).ToArray(), BufferUsageHint.StaticDraw);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

            normalsVbo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, normalsVbo);
            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(Vector3.SizeInBytes * NormalizedVertexBuffer.Length),
                NormalizedVertexBuffer.Select((n) => n.normal).ToArray(), BufferUsageHint.StaticDraw);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        }

        private void CreateElementBufferObject()
        {
            indicesVbo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, indicesVbo);
            GL.BufferData(BufferTarget.ElementArrayBuffer, (IntPtr)(sizeof(uint) * IndexBuffer.Length),
                IndexBuffer, BufferUsageHint.StaticDraw);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
        }
    }
}
