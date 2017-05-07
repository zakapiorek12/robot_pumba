using OpenTK;
using OpenTK.Graphics.OpenGL;
using System;
using System.Drawing;

namespace robot
{
    public class Camera
    {
        public Matrix4 ResultMatrix = Matrix4.Identity;
        Vector3 cameraPosition = new Vector3();
        float cameraDistMove = .05f;
        float cameraWheelMove = .5f;
        float cameraRotate = .005f;
        float latitude = 0, longitude = 0;

        private Vector3 forwardDir, upDir, rightDir;

        public Camera() : base()
        {
            cameraPosition = new Vector3(0, 0, 3);
            Rotate(new Point(0, 0));
            CalculateResultMartix();
        }

        private void CalculateResultMartix()
        {
            ResultMatrix = Matrix4.CreateTranslation(-cameraPosition) *
                Matrix4.CreateRotationY(longitude) *
                Matrix4.CreateRotationX(latitude);
        }

        public void MoveCamera(MoveEnum move)
        {
            float x = 0, y = 0, z = 0;
            switch (move)
            {
                case MoveEnum.Left:
                    x -= cameraDistMove;
                    break;
                case MoveEnum.Right:
                    x += cameraDistMove;
                    break;
                case MoveEnum.Up:
                    y += cameraDistMove;
                    break;
                case MoveEnum.Down:
                    y -= cameraDistMove;
                    break;
                case MoveEnum.Backward:
                    z += cameraWheelMove;
                    break;
                case MoveEnum.Forward:
                    z -= cameraWheelMove;
                    break;
            }
            Vector3 movement = x*rightDir + y*upDir + z*forwardDir;
            cameraPosition += movement;
            CalculateResultMartix();
        }

        public void Rotate(Point move)
        {
            float xAngle = move.X * cameraRotate;
            longitude += xAngle;

            float yAngle = move.Y * cameraRotate;
            float newLatitude = latitude + yAngle;
            if (newLatitude > -Math.PI / 2 &&
                newLatitude < Math.PI / 2)
            {
                latitude = newLatitude;
            }

            CalculateResultMartix();

            var pitchOri = Quaternion.FromAxisAngle(Vector3.UnitY, -longitude);
            var yawOri = Quaternion.FromAxisAngle(Vector3.UnitX, -latitude);
            this.Orient(pitchOri * yawOri);
        }

        public void Orient(Quaternion orientation)
        {
            Matrix4 newOrientation = Matrix4.CreateFromQuaternion(orientation);
            Orient(newOrientation);
        }

        public void Orient(Matrix4 newOrientation)
        {
            this.forwardDir = new Vector3(newOrientation.M31, newOrientation.M32, newOrientation.M33);
            this.upDir = new Vector3(newOrientation.M21, newOrientation.M22, newOrientation.M23);
            this.rightDir = Vector3.Cross(this.upDir, this.forwardDir).Normalized();
        }
    }
}
