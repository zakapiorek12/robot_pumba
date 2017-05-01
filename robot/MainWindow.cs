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
        private Camera camera;
        private Mesh[] mesh;

        private int programId;
        private int vertexShader;
        private int pixelShader;

        private int projectionMatrixLocation;
        private int cameraviewMatrixLocation;
        private int objectMatrixLocation;
        private int cameraModelMatrixLocation;

        private int ambientCoefficientLocation;
        private int lightPositionLocation;
        private int lightColorLocation;

        private int surfaceColorLocation;
        private int materialSpecExponentLocation;
        private int specularColorLocation;

        private float ambientCoefficient = 1.0f;
        private Vector3 lightColor = new Vector3(0.9f, 0.8f, 0.8f);
        private Vector3 lightPosition = new Vector3(-1.0f, 1.0f, 0.0f);

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

            projectionMatrixLocation = GL.GetUniformLocation(programId, "projection_matrix");
            cameraviewMatrixLocation = GL.GetUniformLocation(programId, "cameraview_matrix");
            cameraModelMatrixLocation = GL.GetUniformLocation(programId, "cameraModel_matrix");
            objectMatrixLocation = GL.GetUniformLocation(programId, "object_matrix");

            ambientCoefficientLocation = GL.GetUniformLocation(programId, "ambientCoefficient");
            lightPositionLocation = GL.GetUniformLocation(programId, "lightPosition");
            lightColorLocation = GL.GetUniformLocation(programId, "lightColor");

            surfaceColorLocation = GL.GetUniformLocation(programId, "surfaceColor");
            materialSpecExponentLocation = GL.GetUniformLocation(programId, "materialSpecExponent");
            specularColorLocation = GL.GetUniformLocation(programId, "specularColor");
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

            BindCameraAndProjectionToShaders();
            BindLightDataToShaders();

            foreach (Mesh m in mesh)
            {
                m.BindVAO();
                BindMeshMaterialDataToShaders(m);

                GL.UniformMatrix4(objectMatrixLocation, false, ref m.ResultMatrix);
                GL.DrawElements(PrimitiveType.Triangles, m.IndexBuffer.Length, DrawElementsType.UnsignedInt, 0);
                GL.Flush();
            }

            SwapBuffers();
        }

        private void BindCameraAndProjectionToShaders()
        {
            GL.UniformMatrix4(cameraviewMatrixLocation, false, ref camera.ResultMatrix);
            Matrix4 cameraModelMatrix = camera.ResultMatrix.Inverted();
            GL.UniformMatrix4(cameraModelMatrixLocation, false, ref cameraModelMatrix);
            GL.UniformMatrix4(projectionMatrixLocation, false, ref projectionMatrix);
        }
        private void BindLightDataToShaders()
        {
            GL.Uniform3(lightColorLocation, lightColor);
            GL.Uniform3(lightPositionLocation, lightPosition);
            GL.Uniform1(ambientCoefficientLocation, ambientCoefficient);
        }

        private void BindMeshMaterialDataToShaders(Mesh m)
        {
            GL.Uniform1(materialSpecExponentLocation, m.materialSpecExponent);
            GL.Uniform3(specularColorLocation, m.materialSpecularColor);
            GL.Uniform3(surfaceColorLocation, m.surfaceColor);
        }
    }
}
