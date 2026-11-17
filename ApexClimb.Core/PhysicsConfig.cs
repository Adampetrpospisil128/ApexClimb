namespace ApexClimb.Core;

/// <summary>
/// Tunable movement and "game feel" constants governing player physics.
/// </summary>
public sealed record PhysicsConfig
{
    public float Gravity { get; init; } = 1800f;
    public float MoveSpeed { get; init; } = 320f;
    public float JumpForce { get; init; } = -620f;
    public float DashSpeed { get; init; } = 780f;
    public float DashDuration { get; init; } = 0.16f;
    public float DashEndMomentumMultiplier { get; init; } = 0.45f;

    /// <summary>Maximum downward speed while sliding against a vertical wall.</summary>
    public float WallSlideMaxSpeed { get; init; } = 140f;

    /// <summary>Horizontal launch speed when kicking off a wall.</summary>
    public float WallJumpVelocityX { get; init; } = 360f;

    /// <summary>Vertical launch impulse when kicking off a wall.</summary>
    public float WallJumpVelocityY { get; init; } = -600f;

    /// <summary>Duration during which horizontal steering is locked after a wall-jump.</summary>
    public float WallJumpLockTime { get; init; } = 0.14f;

    /// <summary>Grace window allowing a jump shortly after leaving a ledge or wall.</summary>
    public float CoyoteTime { get; init; } = 0.10f;

    /// <summary>Buffer window registering a jump pressed slightly before landing.</summary>
    public float JumpBufferTime { get; init; } = 0.10f;
}