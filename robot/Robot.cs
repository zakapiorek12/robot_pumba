using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace robot
{
    class Robot : AnimatedObject
    {
        public Robot()
        {
            MeshLoader ml = new MeshLoader();
        }

        public override Mesh[] ProvideObjectMeshes()
        {
            MeshLoader ml = new MeshLoader();
            return ml.GetMesh();
        }

        public override void DoAnimation(float deltaTime)
        {
            
        }
    }
}
