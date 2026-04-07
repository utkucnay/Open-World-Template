namespace Glai.Tween.Core
{
    public interface ITweenManager
    {
        static ITweenManager Instance { get; protected set; }
        TweenState TweenState { get; }
    }
}