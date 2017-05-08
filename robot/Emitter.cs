using OpenTK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace robot
{
    class Emitter
    {
        class Particle
        {
            public Mesh mesh;
            public int old;
            public Vector3 velocity;
        }

        static int MAX_OLD = 20;
        static int PARTICLE_PER_FRAME = 10;

        private Random rand = new Random();
        private GLRenderer.MyShaderType shaderType;
        private Robot robot;
        private List<Particle> particles = new List<Particle>();

        public Emitter(Robot robot, GLRenderer.MyShaderType shaderType)
        {
            this.robot = robot;
            this.shaderType = shaderType;
        }

        public void RefreshParticles()
        {
            for (int i = 0; i < particles.Count;)
            {
                Particle p = particles[i];
                p.old += 1;
                if (p.old >= MAX_OLD)
                {
                    GLRenderer.RemoveMeshToDraw(p.mesh);
                    particles.Remove(p);
                    continue;
                }
                i++;

                p.mesh.NormalizedVertexBuffer[0].vertex += p.velocity;//update position
                p.mesh.NormalizedVertexBuffer[0].normal.X = p.old / (float)MAX_OLD;//update transparency
                p.velocity -= new Vector3(0.0f, 0.0001f, 0.0f);//gravity

                p.mesh.FillVbos();
            }

            for (int i = 0; i < PARTICLE_PER_FRAME; i++)
                AddParticle();
        }

        private void AddParticle()
        {
            List<Vector3> vertices = new List<Vector3>();
            vertices.Add(robot.endPointPos);

            List<Normalized> normalized = new List<Normalized>();
            normalized.Add(new Normalized { vertex = vertices[0], normal = new Vector3(0.0f, 0.0f, 0.0f) });

            List<uint> indices = new List<uint> { 0 };

            Mesh m = new Mesh(vertices.ToArray(), normalized.ToArray(), indices.ToArray(), null);
            m.ModelMatrix = Matrix4.Identity;
            Vector3 r = new Vector3((float)rand.NextDouble() - 0.5f, (float)rand.NextDouble() - 0.5f, (float)rand.NextDouble() - 0.5f);
            r /= 8f;
            Vector3 particleVelocity = (r + robot.endPointDirection) * 0.08f;
            Particle p = new Particle
            {
                mesh = m,
                old = 0,
                velocity = particleVelocity
            };

            particles.Add(p);
            GLRenderer.AddMeshToDraw(m, shaderType);
        }
    }
}
