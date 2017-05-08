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
        private static readonly int MAX_PARTICLES = 400;
        class Particle
        {
            public float old;
            public Vector3 velocity;
        }

        static float MAX_OLD = 0.4f; //in seconds
        static int PARTICLE_PER_FRAME = 10;
        private int insertedParticlesInThisSec;

        private Random rand = new Random();
        private GLRenderer.MyShaderType shaderType;
        private Robot robot;
        private Particle[] particles = new Particle[MAX_PARTICLES];
        public Mesh[] particlesMeshes = new Mesh[MAX_PARTICLES];

        public Emitter(Robot robot, GLRenderer.MyShaderType shaderType)
        {
            this.robot = robot;
            this.shaderType = shaderType;
        }

        public void RefreshParticles(float deltaTime)
        {
            insertedParticlesInThisSec = 0;
            
            for (int i = 0; i < particles.Length; i++)
            {
                if (particles[i] == null)
                {
                    if (insertedParticlesInThisSec < PARTICLE_PER_FRAME)
                    {
                        AddParticle(i);
                        insertedParticlesInThisSec++;
                    }
                    continue;
                }
                Particle p = particles[i];
                p.old += deltaTime;
                if (p.old >= MAX_OLD)
                    p.old = MAX_OLD;

                if (p.old >= MAX_OLD)
                {
                    if (insertedParticlesInThisSec < PARTICLE_PER_FRAME)
                    {
                        AddParticle(i);
                        insertedParticlesInThisSec++;
                    }
                    continue;
                }

                particlesMeshes[i].NormalizedVertexBuffer[0].vertex += p.velocity;//update position
                particlesMeshes[i].ModelMatrix = Matrix4.CreateTranslation(particlesMeshes[i].NormalizedVertexBuffer[0].vertex);
                particlesMeshes[i].NormalizedVertexBuffer[0].normal.X = p.old / MAX_OLD;//update transparency
                p.velocity -= new Vector3(0.0f, 0.0001f, 0.0f);//gravity

                particlesMeshes[i].FillVbos();
            }
        }

        private void AddParticle(int currentParticleInd)
        {
            List<Vector3> vertices = new List<Vector3>();
            vertices.Add(robot.endPointPos);

            List<Normalized> normalized = new List<Normalized>();
            normalized.Add(new Normalized { vertex = vertices[0], normal = new Vector3(0.0f, 0.0f, 0.0f) });

            List<uint> indices = new List<uint> { 0 };

            Vector3 r = new Vector3((float)rand.NextDouble() - 0.5f, (float)rand.NextDouble() - 0.5f,
                    (float)rand.NextDouble() - 0.5f);
            r /= 8f;
            Vector3 particleVelocity = (r + robot.endPointDirection) * 0.08f;
            if (rand.NextDouble() < 0.5)
                particleVelocity = -particleVelocity;
            particleVelocity += robot.endPointNormal * 0.02f;

            Mesh m;
            if (particles[currentParticleInd] == null)
            {
                m = new Mesh(vertices.ToArray(), normalized.ToArray(), indices.ToArray(), null);
                m.ModelMatrix = Matrix4.CreateTranslation(vertices[0]);
                Particle p = new Particle
                {
                    old = 0,
                    velocity = particleVelocity
                };

                particlesMeshes[currentParticleInd] = m;
                particles[currentParticleInd] = p;
                GLRenderer.AddMeshToDraw(m, shaderType);
            }
            else
            {
                particles[currentParticleInd].old = 0;
                particles[currentParticleInd].velocity = particleVelocity;
                particlesMeshes[currentParticleInd].NormalizedVertexBuffer = normalized.ToArray();
                particlesMeshes[currentParticleInd].VertexBuffer = vertices.ToArray();
                particlesMeshes[currentParticleInd].FillVbos();
            }
        }
    }
}
