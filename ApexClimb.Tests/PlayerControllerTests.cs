using System.Numerics;
using ApexClimb.Core;
using Xunit;

namespace ApexClimb.Tests;

/// <summary>
/// Unit tests verifying deterministic physics, wall-jumping, hazard respawns,
/// and input grace windows in <see cref="PlayerController"/>.
/// </summary>
public sealed class PlayerControllerTests
{
    [Fact]
    public void Update_WhenAirborne_AppliesGravityAndTransitionsToFalling()
    {
        var player = new PlayerController(new Vector2(100f, 100f), new Vector2(28f, 36f));

        player.Update(0.016f, new PlayerInput(0f, 0f, false, false), []);

        Assert.True(player.Velocity.Y > 0f);
        Assert.Equal(PlayerState.Falling, player.State);
        Assert.False(player.IsGrounded);
    }

    [Fact]
    public void Update_WhenLandingOnFloor_SetsGroundedAndRechargesDash()
    {
        var floor = new RectF(0f, 200f, 500f, 50f);
        var player = new PlayerController(new Vector2(100f, 160f), new Vector2(28f, 36f));

        player.Update(0.016f, new PlayerInput(1f, 0f, false, true), [floor]);
        Assert.False(player.CanDash);

        for (int i = 0; i < 20; i++)
        {
            player.Update(0.016f, new PlayerInput(0f, 0f, false, false), [floor]);
        }

        Assert.True(player.IsGrounded);
        Assert.True(player.CanDash);
        Assert.Equal(164f, player.Position.Y, precision: 2);
    }

    [Fact]
    public void Update_WithinCoyoteTimeWindow_AllowsJumpAfterLeavingLedge()
    {
        var ledge = new RectF(0f, 200f, 100f, 20f);
        var player = new PlayerController(new Vector2(85f, 164f), new Vector2(28f, 36f));

        player.Update(0.016f, new PlayerInput(0f, 0f, false, false), [ledge]);
        Assert.True(player.IsGrounded);

        player.Update(0.016f, new PlayerInput(1f, 0f, false, false), [ledge]);
        Assert.False(player.IsGrounded);

        player.Update(0.016f, new PlayerInput(1f, 0f, JumpPressed: true, false), [ledge]);
        Assert.Equal(PlayerState.Jumping, player.State);
        Assert.True(player.Velocity.Y < 0f);
    }

    [Fact]
    public void Update_WhenPressingAgainstWallInMidAir_EntersWallSlideAndAllowsWallJump()
    {
        var rightWall = new RectF(200f, 0f, 30f, 400f);
        var player = new PlayerController(new Vector2(171f, 100f), new Vector2(28f, 36f));

        // Step 1: Move right into wall while airborne -> triggers WallSliding
        player.Update(0.016f, new PlayerInput(1f, 0f, false, false), [rightWall]);
        Assert.True(player.IsOnWallRight);
        Assert.Equal(PlayerState.WallSliding, player.State);

        // Step 2: Press jump while on right wall -> launches up and to the left
        player.Update(0.016f, new PlayerInput(1f, 0f, JumpPressed: true, false), [rightWall]);
        Assert.Equal(PlayerState.Jumping, player.State);
        Assert.True(player.Velocity.X < 0f);
        Assert.True(player.Velocity.Y < 0f);
    }

    [Fact]
    public void Update_WhenTouchingHazard_IncrementsDeathCountAndRespawnsAtActiveCheckpoint()
    {
        var level = new LevelData
        {
            SpawnPoint = new Vector2(50f, 200f),
            Solids = [new RectF(0f, 236f, 500f, 40f)],
            Checkpoints = [new RectF(150f, 196f, 30f, 40f)],
            Hazards = [new RectF(300f, 220f, 40f, 16f)]
        };

        var player = new PlayerController(level.SpawnPoint, new Vector2(28f, 36f));

        // Move through checkpoint
        player.Update(0.016f, new PlayerInput(1f, 0f, false, false), level);
        while (player.ActiveCheckpointIndex < 0)
        {
            player.Update(0.016f, new PlayerInput(1f, 0f, false, false), level);
        }

        // Continue walking into hazard
        while (player.DeathCount == 0)
        {
            player.Update(0.016f, new PlayerInput(1f, 0f, false, false), level);
        }

        Assert.Equal(1, player.DeathCount);
        Assert.Equal(151f, player.Position.X, precision: 1);
        Assert.True(player.CanDash);
    }
}