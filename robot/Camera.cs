using OpenTK;
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
            ResultMatrix = ResultMatrix * Move * Rotation;
        }

        public void MoveCamera(MoveEnum move)
        {
            Move = Matrix4.Identity;
            Rotation = Matrix4.Identity;
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
            CalculateResultMartix();
        }

        public void Rotate(Point move)
        {
            Move = Matrix4.CreateTranslation(new Vector3((new Vector4(0, 0, 0, 1) * ResultMatrix)));
            ResultMatrix = Matrix4.Identity;
            latitude = move.Y * cameraRotate;
            longitude = move.X * cameraRotate;
            latitude = Math.Max(Math.Min(latitude, 90), -90);
            Rotation = GetYRotationMatrix(longitude);
            Rotation *= GetXRotationMatrix(latitude);
            CalculateResultMartix();
        }


        private Matrix4 GetYRotationMatrix(double angle)
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

        private Matrix4 GetXRotationMatrix(double angle)
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
