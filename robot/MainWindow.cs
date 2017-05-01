using System;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System.Drawing;
using System.IO;
using System.Linq;

namespace robot
{
    partial class MainWindow : GameWindow
    {
        private Matrix4 projectionMatrix;
        private int programId;
        private int vertexShader;
        private int pixelShader;
        private Camera camera;
        private Mesh[] mesh;
        private int projectionMatrixLocation;
        private int cameraviewMatrixLocation;
        private int objectMatrixLocation;

        public MainWindow() : base()
        {
            InitEvents();
            camera = new Camera();
            MeshLoader ml = new MeshLoader();
            mesh = ml.GetMesh();
            Title = "PUMBA";
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            LoadShaders();
            CreateProjectionMatrix();
            GL.EnableClientState(ArrayCap.VertexArray);
        }

        private void CreateProjectionMatrix()
        {
            projectionMatrix = Matrix4.Identity;
            projectionMatrix[2, 2] = 0;
            projectionMatrix[2, 3] = .25f;
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            GL.Viewport(this.ClientRectangle);
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);
            Render();
        }

        private void LoadShaders()
        {
            programId = GL.CreateProgram();

            string root = RootFolder();

            LoadVertexShader(root);
            LoadPixelShader(root);

            GL.LinkProgram(programId);
            GL.UseProgram(programId);
        }

        private string RootFolder()
        {
            vertexShader = GL.CreateShader(ShaderType.VertexShader);
            Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);
            string root = Directory.GetCurrentDirectory();
            root += "\\shaders";
            return root;
        }

        private void LoadVertexShader(string root)
        {
            int statusCode;
            string info;
            string path = root + "\\VS.vert";
            GL.ShaderSource(vertexShader, File.ReadAllText(path));
            GL.CompileShader(vertexShader);
            info = GL.GetShaderInfoLog(vertexShader);
            GL.GetShader(vertexShader, ShaderParameter.CompileStatus, out statusCode);
            if (statusCode != 1) throw new ApplicationException(info);
            GL.AttachShader(programId, vertexShader);
        }

        private void LoadPixelShader(string root)
        {
            int statusCode;
            string info;
            pixelShader = GL.CreateShader(ShaderType.FragmentShader);
            Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);
            string path = root + "\\PS.vert";
            GL.ShaderSource(pixelShader, File.ReadAllText(path));
            GL.CompileShader(pixelShader);
            info = GL.GetShaderInfoLog(pixelShader);
            GL.GetShader(pixelShader, ShaderParameter.CompileStatus, out statusCode);
            if (statusCode != 1) throw new ApplicationException(info);
            GL.AttachShader(programId, pixelShader);
        }

        private void Render()
        {
            GL.Clear(ClearBufferMask.ColorBufferBit);
            GL.ClearColor(Color.Black);

            BindMartixes();

            foreach (Mesh m in mesh)
            {
                m.BindVAO();
                GL.UniformMatrix4(projectionMatrixLocation, false, ref projectionMatrix);//shader
                GL.UniformMatrix4(objectMatrixLocation, false, ref m.ResultMatrix);//shader
                GL.DrawElements(PrimitiveType.Triangles, m.IndexBuffer.Length, DrawElementsType.UnsignedInt, 0);
                GL.Flush();
            }

            SwapBuffers();
        }

        private void BindMartixes()
        {
            projectionMatrixLocation = GL.GetUniformLocation(programId, "projection_matrix");//shader
            cameraviewMatrixLocation = GL.GetUniformLocation(programId, "cameraview_matrix");//shader
            objectMatrixLocation = GL.GetUniformLocation(programId, "object_matrix");//shader
            GL.UniformMatrix4(cameraviewMatrixLocation, false, ref camera.ResultMatrix);//shader
        }
    }
}
