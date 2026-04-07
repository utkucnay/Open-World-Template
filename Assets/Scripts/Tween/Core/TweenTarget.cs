
namespace Glai.Tween.Core
{
    public struct TweenTarget
    {
        public enum TargetType
        {
            Transform,
            SpriteRenderer,
            CanvasGroup,
            Material,
        }

        public enum PropertyType
        {
            Position,
            Rotation,
            Scale,
            Color,
            Alpha,
        }

        public TargetType targetType;
        public PropertyType propertyType;
        public int targetObjectId;

        public TweenTarget(int targetObjectId, TargetType targetType, PropertyType propertyType)
        {
            this.targetObjectId = targetObjectId;
            this.propertyType = propertyType;
            this.targetType = targetType;
        }
    }
}
