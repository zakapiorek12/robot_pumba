using OpenTK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace robot
{
    class Reflection : AnimatedObject
    {
        private Robot robot;
        private Mesh rectangle;
        private Matrix4 MirrorMatrix;

        public Reflection(Robot robot, Mesh rectangle)
        {
            this.robot = robot;
            this.rectangle = rectangle;
            MirrorMatrix = rectangle.InvertedModelMatrix *
                    Matrix4.CreateScale(new Vector3(1, 1, -1)) *
                    rectangle.ModelMatrix;
        }

        public override void DoAnimation(float deltaTime)
        {
            for (int i = 0; i < meshes.Count(); i++)
                meshes[i].ModelMatrix = robot.meshes[i].ModelMatrix * MirrorMatrix;
        }

        public override Mesh[] ProvideObjectMeshes()
        {
            MeshLoader ml = new MeshLoader();
            return ml.GetRobotMesh();
        }
    }
}
