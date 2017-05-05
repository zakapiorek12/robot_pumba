using OpenTK;
using OpenTK.Graphics.OpenGL;
using System;
using System.Drawing;

namespace robot
{
    public class Camera
    {
        public Matrix4 Move, Rotation, ResultMatrix;
        float cameraDistMove = .05f;
        float cameraWheelMove = .5f;
        float cameraRotate = .005f;
        float latitude = 0, longitude = 0;

        public Camera() : base()
        {
            Move = Matrix4.Identity;
            Rotation = Matrix4.Identity;
            ResultMatrix = Matrix4.Identity;
            CalculateResultMartix();
        }

        private void CalculateResultMartix()
        {
            ResultMatrix = Move * Rotation;
        }

        public void MoveCamera(MoveEnum move)
        {
            Move *= Rotation;
            switch (move)
            {
                case MoveEnum.Left:
                    Move[3, 0] += cameraDistMove;
                    break;
                case MoveEnum.Right:
                    Move[3, 0] -= cameraDistMove;
                    break;
                case MoveEnum.Up:
                    Move[3, 1] -= cameraDistMove;
                    break;
                case MoveEnum.Down:
                    Move[3, 1] += cameraDistMove;
                    break;
                case MoveEnum.Backward:
                    Move[3, 2] -= cameraWheelMove;
                    break;
                case MoveEnum.Forward:
                    Move[3, 2] += cameraWheelMove;
                    break;
            }
            Move *= Rotation.Inverted();
            CalculateResultMartix();
        }

        public void Rotate(Point move)
        {
            float xAngle = move.X * cameraRotate;
            longitude += xAngle;
            Rotation *= GetYRotationMatrix(xAngle);

            float yAngle = move.Y * cameraRotate;
            float newLatitude = latitude + yAngle;
            if (newLatitude > -Math.PI / 2 &&
                newLatitude < Math.PI / 2)
            {
                latitude = newLatitude;
                Rotation *= GetXRotationMatrix(yAngle);
            }

            CalculateResultMartix();
        }


        private Matrix4 GetYRotationMatrix(float angle)
        {
            float sin = (float)Math.Sin(angle);
            float cos = (float)Math.Sqrt(1 - sin * sin);

            Matrix4 thisRotation = new OpenTK.Matrix4(
                cos, 0, sin, 0,
                0, 1, 0, 0,
                -sin, 0, cos, 0,
                0, 0, 0, 1);

            return thisRotation;
        }

        private Matrix4 GetXRotationMatrix(float angle)
        {
            float sin = (float)Math.Sin(angle);
            float cos = (float)Math.Sqrt(1 - sin * sin);

            Matrix4 thisRotation = new OpenTK.Matrix4(
                1, 0, 0, 0,
                0, cos, -sin, 0,
                0, sin, cos, 0,
                0, 0, 0, 1);

            return thisRotation;
        }
    }
}
