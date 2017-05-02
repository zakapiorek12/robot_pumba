namespace robot
{
    public abstract class AnimatedObject
    {
        protected Mesh[] meshes;

        protected AnimatedObject()
        {
            meshes = ProvideObjectMeshes();
        }

        public void AddOnScene()
        {
            foreach(var m in meshes)
                GLRenderer.AddMeshToDraw(m);
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
