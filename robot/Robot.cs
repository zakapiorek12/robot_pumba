using System;
using OpenTK;

namespace robot
{
    class Robot : AnimatedObject
    {
        private float circleRadius = 0.4f;
        private float angularSpeed = (float) (Math.PI); //rotations per sec (in radians)
        private float currentAngle;

        public Vector3 endPointPos, endPointNormal, endPointDirection;

        private Vector3[] rotationPivotPoints = new Vector3[]
        {
            new Vector3(0, 0, 0), //not used - inserted for convenience when indexing
            new Vector3(0, 0.27f, 0),
            new Vector3(0, 0.27f, 0.26f),
            new Vector3(-0.91f, 0.27f, -0.26f),
            new Vector3(-2.05f, 0.27f, -0.26f),
            new Vector3(-1.72f, 0.27f, -0.26f)
        };
        private Mesh rectangle;

        public Robot(Mesh rectangle)
        {
            this.rectangle = rectangle;
            foreach (Mesh m in meshes)
                m.MainObject = true;
        }

        public override Mesh[] ProvideObjectMeshes()
        {
            MeshLoader ml = new MeshLoader();
            return ml.GetRobotMesh();
        }

        public override void DoAnimation(float deltaTime)
        {
            currentAngle += angularSpeed * deltaTime;
            currentAngle %= (float)(2 * Math.PI);

            float endPointX = (float)(circleRadius * Math.Cos(currentAngle));
            float endPointY = (float)(circleRadius * Math.Sin(currentAngle));
            endPointPos = (new Vector4(endPointX, endPointY, 0.0f, 1.0f) * rectangle.ModelMatrix).Xyz;
            //MeshLoader ml = new MeshLoader();
            //Mesh m = ml.GetDoubleSidedRectangleMesh(0.01f, 0.01f, new Vector3(1.0f, 0.0f, 0.0f));
            //m.ModelMatrix = Matrix4.CreateTranslation(endPointPos.Xyz);
            //GLRenderer.AddMeshToDraw(m);
            
            endPointNormal = (new Vector4(0.0f, 0.0f, 1.0f, 0.0f) * rectangle.ModelMatrix).Xyz.Normalized();
            endPointDirection = Vector3.Cross((new Vector4(endPointX, endPointY, 0.0f, 0.0f) * rectangle.ModelMatrix).Xyz, endPointNormal);

            float a1, a2, a3, a4, a5;

            InverseKinematics(endPointPos, endPointNormal, out a1, out a2, out a3, out a4, out a5);

            meshes[1].ModelMatrix = Matrix4.CreateTranslation(-rotationPivotPoints[1]) *
                Matrix4.CreateRotationY(a1) *
                Matrix4.CreateTranslation(rotationPivotPoints[1]);

            meshes[2].ModelMatrix = Matrix4.CreateTranslation(-rotationPivotPoints[2]) *
                                     Matrix4.CreateRotationZ(a2) *
                                     Matrix4.CreateTranslation(rotationPivotPoints[2]) *
                                     meshes[1].ModelMatrix;

            meshes[3].ModelMatrix = Matrix4.CreateTranslation(-rotationPivotPoints[3]) *
                                     Matrix4.CreateRotationZ(a3) *
                                     Matrix4.CreateTranslation(rotationPivotPoints[3]) *
                                     meshes[2].ModelMatrix;

            meshes[4].ModelMatrix = Matrix4.CreateTranslation(-rotationPivotPoints[4]) *
                                     Matrix4.CreateRotationX(a4) *
                                     Matrix4.CreateTranslation(rotationPivotPoints[4]) *
                                     meshes[3].ModelMatrix;

            meshes[5].ModelMatrix = Matrix4.CreateTranslation(-rotationPivotPoints[5])*
                                    Matrix4.CreateRotationZ(a5)*
                                    Matrix4.CreateTranslation(rotationPivotPoints[5]) *
                                    meshes[4].ModelMatrix;
        }
        private void InverseKinematics(Vector3 pos, Vector3 normal, out float a1, out float a2, out float a3, out float a4, out float a5)
        {
            float l1 = .91f, l2 = .81f, l3 = .33f, dy = .27f, dz = .26f;
            normal.Normalize();
            Vector3 pos1 = pos + normal * l3;

            float e = (float)Math.Sqrt(pos1.Z * pos1.Z + pos1.X * pos1.X - dz * dz);
            a1 = (float)(Math.Atan2(pos1.Z, -pos1.X) + Math.Atan2(dz, e));

            Vector3 pos2 = new Vector3(e, pos1.Y - dy, 0.0f);
            a3 = (float)-Math.Acos(Math.Min(1.0f, (pos2.X * pos2.X + pos2.Y * pos2.Y - l1 * l1 - l2 * l2) / (2.0f * l1 * l2)));

            float k = (float)(l1 + l2 * Math.Cos(a3));
            float l = (float)(l2 * Math.Sin(a3));
            a2 = (float)(-Math.Atan2(pos2.Y, Math.Sqrt(pos2.X * pos2.X + pos2.Z * pos2.Z)) - Math.Atan2(l, k));

            Vector3 normal1;
            normal1 = new Vector3(Matrix4.CreateRotationY(a1) * new Vector4(normal, 0.0f));
            normal1 = new Vector3(Matrix4.CreateRotationZ((a2 + a3)) * new Vector4(normal1, 0.0f));
            a5 = (float)Math.Acos(normal1.X);
            a4 = (float)Math.Atan2(normal1.Z, normal1.Y);
        }

    }
}
