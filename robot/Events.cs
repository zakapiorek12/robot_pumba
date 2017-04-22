using OpenTK.Input;
using System.Drawing;

namespace robot
{
    partial class MainWindow
    {
        MouseButton? mouseButtonDown = null;
        Point mousePosition;

        private void InitEvents()
        {
            KeyDown += MyWindow_KeyDown;
            MouseWheel += MyWindow_MouseWheel;
            MouseMove += MyWindow_MouseMove;
            MouseDown += MyWindow_MouseDown;
            MouseUp += MyWindow_MouseUp;
        }

        private void MyWindow_KeyDown(object sender, KeyboardKeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Escape:
                    Close();
                    break;
                case Key.Left:
                case Key.Right:
                case Key.Up:
                case Key.Down:
                    camera.MoveCamera((MoveEnum)e.Key);
                    break;
            }
        }


        private void MyWindow_MouseWheel(object sender, OpenTK.Input.MouseWheelEventArgs e)
        {
            if (e.Delta > 0)
                camera.MoveCamera(MoveEnum.Forward);
            else
                camera.MoveCamera(MoveEnum.Backward);
        }

        private void MyWindow_MouseUp(object sender, OpenTK.Input.MouseButtonEventArgs e)
        {
            mouseButtonDown = null;
        }

        private void MyWindow_MouseDown(object sender, OpenTK.Input.MouseButtonEventArgs e)
        {
            mouseButtonDown = e.Button;
            mousePosition = e.Position;
        }

        private void MyWindow_MouseMove(object sender, OpenTK.Input.MouseMoveEventArgs e)
        {
            if (mousePosition != null &&
                mouseButtonDown != null)
            {
                Point move = new Point(e.Position.X - mousePosition.X, e.Position.Y - mousePosition.Y);
                camera.Rotate(move);
            }
            mousePosition = e.Position;
        }
    }
}
