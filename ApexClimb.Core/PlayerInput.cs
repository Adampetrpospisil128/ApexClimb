namespace ApexClimb.Core;

/// <summary>
/// Immutable snapshot of player inputs captured during a single frame.
/// </summary>
public readonly record struct PlayerInput(
    float MoveX,
    float MoveY,
    bool JumpPressed,
    bool DashPressed);