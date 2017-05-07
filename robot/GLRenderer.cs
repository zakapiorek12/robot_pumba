using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace robot
{
    public class GLRenderer
    {
        private Matrix4 projectionMatrix;

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

        private int isRectangle;

        private float ambientCoefficient = 1.0f;
        private Vector3 lightColor = new Vector3(0.9f, 0.8f, 0.8f);
        private Vector3 lightPosition = new Vector3(-1.0f, 1.0f, 1.0f);

        private static List<Mesh> meshesToDraw = new List<Mesh>();
        private static List<AnimatedObject> animatedObjects = new List<AnimatedObject>();
        private bool previous;
        private Mesh rectangle;
        private Reflection reflection;

        public GLRenderer(int viewPortWidth, int viewportHeight)
        {
            LoadShaders("");
            CreateProjectionMatrix(viewPortWidth, viewportHeight);
            CreateScene();
        }

        private void LoadShaders(string name)
        {
            GL.DeleteProgram(programId);
            programId = GL.CreateProgram();

            LoadShadersUsingName(name);

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

            isRectangle = GL.GetUniformLocation(programId, "isRectangle");
        }

        private void LoadShadersUsingName(string name)
        {
            string root = RootFolder();

            LoadVertexShader(root, name);
            LoadPixelShader(root, name);
        }

        private string RootFolder()
        {
            string root = Directory.GetCurrentDirectory();
            root += "\\shaders";
            return root;
        }

        private void LoadVertexShader(string root, string name)
        {
            int statusCode;
            string info;
            vertexShader = GL.CreateShader(ShaderType.VertexShader);
            Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);
            string path = root + "\\VS" + name + ".vert";
            GL.ShaderSource(vertexShader, File.ReadAllText(path));
            GL.CompileShader(vertexShader);
            info = GL.GetShaderInfoLog(vertexShader);
            GL.GetShader(vertexShader, ShaderParameter.CompileStatus, out statusCode);
            if (statusCode != 1) throw new ApplicationException(info);
            GL.AttachShader(programId, vertexShader);
        }

        private void LoadPixelShader(string root, string name)
        {
            int statusCode;
            string info;
            pixelShader = GL.CreateShader(ShaderType.FragmentShader);
            Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);
            string path = root + "\\PS" + name + ".vert";
            GL.ShaderSource(pixelShader, File.ReadAllText(path));
            GL.CompileShader(pixelShader);
            info = GL.GetShaderInfoLog(pixelShader);
            GL.GetShader(pixelShader, ShaderParameter.CompileStatus, out statusCode);
            if (statusCode != 1) throw new ApplicationException(info);
            GL.AttachShader(programId, pixelShader);
        }

        public void CreateProjectionMatrix(int viewportWidth, int viewportHeight)
        {
            float far = (float)25.0f;
            float near = (float)0.01f;
            float fov = (float)(90f / 180f * Math.PI);
            float e = (float)(1f / Math.Tan(fov / 2f));
            float a = (float)viewportHeight / (float)viewportWidth;

            projectionMatrix = new Matrix4(
                e, 0, 0, 0,
                0, e / a, 0, 0,
                0, 0, -(far + near) / (far - near), -2f * far * near / (far - near),
                0, 0, -1, 0);
            projectionMatrix.Transpose();
        }

        private void CreateScene()
        {
            MeshLoader ml = new MeshLoader();
            rectangle = ml.GetDoubleSidedRectangleMesh(1.5f, 1.0f, new Vector4(0.8f, 1.0f, 1.0f, 0.5f));
            rectangle.ModelMatrix = Matrix4.CreateRotationY((float)(Math.PI / 2.0f)) *
                                     Matrix4.CreateRotationZ((float)(30.0f * Math.PI / 180.0f)) *
                                     Matrix4.CreateTranslation(-1.5f, 0.0f, 0.0f);
            rectangle.CalculateInverted();
            Robot robot = new Robot(rectangle);

            reflection = new Reflection(robot, rectangle);
            reflection.AddOnScene();
            AddMeshToDraw(rectangle);
            robot.AddOnScene();

            float floorYOffset = -1.0f;
            Mesh floor = ml.GetDoubleSidedRectangleMesh(6.0f, 6.0f, new Vector4(0.8f, 0.8f, 0.8f, 1.0f));
            floor.ModelMatrix = Matrix4.CreateRotationX((float) (Math.PI/2.0f)) * Matrix4.CreateTranslation(0, floorYOffset, 0);
            GLRenderer.AddMeshToDraw(floor);

            Mesh cylinder = ml.GetCylinderMesh(0.5f, 2.5f, new Vector4(0, 0, 1, 1), 360);
            cylinder.ModelMatrix = Matrix4.CreateRotationX((float) (Math.PI/2.0f))*
                                   Matrix4.CreateTranslation(1.5f, 0.51f + floorYOffset, 0.0f);
            AddMeshToDraw(cylinder);
        }

        public void DoScene(float deltaTime, Camera camera)
        {
            foreach (var anim in animatedObjects)
                anim.DoAnimation(deltaTime);

            Render(camera);
        }

        private void Render(Camera camera)
        {
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);
            GL.ClearColor(Color.Black);
            GL.Enable(EnableCap.DepthTest);

            GL.Enable(EnableCap.Blend);

            GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);


            Stencil();
            foreach (Mesh m in meshesToDraw)
            {
                if (previous != m.MainObject)
                {
                    if (previous = m.MainObject)
                    {
                        LoadShaders("");
                        //GL.ClearDepth(1);
                        //GL.DepthFunc(DepthFunction.Less);
                        //GL.Clear(ClearBufferMask.DepthBufferBit);
                    }
                    else
                    {
                        LoadShaders("_Plate");
                    }

                    BindCameraAndProjectionToShaders(camera);
                    BindLightDataToShaders();
                }

                DrawMesh(m);
            }

            GL.Flush();
        }

        private void DrawMesh(Mesh m)
        {
            m.BindVAO();
            BindMeshMaterialDataToShaders(m);

            GL.UniformMatrix4(objectMatrixLocation, false, ref m.ModelMatrix);
            GL.DrawElements(PrimitiveType.Triangles, m.IndexBuffer.Length, DrawElementsType.UnsignedInt, 0);
        }

        private void Stencil()
        {
            //GL.Enable(EnableCap.StencilTest);
            ////GL.ClearStencil(0);
            //GL.Clear(ClearBufferMask.StencilBufferBit);
            ////GL.ColorMask(false, false, false, false);
            //GL.StencilFunc(StencilFunction.Always, 1, 1);
            //GL.StencilOp(StencilOp.Keep, StencilOp.Keep, StencilOp.Replace);

            //// Draw the stencil texture
            ////GL.BindTexture(TextureTarget.Texture2D, stencilBuffer);

            //GL.Disable(EnableCap.DepthTest);
            //Vector4 prev = rectangle.surfaceColor;
            //rectangle.surfaceColor = new Vector4(0.3f, 0.8f, 0.5f, 0.6f);    // change color - test
            //DrawMesh(rectangle);
            //rectangle.surfaceColor = prev;
            //GL.Enable(EnableCap.DepthTest);

            //GL.Disable(EnableCap.StencilTest);
            //GL.StencilFunc(StencilFunction.Equal, 1, 1);
            //GL.StencilMask(0x00);
            //GL.ColorMask(true, true, true, true);

            //-------------------------------------------------------------------------

            //GL.Enable(EnableCap.StencilTest);
            //GL.StencilMask(0);
            //GL.StencilFunc(StencilFunction.Always, 1, 1);
            //GL.StencilOp(StencilOp.Replace, StencilOp.Replace, StencilOp.Replace);
            ////GL.DepthMask(false);
            //GL.Clear(ClearBufferMask.StencilBufferBit);

            ////glDrawArrays(GL_TRIANGLES, 36, 6);
            //DrawMesh(rectangle);

            //GL.StencilFunc(StencilFunction.Equal, 0, 1);
            //GL.StencilOp(StencilOp.Keep, StencilOp.Keep, StencilOp.Keep);
            //GL.DepthMask(true);

            //-----------------------------------------------------------------------------------------

            //GL.ClearDepth(1);
            //GL.DepthFunc(DepthFunction.Always);

            //GL.Uniform1(isRectangle, 1);
            //DrawMesh(rectangle);
            //GL.Uniform1(isRectangle, 0);

            //GL.DepthFunc(DepthFunction.Greater);

            //////////
            DrawMesh(rectangle);
        }

        private void BindCameraAndProjectionToShaders(Camera camera)
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
            GL.Uniform3(specularColorLocation, m.materialDiffuseSpecularColor);
            GL.Uniform4(surfaceColorLocation, m.surfaceColor);
        }

        public static void AddMeshToDraw(Mesh m)
        {
            meshesToDraw.Add(m);
        }

        public static void AddAnimatedObject(AnimatedObject animObj)
        {
            animatedObjects.Add(animObj);
        }

        public static void RemoveMeshToDraw(Mesh m)
        {
            meshesToDraw.Remove(m);
        }

        public static void RemoveAnimatedObject(AnimatedObject animObj)
        {
            animatedObjects.Remove(animObj);
        }
    }
}
