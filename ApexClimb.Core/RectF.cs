using System.Numerics;

namespace ApexClimb.Core;

/// <summary>
/// Represents an immutable Axis-Aligned Bounding Box (AABB) in 2D world space.
/// </summary>
public readonly record struct RectF(float X, float Y, float Width, float Height)
{
    public float Left => X;
    public float Right => X + Width;
    public float Top => Y;
    public float Bottom => Y + Height;
    public Vector2 Center => new(X + Width * 0.5f, Y + Height * 0.5f);

    /// <summary>
    /// Determines whether this rectangle overlaps with another bounding box.
    /// </summary>
    public bool Intersects(RectF other) =>
        Left < other.Right &&
        Right > other.Left &&
        Top < other.Bottom &&
        Bottom > other.Top;
}