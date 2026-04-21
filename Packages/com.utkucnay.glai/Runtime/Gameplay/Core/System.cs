using Glai.Core;
using Glai.Module;

namespace Glai.Gameplay
{
    public abstract class System : Object, IStart, ITick, ILateTick
    {
        public abstract void Start();
        public abstract void Tick(float deltaTime);
        public abstract void LateTick(float deltaTime);
    }
}