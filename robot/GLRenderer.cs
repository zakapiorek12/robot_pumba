﻿using System;
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
            PLATE,
            PARTICLES
        }
        private Dictionary<MyShaderType, ShaderProgram> shaders = new Dictionary<MyShaderType, ShaderProgram>();

        private float ambientCoefficient = 1.0f;
        private Vector3 lightColor = new Vector3(0.9f, 0.8f, 0.8f);
        private Vector3 lightPosition = new Vector3(0.0f, 4.0f, 2.0f);

        private static List<Mesh>[] meshesToDraw;
        private static List<AnimatedObject> animatedObjects = new List<AnimatedObject>();
        private Mesh rectangle;
        private Reflection robotReflection, particlesReflection;

        private static readonly int maximumContourEdges = 10000;
        Edge[] contourEdges = new Edge[maximumContourEdges];
        Mesh[] shadowFaces = new Mesh[maximumContourEdges];

        MeshLoader meshLoader = new MeshLoader();
        private Robot robot;
        private Emitter emitter;

        private int plateTextureID, particleTextureID;

        private Mesh cube;

        public GLRenderer(int viewPortWidth, int viewportHeight)
        {
            meshesToDraw = new List<Mesh>[Enum.GetValues(typeof(MyShaderType)).Length];
            for (int i = 0; i < meshesToDraw.Length; i++)
                meshesToDraw[i] = new List<Mesh>();

            LoadShaders();
            LoadTexture("plate.jpg", ref plateTextureID);
            LoadTexture("iskra.jpg", ref particleTextureID);
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

            shaderProgram = new ShaderProgram("shaders/VS_Particles.vert", "shaders/PS_Particles.vert", "shaders/GS_Particles.vert", true);
            shaders.Add(MyShaderType.PARTICLES, shaderProgram);
        }

        public void CreateProjectionMatrix(int viewportWidth, int viewportHeight)
        {
            float far = (float)100f;
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
            rectangle = meshLoader.GetRectangleMesh(1.5f, 1.0f, new Vector4(0.4f, 0.4f, 0.4f, 0.6f));
            rectangle.isPlate = 1;
            rectangle.ModelMatrix = Matrix4.CreateRotationY((float)(Math.PI / 2.0f)) *
                                     Matrix4.CreateRotationZ((float)(30.0f * Math.PI / 180.0f)) *
                                     Matrix4.CreateTranslation(-1.5f, 0.0f, 0.0f);
            rectangle.CalculateInverted();
            robot = new Robot(rectangle);

            emitter = new Emitter(robot, MyShaderType.PARTICLES);

            robotReflection = new Reflection(robot.meshes, rectangle);
            particlesReflection = new Reflection(emitter.particlesMeshes, rectangle);

            AddMeshToDraw(rectangle, MyShaderType.PHONG_LIGHT);

            robot.AddOnScene(MyShaderType.PHONG_LIGHT);

            float floorYOffset = -1.0f;
            float roomSize = 10.0f;
            Vector4 roomColor = new Vector4(0.2f, 0.8f, 0.2f, 1.0f);
            Mesh floor = meshLoader.GetRectangleMesh(roomSize, roomSize, roomColor);
            floor.ModelMatrix = Matrix4.CreateRotationX((float)(-Math.PI / 2.0f)) * Matrix4.CreateTranslation(0, floorYOffset, 0);
            GLRenderer.AddMeshToDraw(floor, MyShaderType.PHONG_LIGHT);

            Mesh ceiling = meshLoader.GetRectangleMesh(roomSize, roomSize, roomColor);
            ceiling.ModelMatrix = Matrix4.CreateRotationX((float)(Math.PI / 2.0f)) * Matrix4.CreateTranslation(0, roomSize + floorYOffset, 0);
            GLRenderer.AddMeshToDraw(ceiling, MyShaderType.PHONG_LIGHT);

            Mesh left = meshLoader.GetRectangleMesh(roomSize, roomSize, roomColor);
            left.ModelMatrix = Matrix4.CreateRotationY((float)(Math.PI / 2.0f)) * Matrix4.CreateTranslation(-roomSize / 2.0f, roomSize / 2.0f + floorYOffset, 0);
            GLRenderer.AddMeshToDraw(left, MyShaderType.PHONG_LIGHT);

            Mesh right = meshLoader.GetRectangleMesh(roomSize, roomSize, roomColor);
            right.ModelMatrix = Matrix4.CreateRotationY((float)(-Math.PI / 2.0f)) * Matrix4.CreateTranslation(roomSize / 2.0f, roomSize / 2.0f + floorYOffset, 0);
            GLRenderer.AddMeshToDraw(right, MyShaderType.PHONG_LIGHT);

            Mesh front = meshLoader.GetRectangleMesh(roomSize, roomSize, roomColor);
            front.ModelMatrix = Matrix4.CreateTranslation(0, roomSize / 2.0f + floorYOffset, -roomSize / 2.0f);
            GLRenderer.AddMeshToDraw(front, MyShaderType.PHONG_LIGHT);

            Mesh back = meshLoader.GetRectangleMesh(roomSize, roomSize, roomColor);
            back.ModelMatrix = Matrix4.CreateRotationY((float)(Math.PI)) * Matrix4.CreateTranslation(0, roomSize / 2.0f + floorYOffset, roomSize / 2.0f);
            GLRenderer.AddMeshToDraw(back, MyShaderType.PHONG_LIGHT);

            Mesh cylinder = meshLoader.GetCylinderMesh(0.5f, 2.5f, new Vector4(0, 0, 1, 1), 40);
            cylinder.ModelMatrix = Matrix4.CreateRotationX((float)(Math.PI / 2.0f)) *
                                   Matrix4.CreateTranslation(1.5f, 0.51f + floorYOffset, 0.0f) *
                                   Matrix4.CreateRotationY((float)(Math.PI / 2.0f));
            AddMeshToDraw(cylinder, MyShaderType.PHONG_LIGHT);

            cube = meshLoader.GetCubeMesh(1.0f, new Vector4(1, 0, 0, 1));
            cube.ModelMatrix = Matrix4.CreateTranslation(2.0f, 1.0f, 0);
            GLRenderer.AddMeshToDraw(cube, MyShaderType.PHONG_LIGHT);
        }

        private void LoadTexture(string name, ref int id)
        {
            Bitmap texture = new Bitmap(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "textures",  name));

            GL.GenTextures(1, out id);
            GL.BindTexture(TextureTarget.Texture2D, id);

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

            GL.DepthMask(true);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);

            RenderParticles(deltaTime, camera, true);
            RenderWithPhongLightShader(camera);
            RenderParticles(deltaTime, camera, false);

            //tutaj dodac renderowanie przez nowe shadery
        }

        private void RenderParticles(float deltaTime, Camera camera, bool onlyStencil)
        {
            ShaderProgram activeShader = shaders[MyShaderType.PARTICLES];
            GL.UseProgram(activeShader.ProgramID);
            
            GL.BindTexture(TextureTarget.Texture2D, particleTextureID);
            
            GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.One);

            BindCameraAndProjectionToShaders(camera, activeShader);
            BindLightDataToShaders(activeShader);

            emitter.RefreshParticles(deltaTime);

            if (onlyStencil)
                Stencil(activeShader, particlesReflection, PrimitiveType.Points, false, false);
            else
            {
                GL.DepthMask(false);
                foreach (Mesh m in meshesToDraw[(int)MyShaderType.PARTICLES])
                     DrawMesh(m, activeShader, PrimitiveType.Points);
            }

            GL.Flush();
        }

        private void RenderWithPhongLightShader(Camera camera)
        {
            ShaderProgram activeShader = shaders[MyShaderType.PHONG_LIGHT];
            GL.UseProgram(activeShader.ProgramID);
            
            GL.Enable(EnableCap.DepthTest);

            GL.Enable(EnableCap.Texture2D);
            GL.BindTexture(TextureTarget.Texture2D, plateTextureID);

            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);

            BindCameraAndProjectionToShaders(camera, activeShader);
            BindLightDataToShaders(activeShader);
            
            Stencil(activeShader, robotReflection, PrimitiveType.Triangles);
            //RenderWithShadows(robot.meshes, activeShader, false);
            RenderWithShadows(new Mesh[] {cube}, activeShader, true); //nie moga byc 2 na raz bo tak gowniano zrobione ze renderowana jest od nowa scena w kazdym wywolaniu RenderSahdows

            GL.Flush();
        }

        private void DrawMesh(Mesh m, ShaderProgram shader, PrimitiveType mode = PrimitiveType.Triangles)
        {
            m.BindVAO();
            BindMeshMaterialDataToShaders(m, shader);

            GL.UniformMatrix4(shader.GetUniform("object_matrix"), false, ref m.ModelMatrix);
            GL.DrawElements(mode, m.IndexBuffer.Length, DrawElementsType.UnsignedInt, 0);
        }

        private void RenderWithShadows(Mesh[] meshesToCreateShadowFrom, ShaderProgram shader, bool detectContourEdgesBruteForce)
        {
            float extrusion = 50f;
            int shadowFacesInd = 0;
            int contourEdgeInd = 0;
            HashSet<Edge> hashsetEdges = new HashSet<Edge>();
            foreach (Mesh m in meshesToCreateShadowFrom)
            {
                //lightPosition juz mamy we wspolrzednych swiata defaultowo
                if (!detectContourEdgesBruteForce)
                {
                    for (int i = 0; i < m.Neighbourhood.Length; i++)
                    {
                        Triangle firstTri = m.TrianglesList[m.Neighbourhood[i].firstTriangle];
                        Vector3 triangleCenter1 = (m.NormalizedVertexBuffer[firstTri.firstVertex].vertex +
                                                   m.NormalizedVertexBuffer[firstTri.secondVertex].vertex +
                                                   m.NormalizedVertexBuffer[firstTri.thirdVertex].vertex)/3.0f;
                        triangleCenter1 = (new Vector4(triangleCenter1, 1)*m.ModelMatrix).Xyz;
                        Vector3 lightDir1 = (triangleCenter1 - lightPosition).Normalized();
                        Vector3 triangleNormal1 = m.NormalizedVertexBuffer[firstTri.firstVertex].normal;
                        triangleNormal1 = (new Vector4(triangleNormal1, 0)*m.ModelMatrix).Xyz;

                        Triangle secTri = m.TrianglesList[m.Neighbourhood[i].secondTriangle];
                        Vector3 triangleCenter2 = (m.NormalizedVertexBuffer[secTri.firstVertex].vertex +
                                                   m.NormalizedVertexBuffer[secTri.secondVertex].vertex +
                                                   m.NormalizedVertexBuffer[secTri.thirdVertex].vertex)/3.0f;
                        triangleCenter2 = (new Vector4(triangleCenter2, 1)*m.ModelMatrix).Xyz;
                        Vector3 lightDir2 = (triangleCenter2 - lightPosition).Normalized();
                        Vector3 triangleNormal2 = m.NormalizedVertexBuffer[secTri.firstVertex].normal;
                        triangleNormal2 = (new Vector4(triangleNormal2, 0)*m.ModelMatrix).Xyz;

                        float dot1 = Vector3.Dot(lightDir1, triangleNormal1);
                        float dot2 = Vector3.Dot(lightDir2, triangleNormal2);
                        if (dot1*dot2 <= 0.0f)
                        {
                            //WAZNE!!! W modelu pumba mamy podanych sasiadow (2 trojkaty i krawedz je laczaca ale zeby odroznic sciany bryly cienia tylne od przednich - to tutaj wprowadzam rozna kolejnosc wierzcholkow krawedzi w zaleznosci od iloczynu iloczynow skalarnych
                            if (dot1 <= 0.0f)
                                contourEdges[contourEdgeInd] = new Edge(m.VertexBuffer[m.Neighbourhood[i].firstVertex],
                                    m.VertexBuffer[m.Neighbourhood[i].secondVertex]);
                            else
                                contourEdges[contourEdgeInd] = new Edge(m.VertexBuffer[m.Neighbourhood[i].secondVertex],
                                    m.VertexBuffer[m.Neighbourhood[i].firstVertex]);
                            contourEdges[contourEdgeInd].first =
                                (new Vector4(contourEdges[contourEdgeInd].first, 1)*m.ModelMatrix).Xyz;
                            contourEdges[contourEdgeInd].second =
                                (new Vector4(contourEdges[contourEdgeInd].second, 1)*m.ModelMatrix).Xyz;
                            contourEdgeInd++;
                        }
                    }
                }
                else
                {
                    for (int i = 2; i < m.NormalizedVertexBuffer.Length; i += 3)
                    {
                        Vector3 triangleCenter1 = (m.NormalizedVertexBuffer[i - 2].vertex +
                                                   m.NormalizedVertexBuffer[i - 1].vertex +
                                                   m.NormalizedVertexBuffer[i].vertex)/3.0f;
                        triangleCenter1 = (new Vector4(triangleCenter1, 1)*m.ModelMatrix).Xyz;
                        Vector3 lightDir1 = (triangleCenter1 - lightPosition).Normalized();
                        Vector3 triangleNormal1 = m.NormalizedVertexBuffer[i].normal;
                        triangleNormal1 = (new Vector4(triangleNormal1, 0)*m.ModelMatrix).Xyz;

                        float dot1 = Vector3.Dot(lightDir1, triangleNormal1);
                        if (dot1 >= 0.0f)
                        {
                            for (int k = i - 2, n = i; k < i - 2 + 3; n = k, k++)
                            {
                                Edge e1 = new Edge(m.NormalizedVertexBuffer[k].vertex,
                                    m.NormalizedVertexBuffer[n].vertex);
                                e1.first =
                                    (new Vector4(e1.first, 1)*m.ModelMatrix).Xyz;
                                e1.second =
                                    (new Vector4(e1.second, 1)*m.ModelMatrix).Xyz;

                                Edge e2 = new Edge(e1.second, e1.first);

                                if (hashsetEdges.Contains(e1))
                                {
                                    hashsetEdges.Remove(e1);
                                }
                                else if (hashsetEdges.Contains(e2))
                                    hashsetEdges.Remove(e2);
                                else
                                    hashsetEdges.Add(e1);
                            }
                        }
                    }
                    contourEdgeInd = 0;
                    foreach (Edge e in hashsetEdges)
                    {
                        contourEdges[contourEdgeInd] = e;
                        contourEdgeInd++;
                    }
                }
                
                for(int i = 0; i < contourEdgeInd; i++)
                {
                    Edge e = contourEdges[i];
                    Vector3 vert1 = e.first;
                    Vector3 vert2 = e.second;
                    Vector3 vert3 = e.first + extrusion * (e.first - lightPosition).Normalized();
                    Vector3 vert4 = e.second + extrusion * (e.second - lightPosition).Normalized();

                    AddNewShadowFace(ref shadowFacesInd, vert1, vert2, vert3, vert4);
                }

                contourEdgeInd = 0;
            }
            //shadowFacesInd = 0;
            //Vector4 v1 = new Vector4(-1, -1, 0, 1);
            //Vector4 v2 = new Vector4(1, -1, 0, 1);
            //Vector4 v3 = new Vector4(-1, 1, 0, 1);
            //Vector4 v4 = new Vector4(1, 1, 0, 1);
            //Matrix4 mat = Matrix4.CreateRotationX((float)(-Math.PI / 4.0f));
            //AddNewShadowFace(ref shadowFacesInd, (v1 * mat).Xyz, (v2 * mat).Xyz, (v3 * mat).Xyz, (v4 * mat).Xyz);

            //mat = Matrix4.CreateRotationY((float) Math.PI) * Matrix4.CreateTranslation(0, 0, -0.8f) * Matrix4.CreateRotationX((float)(-Math.PI / 4.0f));
            //AddNewShadowFace(ref shadowFacesInd, (v1 * mat).Xyz, (v2 * mat).Xyz, (v3 * mat).Xyz, (v4 * mat).Xyz);

            //mat = Matrix4.CreateRotationY((float)(-Math.PI / 2.0f)) * Matrix4.CreateTranslation(-0.5f, 0, -0.5f);
            //AddNewShadowFace(ref shadowFacesInd, (v1 * mat).Xyz, (v2 * mat).Xyz, (v3 * mat).Xyz, (v4 * mat).Xyz);

            //renderowanie cieni
            GL.DepthMask(true);
            GL.ColorMask(true, true, true, true);
            GL.StencilMask(~0);
            GL.Clear(ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);
            GL.Disable(EnableCap.StencilTest);

            GL.Uniform1(shader.GetUniform("drawUnlitScene"), 1);
            foreach (Mesh m in meshesToDraw[(int)MyShaderType.PHONG_LIGHT])
                DrawMesh(m, shader);

            GL.Enable(EnableCap.CullFace);
            GL.Enable(EnableCap.StencilTest);
            GL.DepthMask(false);
            GL.ColorMask(false, false, false, false);
            GL.StencilFunc(StencilFunction.Always, 0, ~0);
            
            GL.CullFace(CullFaceMode.Back);
            GL.StencilOp(StencilOp.Keep, StencilOp.IncrWrap, StencilOp.Keep);
            for (int i = 0; i < shadowFacesInd; i++)
                DrawMesh(shadowFaces[i], shader);
            //przednie denko
            foreach (Mesh m in meshesToCreateShadowFrom)
            {
                DrawMesh(m, shader);
            }
            GL.CullFace(CullFaceMode.Front);
            GL.StencilOp(StencilOp.Keep, StencilOp.DecrWrap, StencilOp.Keep);
            for (int i = 0; i < shadowFacesInd; i++)
                DrawMesh(shadowFaces[i], shader);
            //tylne denko
            foreach (Mesh m in meshesToCreateShadowFrom)
            {
                Matrix4 prevMat = m.ModelMatrix;
                for (int i = 0; i < m.NormalizedVertexBuffer.Length; i++)
                {
                    Vector3 vert = (new Vector4(m.NormalizedVertexBuffer[i].vertex, 1) * m.ModelMatrix).Xyz;
                    m.NormalizedVertexBuffer[i].vertex = vert + extrusion * (vert - lightPosition).Normalized();
                }
                m.ModelMatrix = Matrix4.Identity;
                m.FillVbos();
                DrawMesh(m, shader);
                m.ModelMatrix = prevMat;
                for (int i = 0; i < m.NormalizedVertexBuffer.Length; i++)
                {
                    Vector4 vert = new Vector4(m.NormalizedVertexBuffer[i].vertex - extrusion * (m.NormalizedVertexBuffer[i].vertex - lightPosition).Normalized(), 1);
                    m.NormalizedVertexBuffer[i].vertex = (vert * m.ModelMatrix.Inverted()).Xyz;
                }
                m.FillVbos();
            }
            GL.Disable(EnableCap.CullFace);

            GL.DepthMask(true);
            GL.Clear(ClearBufferMask.DepthBufferBit);
            GL.StencilMask(0);
            GL.StencilFunc(StencilFunction.Equal, 0, ~0);
            GL.StencilOp(StencilOp.Keep, StencilOp.Keep, StencilOp.Keep);
            GL.ColorMask(true, true, true, true);
            GL.Uniform1(shader.GetUniform("drawUnlitScene"), 0);
            foreach (Mesh m in meshesToDraw[(int)MyShaderType.PHONG_LIGHT])
                DrawMesh(m, shader);

            //for (int i = 0; i < shadowFacesInd; i++)
            //    DrawMesh(shadowFaces[i], shader);
        }

        private void AddNewShadowFace(ref int shadowFacesInd, Vector3 vert1, Vector3 vert2, Vector3 vert3, Vector3 vert4)
        {
            if (shadowFaces[shadowFacesInd] != null)
            {
                    shadowFaces[shadowFacesInd].VertexBuffer[0] =
                        shadowFaces[shadowFacesInd].NormalizedVertexBuffer[0].vertex = vert1;
                    shadowFaces[shadowFacesInd].VertexBuffer[1] =
                        shadowFaces[shadowFacesInd].NormalizedVertexBuffer[1].vertex = vert2;
                    shadowFaces[shadowFacesInd].VertexBuffer[2] =
                        shadowFaces[shadowFacesInd].NormalizedVertexBuffer[2].vertex = vert3;
                    shadowFaces[shadowFacesInd].VertexBuffer[3] =
                        shadowFaces[shadowFacesInd].NormalizedVertexBuffer[3].vertex = vert4;
                shadowFaces[shadowFacesInd].FillVbos();
            }
            else
                shadowFaces[shadowFacesInd] = meshLoader.GetShadowQuadMesh(vert1, vert2, vert3, vert4);

            shadowFacesInd++;
        }

        private void Stencil(ShaderProgram shader, Reflection reflection, PrimitiveType reflectionPrimitiveType, bool reflectionDepthMaskWrite = true, bool fillStencil = true)
        {
            //GL.Clear(ClearBufferMask.DepthBufferBit | ClearBufferMask.ColorBufferBit);

            GL.Enable(EnableCap.StencilTest);

            if (fillStencil)
            {
                GL.DepthMask(false);
                GL.ColorMask(false, false, false, false);
                GL.StencilMask(~0);
                GL.StencilFuncSeparate(StencilFace.Back, StencilFunction.Never, 1, ~0);
                GL.StencilFuncSeparate(StencilFace.Front, StencilFunction.Always, 1, ~0);
                GL.StencilOp(StencilOp.Zero, StencilOp.Zero, StencilOp.Replace);
                GL.Clear(ClearBufferMask.StencilBufferBit);

                DrawMesh(reflection.mirror, shader);
            }

            GL.ColorMask(true, true, true, true);
            GL.StencilMask(0);
            GL.DepthMask(reflectionDepthMaskWrite);
            GL.StencilFunc(StencilFunction.Equal, 1, ~0);
            GL.StencilOp(StencilOp.Zero, StencilOp.Zero, StencilOp.Replace);
            foreach (var mesh in reflection.mirroredObject)
                if (mesh != null)
                {
                    Matrix4 prevMat = mesh.ModelMatrix;
                    mesh.ModelMatrix = mesh.ModelMatrix*reflection.MirrorMatrix;
                    DrawMesh(mesh, shader, reflectionPrimitiveType);
                    mesh.ModelMatrix = prevMat;
                }

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
