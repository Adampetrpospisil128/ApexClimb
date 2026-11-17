using System.Numerics;

namespace ApexClimb.Core;

/// <summary>
/// Deterministic 2D precision platformer controller featuring separated-axis AABB collision,
/// 8-directional dashing, coyote time, jump buffering, wall-sliding, and wall-jumping.
/// </summary>
public sealed class PlayerController
{
    private readonly PhysicsConfig _config;
    private Vector2 _currentRespawnPoint;
    private float _dashTimer;
    private float _coyoteTimer;
    private float _wallCoyoteTimer;
    private float _jumpBufferTimer;
    private float _wallJumpLockTimer;
    private int _lastWallDirection;
    private Vector2 _dashDirection;

    public Vector2 Position { get; private set; }
    public Vector2 Size { get; }
    public Vector2 Velocity { get; private set; }
    public PlayerState State { get; private set; } = PlayerState.Idle;
    public bool IsGrounded { get; private set; }
    public bool IsOnWallLeft { get; private set; }
    public bool IsOnWallRight { get; private set; }
    public bool CanDash { get; private set; } = true;
    public int FacingDirection { get; private set; } = 1;
    public int ActiveCheckpointIndex { get; private set; } = -1;
    public int DeathCount { get; private set; }
    public bool IsGoalReached { get; private set; }

    public RectF Bounds => new(Position.X, Position.Y, Size.X, Size.Y);

    // Decoupled simulation events consumed by the presentation layer for particles & camera shake
    public event Action? OnJumped;
    public event Action? OnLanded;
    public event Action? OnDashed;
    public event Action<Vector2>? OnDied;
    public event Action<int>? OnCheckpointActivated;

    public PlayerController(Vector2 startPosition, Vector2 size, PhysicsConfig? config = null)
    {
        Position = startPosition;
        _currentRespawnPoint = startPosition;
        Size = size;
        _config = config ?? new PhysicsConfig();
    }

    /// <summary>
    /// Advances the character physics simulation by one time step against the provided level.
    /// </summary>
    public void Update(float deltaTime, PlayerInput input, LevelData level)
    {
        if (deltaTime <= 0f || IsGoalReached) return;

        bool wasGrounded = IsGrounded;

        if (_wallJumpLockTimer <= 0f && MathF.Abs(input.MoveX) > 0.01f)
        {
            FacingDirection = input.MoveX > 0f ? 1 : -1;
        }

        UpdateTimers(deltaTime, input.JumpPressed);
        HandleDashTrigger(input);
        ApplyMovementAndGravity(deltaTime, input);
        ResolveCollisions(deltaTime, level.Solids);

        if (!wasGrounded && IsGrounded)
        {
            OnLanded?.Invoke();
        }

        if (EvaluateLevelTriggers(level)) return;

        UpdateState(input);
    }

    /// <summary>
    /// Overload supporting collision testing against raw solid geometry without level triggers.
    /// </summary>
    public void Update(float deltaTime, PlayerInput input, IReadOnlyList<RectF> colliders)
    {
        var minimalLevel = new LevelData
        {
            SpawnPoint = _currentRespawnPoint,
            Solids = colliders
        };
        Update(deltaTime, input, minimalLevel);
    }

    /// <summary>
    /// Resets the player back to the initial spawn point for a fresh speedrun attempt.
    /// </summary>
    public void ResetRun(Vector2 initialSpawn)
    {
        _currentRespawnPoint = initialSpawn;
        Position = initialSpawn;
        Velocity = Vector2.Zero;
        State = PlayerState.Idle;
        IsGrounded = false;
        IsOnWallLeft = false;
        IsOnWallRight = false;
        CanDash = true;
        ActiveCheckpointIndex = -1;
        DeathCount = 0;
        IsGoalReached = false;
        _dashTimer = 0f;
        _coyoteTimer = 0f;
        _wallCoyoteTimer = 0f;
        _jumpBufferTimer = 0f;
        _wallJumpLockTimer = 0f;
    }

    private void UpdateTimers(float dt, bool jumpPressed)
    {
        _coyoteTimer = IsGrounded ? _config.CoyoteTime : MathF.Max(0f, _coyoteTimer - dt);
        _jumpBufferTimer = jumpPressed ? _config.JumpBufferTime : MathF.Max(0f, _jumpBufferTimer - dt);
        _wallJumpLockTimer = MathF.Max(0f, _wallJumpLockTimer - dt);

        if (!IsGrounded && (IsOnWallLeft || IsOnWallRight))
        {
            _wallCoyoteTimer = _config.CoyoteTime;
            _lastWallDirection = IsOnWallLeft ? -1 : 1;
        }
        else
        {
            _wallCoyoteTimer = MathF.Max(0f, _wallCoyoteTimer - dt);
        }
    }

    private void HandleDashTrigger(PlayerInput input)
    {
        if (!input.DashPressed || !CanDash || _dashTimer > 0f) return;

        CanDash = false;
        _dashTimer = _config.DashDuration;
        _wallJumpLockTimer = 0f;

        Vector2 rawDirection = new(input.MoveX, input.MoveY);
        _dashDirection = rawDirection != Vector2.Zero
            ? Vector2.Normalize(rawDirection)
            : new Vector2(FacingDirection, 0f);

        Velocity = _dashDirection * _config.DashSpeed;
        OnDashed?.Invoke();
    }

