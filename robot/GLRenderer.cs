﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        private float ambientCoefficient = 1.0f;
        private Vector3 lightColor = new Vector3(0.9f, 0.8f, 0.8f);
        private Vector3 lightPosition = new Vector3(-1.0f, 1.0f, 0.0f);

        private static List<Mesh> meshesToDraw = new List<Mesh>();
        private static List<AnimatedObject> animatedObjects = new List<AnimatedObject>();  

        public GLRenderer()
        {
            LoadShaders();
            CreateProjectionMatrix();
            CreateScene();
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

        private void CreateProjectionMatrix()
        {
            projectionMatrix = Matrix4.Perspective(1f, 1f, .1f, 5f);
        }

        private void CreateScene()
        {
            MeshLoader ml = new MeshLoader();
            Mesh rectangle = ml.GetDoubleSidedRectangleMesh(1.5f, 1.0f, new Vector3(0.8f, 1.0f, 1.0f));
            AddMeshToDraw(rectangle);
            rectangle.ModelMatrix = Matrix4.CreateRotationY((float) (Math.PI/2.0f))*
                                     Matrix4.CreateRotationZ((float) (30.0f* Math.PI / 180.0f)) *
                                     Matrix4.CreateTranslation(-1.0f, 0.0f, 0.0f);

            Robot robot = new Robot(rectangle);
            robot.AddOnScene();
        }

        public void DoScene(float deltaTime, Camera camera)
        {
            foreach(var anim in animatedObjects)
                anim.DoAnimation(deltaTime);

            Render(camera);
        }

        private void Render(Camera camera)
        {
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            GL.ClearColor(Color.Black);
            GL.Enable(EnableCap.DepthTest);

            BindCameraAndProjectionToShaders(camera);
            BindLightDataToShaders();

            foreach (Mesh m in meshesToDraw)
            {
                m.BindVAO();
                BindMeshMaterialDataToShaders(m);

                GL.UniformMatrix4(objectMatrixLocation, false, ref m.ModelMatrix);
                GL.DrawElements(PrimitiveType.Triangles, m.IndexBuffer.Length, DrawElementsType.UnsignedInt, 0);
                GL.Flush();
            }
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
            GL.Uniform3(specularColorLocation, m.materialSpecularColor);
            GL.Uniform3(surfaceColorLocation, m.surfaceColor);
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