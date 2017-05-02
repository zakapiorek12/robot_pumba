using System;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace robot
{
    partial class MainWindow : GameWindow
    {
        private Camera camera;
        private GLRenderer glRenderer;

        public MainWindow() : base()
        {
            InitEvents();
            Title = "PUMBA";
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            camera = new Camera();
            glRenderer = new GLRenderer();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            GL.Viewport(this.ClientRectangle);
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);
            
            glRenderer.DoScene((float)e.Time, camera);

            SwapBuffers();
        }
    }
}