    private void ApplyMovementAndGravity(float dt, PlayerInput input)
    {
        if (_dashTimer > 0f)
        {
            _dashTimer -= dt;
            Velocity = _dashDirection * _config.DashSpeed;

            if (_dashTimer <= 0f)
            {
                Velocity *= _config.DashEndMomentumMultiplier;
            }
            return;
        }

        float velocityX = _wallJumpLockTimer > 0f
            ? Velocity.X
            : input.MoveX * _config.MoveSpeed;

        float velocityY = Velocity.Y + _config.Gravity * dt;

        // Clamp fall speed when actively gripping a wall
        bool isPressingIntoWall = (IsOnWallLeft && input.MoveX < -0.01f) || (IsOnWallRight && input.MoveX > 0.01f);
        if (!IsGrounded && isPressingIntoWall && velocityY > _config.WallSlideMaxSpeed)
        {
            velocityY = _config.WallSlideMaxSpeed;
        }

        // Priority 1: Grounded or Coyote-Time Jump
        if (_jumpBufferTimer > 0f && _coyoteTimer > 0f)
        {
            velocityY = _config.JumpForce;
            IsGrounded = false;
            _coyoteTimer = 0f;
            _jumpBufferTimer = 0f;
            OnJumped?.Invoke();
        }
        // Priority 2: Wall-Jump while airborne
        else if (_jumpBufferTimer > 0f && _wallCoyoteTimer > 0f)
        {
            int kickDirection = -_lastWallDirection;
            velocityX = kickDirection * _config.WallJumpVelocityX;
            velocityY = _config.WallJumpVelocityY;
            FacingDirection = kickDirection;
            _wallJumpLockTimer = _config.WallJumpLockTime;
            _wallCoyoteTimer = 0f;
            _jumpBufferTimer = 0f;
            OnJumped?.Invoke();
        }

        Velocity = new Vector2(velocityX, velocityY);
    }

    private void ResolveCollisions(float dt, IReadOnlyList<RectF> colliders)
    {
        // Pass 1: Horizontal axis
        float nextX = Position.X + Velocity.X * dt;
        RectF horizontalBox = new(nextX, Position.Y, Size.X, Size.Y);

        foreach (var solid in colliders)
        {
            if (!horizontalBox.Intersects(solid)) continue;

            if (Velocity.X > 0f) nextX = solid.Left - Size.X;
            else if (Velocity.X < 0f) nextX = solid.Right;

            Velocity = Velocity with { X = 0f };
            horizontalBox = horizontalBox with { X = nextX };
        }

        // Pass 2: Vertical axis
        float nextY = Position.Y + Velocity.Y * dt;
        IsGrounded = false;
        RectF verticalBox = new(nextX, nextY, Size.X, Size.Y);

        foreach (var solid in colliders)
        {
            if (!verticalBox.Intersects(solid)) continue;

            if (Velocity.Y > 0f)
            {
                nextY = solid.Top - Size.Y;
                IsGrounded = true;
                CanDash = true;
            }
            else if (Velocity.Y < 0f)
            {
                nextY = solid.Bottom;
            }

            Velocity = Velocity with { Y = 0f };
            verticalBox = verticalBox with { Y = nextY };
        }

        Position = new Vector2(nextX, nextY);

        // Pass 3: 1px lateral wall sensors for reliable wall-slide and wall-jump detection
        IsOnWallLeft = false;
        IsOnWallRight = false;
        RectF leftSensor = new(Position.X - 1f, Position.Y + 4f, 1f, Size.Y - 8f);
        RectF rightSensor = new(Position.X + Size.X, Position.Y + 4f, 1f, Size.Y - 8f);

        foreach (var solid in colliders)
        {
            if (leftSensor.Intersects(solid)) IsOnWallLeft = true;
            if (rightSensor.Intersects(solid)) IsOnWallRight = true;
        }
    }

    private bool EvaluateLevelTriggers(LevelData level)
    {
        RectF currentBounds = Bounds;

        // Checkpoints
        for (int i = 0; i < level.Checkpoints.Count; i++)
        {
            if (i > ActiveCheckpointIndex && currentBounds.Intersects(level.Checkpoints[i]))
            {
                ActiveCheckpointIndex = i;
                RectF cp = level.Checkpoints[i];
                _currentRespawnPoint = new Vector2(cp.Center.X - Size.X * 0.5f, cp.Bottom - Size.Y);
                OnCheckpointActivated?.Invoke(i);
            }
        }

        // Lethal hazards
        foreach (var hazard in level.Hazards)
        {
            if (currentBounds.Intersects(hazard))
            {
                Vector2 deathPosition = Bounds.Center;
                DeathCount++;
                Position = _currentRespawnPoint;
                Velocity = Vector2.Zero;
                CanDash = true;
                _dashTimer = 0f;
                _wallJumpLockTimer = 0f;
                _coyoteTimer = 0f;
                _wallCoyoteTimer = 0f;
                State = PlayerState.Idle;
                OnDied?.Invoke(deathPosition);
                return true;
            }
        }

        // Summit goal
        if (level.GoalZone.Width > 0f && currentBounds.Intersects(level.GoalZone))
        {
            IsGoalReached = true;
            Velocity = Vector2.Zero;
        }

        return false;
    }

    private void UpdateState(PlayerInput input)
    {
        bool isPressingIntoWall = (IsOnWallLeft && input.MoveX < -0.01f) || (IsOnWallRight && input.MoveX > 0.01f);

        if (_dashTimer > 0f)
        {
            State = PlayerState.Dashing;
        }
        else if (!IsGrounded && isPressingIntoWall && Velocity.Y > 0f)
        {
            State = PlayerState.WallSliding;
        }
        else if (!IsGrounded)
        {
            State = Velocity.Y < 0f ? PlayerState.Jumping : PlayerState.Falling;
        }
        else if (MathF.Abs(Velocity.X) > 0.01f)
        {
            State = PlayerState.Running;
        }
        else
        {
            State = PlayerState.Idle;
        }
    }
}