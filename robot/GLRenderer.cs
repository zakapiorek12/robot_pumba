using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System.Reflection;
using System.Drawing.Imaging;

namespace robot
{
    public class GLRenderer
    {
        private Matrix4 projectionMatrix;

        public enum MyShaderType
        {
            PHONG_LIGHT,
            PLATE
        }
        private Dictionary<MyShaderType, ShaderProgram> shaders = new Dictionary<MyShaderType, ShaderProgram>();

        private float ambientCoefficient = 1.0f;
        private Vector3 lightColor = new Vector3(0.9f, 0.8f, 0.8f);
        private Vector3 lightPosition = new Vector3(-1.0f, 1.0f, 1.0f);

        private static List<Mesh>[] meshesToDraw;
        private static List<AnimatedObject> animatedObjects = new List<AnimatedObject>();
        private bool previous;
        private Mesh rectangle;
        private Reflection reflection;

        private static readonly int maximumContourEdges = 10000;
        Edge[] contourEdges = new Edge[maximumContourEdges];
        Mesh[] shadowFaces = new Mesh[maximumContourEdges];

        MeshLoader meshLoader = new MeshLoader();
        private Robot robot;

        private Bitmap texture;
        private int textureID;

        public GLRenderer(int viewPortWidth, int viewportHeight)
        {
            meshesToDraw = new List<Mesh>[Enum.GetValues(typeof(MyShaderType)).Length];
            for (int i = 0; i < meshesToDraw.Length; i++)
                meshesToDraw[i] = new List<Mesh>();

            LoadShaders();
            LoadTexture();
            CreateProjectionMatrix(viewPortWidth, viewportHeight);
            CreateScene();

            GL.ClearColor(Color.Black);
        }

        private void LoadShaders()
        {
            ShaderProgram shaderProgram = new ShaderProgram("shaders/VS.vert",
                "shaders/PS.vert", true);
            shaders.Add(MyShaderType.PHONG_LIGHT, shaderProgram);

            shaderProgram = new ShaderProgram("shaders/VS_Plate.vert",
                "shaders/PS_Plate.vert", true);
            shaders.Add(MyShaderType.PLATE, shaderProgram);
        }

        public void CreateProjectionMatrix(int viewportWidth, int viewportHeight)
        {
            float far = (float)20f;
            float near = (float)0.1f;
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
            rectangle = meshLoader.GetRectangleMesh(1.5f, 1.0f, new Vector4(0.4f, 0.4f, 1.0f, 0.5f));
            rectangle.isPlate = 1;
            rectangle.ModelMatrix = Matrix4.CreateRotationY((float)(Math.PI / 2.0f)) *
                                     Matrix4.CreateRotationZ((float)(30.0f * Math.PI / 180.0f)) *
                                     Matrix4.CreateTranslation(-1.5f, 0.0f, 0.0f);
            rectangle.CalculateInverted();
            robot = new Robot(rectangle);

            reflection = new Reflection(robot, rectangle);
            AddAnimatedObject(reflection);

            AddMeshToDraw(rectangle, MyShaderType.PHONG_LIGHT);

            robot.AddOnScene(MyShaderType.PHONG_LIGHT);

            float floorYOffset = -1.0f;
            Mesh floor = meshLoader.GetRectangleMesh(6.0f, 6.0f, new Vector4(0.8f, 0.8f, 0.8f, 1.0f));
            floor.ModelMatrix = Matrix4.CreateRotationX((float)(-Math.PI / 2.0f)) * Matrix4.CreateTranslation(0, floorYOffset, 0);
            GLRenderer.AddMeshToDraw(floor, MyShaderType.PHONG_LIGHT);

            Mesh cylinder = meshLoader.GetCylinderMesh(0.5f, 2.5f, new Vector4(0, 0, 1, 1), 40);
            cylinder.ModelMatrix = Matrix4.CreateRotationX((float)(Math.PI / 2.0f)) *
                                   Matrix4.CreateTranslation(1.5f, 0.51f + floorYOffset, 0.0f);
            AddMeshToDraw(cylinder, MyShaderType.PHONG_LIGHT);
        }

        private void LoadTexture()
        {
            texture = new Bitmap(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "texture.jpg"));

            GL.GenTextures(1, out textureID);
            GL.BindTexture(TextureTarget.Texture2D, textureID);

            BitmapData data = texture.LockBits(new System.Drawing.Rectangle(0, 0, texture.Width, texture.Height),
                ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, data.Width, data.Height, 0,
                OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);

            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);

