using System;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace robot
{
    partial class MainWindow : GameWindow
    {
        private Camera camera;
        private GLRenderer glRenderer;

        public MainWindow() : 
            base(800, 600, new GraphicsMode(new ColorFormat(8, 8, 8, 8), 24, 8))
        {
            InitEvents();
            Title = "PUMBA";
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            camera = new Camera();
            glRenderer = new GLRenderer(this.Width, this.Height);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            GL.Viewport(this.ClientRectangle);
            glRenderer.CreateProjectionMatrix(this.Width, this.Height);
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);
            
            glRenderer.DoScene((float)e.Time, camera);

            SwapBuffers();
        }
    }
}
