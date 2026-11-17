namespace ApexClimb.Core;

/// <summary>
/// Discrete behavioral states of the player's Finite State Machine (FSM).
/// </summary>
public enum PlayerState
{
    Idle,
    Running,
    Jumping,
    Falling,
    WallSliding,
    Dashing
}