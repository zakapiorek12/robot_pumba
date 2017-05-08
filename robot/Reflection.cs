using OpenTK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace robot
{
    class Reflection
    {
        public Mesh[] mirroredObject;
        public Mesh mirror;
        public Matrix4 MirrorMatrix;

        public Reflection(Mesh[] mirroredObject, Mesh mirror)
        {
            this.mirroredObject = mirroredObject;
            this.mirror = mirror;
            MirrorMatrix = mirror.InvertedModelMatrix *
                    Matrix4.CreateScale(new Vector3(1, 1, -1)) *
                    mirror.ModelMatrix;
        }
    }
}