            texture.UnlockBits(data);
        }

        public void DoScene(float deltaTime, Camera camera)
        {
            foreach (var anim in animatedObjects)
                anim.DoAnimation(deltaTime);

            RenderWithPhongLightShader(camera);
            
            //tutaj dodac renderowanie przez nowe shadery
        }

        private void RenderWithPhongLightShader(Camera camera)
        {
            ShaderProgram activeShader = shaders[MyShaderType.PHONG_LIGHT];
            GL.UseProgram(activeShader.ProgramID);

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);
            GL.Enable(EnableCap.DepthTest);

            GL.Enable(EnableCap.Texture2D);
            GL.BindTexture(TextureTarget.Texture2D, textureID);

            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);

            BindCameraAndProjectionToShaders(camera, activeShader);
            BindLightDataToShaders(activeShader);
            
            Stencil(activeShader);
            RenderShadows(activeShader);
            //foreach (Mesh m in meshesToDraw[(int)MyShaderType.PHONG_LIGHT])
            //    DrawMesh(m, activeShader);

            GL.Flush();
        }

        private void DrawMesh(Mesh m, ShaderProgram shader)
        {
            m.BindVAO();
            BindMeshMaterialDataToShaders(m, shader);

            GL.UniformMatrix4(shader.GetUniform("object_matrix"), false, ref m.ModelMatrix);
            GL.DrawElements(PrimitiveType.Triangles, m.IndexBuffer.Length, DrawElementsType.UnsignedInt, 0);
        }

        private void RenderShadows(ShaderProgram shader)
        {
            int shadowFacesInd = 0;
            int contourEdgeInd = 0;
            foreach (Mesh m in robot.meshes)
            {
                Vector3 lightPos = (new Vector4(lightPosition, 1) * m.ModelMatrix.Inverted()).Xyz;
                for(int i = 0; i < m.Neighbourhood.Length; i++)
                {
                    Triangle firstTri = m.TrianglesList[m.Neighbourhood[i].firstTriangle];
                    Vector3 triangleCenter1 = (m.NormalizedVertexBuffer[firstTri.firstVertex].vertex +
                        m.NormalizedVertexBuffer[firstTri.secondVertex].vertex +
                        m.NormalizedVertexBuffer[firstTri.thirdVertex].vertex) / 3.0f;
                    triangleCenter1 = (new Vector4(triangleCenter1, 1)*m.ModelMatrix).Xyz;
                    Vector3 lightDir1 = triangleCenter1 - lightPos;
                    Vector3 triangleNormal1 = m.NormalizedVertexBuffer[firstTri.firstVertex].normal;
                    triangleNormal1 = (new Vector4(triangleNormal1, 0) *m.ModelMatrix).Xyz;

                    Triangle secTri = m.TrianglesList[m.Neighbourhood[i].secondTriangle];
                    Vector3 triangleCenter2 = (m.NormalizedVertexBuffer[secTri.firstVertex].vertex +
                        m.NormalizedVertexBuffer[secTri.secondVertex].vertex +
                        m.NormalizedVertexBuffer[secTri.thirdVertex].vertex) / 3.0f;
                    triangleCenter2 = (new Vector4(triangleCenter2, 1) * m.ModelMatrix).Xyz;
                    Vector3 lightDir2 = triangleCenter2 - lightPos;
                    Vector3 triangleNormal2 = m.NormalizedVertexBuffer[secTri.firstVertex].normal;
                    triangleNormal2 = (new Vector4(triangleNormal2, 0) * m.ModelMatrix).Xyz;

                    if (Vector3.Dot(lightDir1, triangleNormal1)*Vector3.Dot(lightDir2, triangleNormal2) >= 0.0f)
                    {
                        contourEdges[contourEdgeInd] = new Edge(m.VertexBuffer[m.Neighbourhood[i].firstVertex],
                            m.VertexBuffer[m.Neighbourhood[i].secondVertex]);
                        contourEdges[contourEdgeInd].first = (new Vector4(contourEdges[contourEdgeInd].first, 1) * m.ModelMatrix).Xyz;
                        contourEdges[contourEdgeInd].second = (new Vector4(contourEdges[contourEdgeInd].second, 1) * m.ModelMatrix).Xyz;
                        contourEdgeInd++;
                    }
                }
                
                float extrusion = 1000.0f;
                for(int i = 0; i < contourEdgeInd; i++)
                {
                    Edge e = contourEdges[i];
                    Vector3 vert1 = e.first;
                    Vector3 vert2 = e.second;
                    Vector3 vert3 = e.second + extrusion * (e.second - lightPos);
                    Vector3 vert4 = e.first + extrusion * (e.first - lightPos);
                    if (shadowFaces[shadowFacesInd] != null)
                    {
                        shadowFaces[shadowFacesInd].VertexBuffer[0] = shadowFaces[shadowFacesInd].NormalizedVertexBuffer[0].vertex = vert1;
                        shadowFaces[shadowFacesInd].VertexBuffer[1] = shadowFaces[shadowFacesInd].NormalizedVertexBuffer[1].vertex = vert2;
                        shadowFaces[shadowFacesInd].VertexBuffer[2] = shadowFaces[shadowFacesInd].NormalizedVertexBuffer[2].vertex = vert3;
                        shadowFaces[shadowFacesInd].VertexBuffer[3] = shadowFaces[shadowFacesInd].NormalizedVertexBuffer[3].vertex = vert4;
                        shadowFaces[shadowFacesInd].FillVbos();
                    }
                    else
                        shadowFaces[shadowFacesInd] = meshLoader.GetShadowQuadMesh(vert1, vert2, vert3, vert4);
                    shadowFacesInd++;
                }

                contourEdgeInd = 0;
            }
            //I sposob
            //GL.ColorMask(false, false, false, false);
            //foreach (Mesh m in meshesToDraw[(int) MyShaderType.PHONG_LIGHT])
            //    DrawMesh(m, shader);
            //GL.Enable(EnableCap.CullFace);
            //GL.Enable(EnableCap.StencilTest);
            //GL.DepthMask(false);
            //GL.StencilFunc(StencilFunction.Always, 0, ~0);
            //GL.CullFace(CullFaceMode.Back);
            //GL.StencilOp(StencilOp.Keep, StencilOp.Keep, StencilOp.Incr);
            //for (int i = 0; i < shadowFacesInd; i++)
            //    DrawMesh(shadowFaces[i], shader);
            //GL.CullFace(CullFaceMode.Front);
            //GL.StencilOp(StencilOp.Keep, StencilOp.Keep, StencilOp.Decr);
            //for (int i = 0; i < shadowFacesInd; i++)
            //    DrawMesh(shadowFaces[i], shader);

            //GL.DepthMask(true);
            //GL.ColorMask(true, true, true, true);
            //GL.CullFace(CullFaceMode.Back);
            //GL.DepthFunc(DepthFunction.Lequal);
            //GL.StencilOp(StencilOp.Keep, StencilOp.Keep, StencilOp.Keep);
            //GL.StencilFunc(StencilFunction.Greater, 0, ~0);
            //GL.Disable(EnableCap.Light0);
            //foreach (Mesh m in meshesToDraw[(int) MyShaderType.PHONG_LIGHT])
            //    DrawMesh(m, shader);
            //GL.StencilFunc(StencilFunction.Equal, 0, ~0);
            //GL.Enable(EnableCap.Light0);
            //foreach (Mesh m in meshesToDraw[(int) MyShaderType.PHONG_LIGHT])
            //    DrawMesh(m, shader);
            //GL.Disable(EnableCap.StencilTest);

            //GL.ColorMask(false, false, false, false);
            //GL.Disable(EnableCap.CullFace);
            //GL.Enable(EnableCap.StencilTest);
            //GL.DepthMask(false);
            //GL.StencilOpSeparate(StencilFace.Front, StencilOp.Keep, StencilOp.Keep, StencilOp.IncrWrap);
            //GL.StencilOpSeparate(StencilFace.Back, StencilOp.Keep, StencilOp.Keep, StencilOp.DecrWrap);
            //GL.StencilFuncSeparate(StencilFace.FrontAndBack, StencilFunction.Always, 0, ~0);
            //for (int i = 0; i < shadowFacesInd; i++)
            //    DrawMesh(shadowFaces[i], shader);

            //II sposob
            GL.DepthMask(true);
            GL.ColorMask(true, true, true, true);
            GL.StencilMask(~0);
            GL.Clear(ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);
            GL.Disable(EnableCap.StencilTest);
            
            GL.Uniform1(shader.GetUniform("drawUnlitScene"), 1);
            foreach (Mesh m in meshesToDraw[(int)MyShaderType.PHONG_LIGHT])
                DrawMesh(m, shader);
            
            GL.DepthMask(false);
            GL.ColorMask(false, false, false, false);
            GL.Enable(EnableCap.StencilTest);
            GL.StencilFunc(StencilFunction.Always, 1, ~0);
            GL.StencilOpSeparate(StencilFace.Front, StencilOp.Keep, StencilOp.Keep, StencilOp.IncrWrap);
            GL.StencilOpSeparate(StencilFace.Back, StencilOp.Keep, StencilOp.Keep, StencilOp.DecrWrap);
            for (int i = 0; i < shadowFacesInd; i++)
                DrawMesh(shadowFaces[i], shader);

            GL.DepthMask(true);
            GL.Clear(ClearBufferMask.DepthBufferBit);
            GL.StencilMask(0);
            GL.StencilFunc(StencilFunction.Equal, 0, ~0);
            GL.StencilOp(StencilOp.Keep, StencilOp.Keep, StencilOp.Keep);
            GL.ColorMask(true, true, true, true);
            GL.Uniform1(shader.GetUniform("drawUnlitScene"), 0);
            foreach (Mesh m in meshesToDraw[(int)MyShaderType.PHONG_LIGHT])
                DrawMesh(m, shader);
            

            //GL.ColorMask(true, true, true, true);
            //GL.Disable(EnableCap.StencilTest);
            //GL.Disable(EnableCap.CullFace);
            //GL.DepthMask(true);
        }

        private void Stencil(ShaderProgram shader)
        {
            GL.Clear(ClearBufferMask.DepthBufferBit | ClearBufferMask.ColorBufferBit);

            GL.DepthMask(false);
            GL.Enable(EnableCap.StencilTest);
            GL.StencilMask(~0);
            GL.StencilFuncSeparate(StencilFace.Back, StencilFunction.Never, 1, ~0);
            GL.StencilFuncSeparate(StencilFace.Front, StencilFunction.Always, 1, ~0);
            GL.StencilOp(StencilOp.Zero, StencilOp.Zero, StencilOp.Replace);
            GL.Clear(ClearBufferMask.StencilBufferBit);

            DrawMesh(rectangle, shader);

            GL.StencilMask(0);
            GL.DepthMask(true);
            GL.StencilFunc(StencilFunction.Equal, 1, ~0);
            GL.StencilOp(StencilOp.Zero, StencilOp.Zero, StencilOp.Replace);
            foreach (var mesh in reflection.meshes)
                DrawMesh(mesh, shader);

            GL.Disable(EnableCap.StencilTest);
        }

        private void BindCameraAndProjectionToShaders(Camera camera, ShaderProgram shader)
        {
            GL.UniformMatrix4(shader.GetUniform("cameraview_matrix"), false, ref camera.ResultMatrix);
            Matrix4 cameraModelMatrix = camera.ResultMatrix.Inverted();
            GL.UniformMatrix4(shader.GetUniform("cameraModel_matrix"), false, ref cameraModelMatrix);
            GL.UniformMatrix4(shader.GetUniform("projection_matrix"), false, ref projectionMatrix);
        }
        private void BindLightDataToShaders(ShaderProgram shader)
        {
            GL.Uniform3(shader.GetUniform("lightColor"), lightColor);
            GL.Uniform3(shader.GetUniform("lightPosition"), lightPosition);
            GL.Uniform1(shader.GetUniform("ambientCoefficient"), ambientCoefficient);
        }

        private void BindMeshMaterialDataToShaders(Mesh m, ShaderProgram shader)
        {
            GL.Uniform1(shader.GetUniform("materialSpecExponent"), m.materialSpecExponent);
            GL.Uniform3(shader.GetUniform("specularColor"), m.materialDiffuseSpecularColor);
            GL.Uniform4(shader.GetUniform("surfaceColor"), m.surfaceColor);
            GL.Uniform1(shader.GetUniform("isPlate"), m.isPlate);
        }

        public static void AddMeshToDraw(Mesh m, MyShaderType shaderType)
        {
            meshesToDraw[(int) shaderType].Add(m);
        }

        public static void AddAnimatedObject(AnimatedObject animObj)
        {
            animatedObjects.Add(animObj);
        }

        public static void RemoveMeshToDraw(Mesh m)
        {
            for(int i = 0; i < (int) Enum.GetValues(typeof(MyShaderType)).Length; i++)
                meshesToDraw[i].Remove(m);
        }

        public static void RemoveAnimatedObject(AnimatedObject animObj)
        {
            animatedObjects.Remove(animObj);
        }
    }
}
