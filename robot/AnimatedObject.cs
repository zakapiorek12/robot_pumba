namespace robot
{
    public abstract class AnimatedObject
    {
        public Mesh[] meshes { get; set; }

        protected AnimatedObject()
        {
            meshes = ProvideObjectMeshes();
        }

        public void AddOnScene(GLRenderer.MyShaderType shaderType)
        {
            foreach(var m in meshes)
                GLRenderer.AddMeshToDraw(m, shaderType);
            GLRenderer.AddAnimatedObject(this);
        }

        public void RemoveFromScene()
        {
            foreach(var m in meshes)
                GLRenderer.RemoveMeshToDraw(m);
            GLRenderer.RemoveAnimatedObject(this);
        }

        public abstract Mesh[] ProvideObjectMeshes();
        public abstract void DoAnimation(float deltaTime);
    }
}
