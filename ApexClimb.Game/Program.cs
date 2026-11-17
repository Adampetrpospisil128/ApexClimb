using System.Numerics;
using ApexClimb.Core;
using Raylib_cs;
using Color = Raylib_cs.Color;

const int ScreenWidth = 960;
const int ScreenHeight = 540;
const float MaxDeltaTime = 0.05f;

Raylib.InitWindow(ScreenWidth, ScreenHeight, "ApexClimb - Precision 2D Platformer");
Raylib.SetTargetFPS(60);

LevelData level = LevelData.CreateVerticalTower();
var player = new PlayerController(level.SpawnPoint, new Vector2(28f, 36f));

// Visual "Game Juice" state
var particles = new List<Particle>();
var dashTrails = new List<DashAfterimage>();
var random = new Random();
float shakeTimer = 0f;
float shakeIntensity = 0f;
float dashTrailTimer = 0f;
float checkpointBannerTimer = 0f;
float runElapsedSeconds = 0f;

// Subscribe presentation effects to decoupled Core simulation events
player.OnJumped += () =>
{
    SpawnBurst(particles, random, new Vector2(player.Bounds.Center.X, player.Bounds.Bottom), 8, new Color(203, 213, 225, 255), 140f);
};

player.OnLanded += () =>
{
    SpawnBurst(particles, random, new Vector2(player.Bounds.Center.X, player.Bounds.Bottom), 10, new Color(148, 163, 184, 255), 160f);
};

player.OnDashed += () =>
{
    shakeTimer = 0.09f;
    shakeIntensity = 4.5f;
    SpawnBurst(particles, random, player.Bounds.Center, 14, new Color(56, 189, 248, 255), 240f);
};

player.OnDied += deathPos =>
{
    shakeTimer = 0.22f;
    shakeIntensity = 9f;
    SpawnBurst(particles, random, deathPos, 28, new Color(244, 63, 94, 255), 320f);
};

player.OnCheckpointActivated += _ =>
{
    checkpointBannerTimer = 2.0f;
    SpawnBurst(particles, random, player.Bounds.Center, 20, new Color(52, 211, 153, 255), 220f);
};

var camera = new Camera2D
{
    Offset = new Vector2(ScreenWidth * 0.5f, ScreenHeight * 0.58f),
    Target = new Vector2(ScreenWidth * 0.5f, player.Position.Y),
    Rotation = 0f,
    Zoom = 1f
};

while (!Raylib.WindowShouldClose())
{
    float dt = MathF.Min(Raylib.GetFrameTime(), MaxDeltaTime);

    if (Raylib.IsKeyPressed(KeyboardKey.R))
    {
        player.ResetRun(level.SpawnPoint);
        particles.Clear();
        dashTrails.Clear();
        runElapsedSeconds = 0f;
    }

    if (!player.IsGoalReached)
    {
        runElapsedSeconds += dt;
        PlayerInput input = PollInput();
        player.Update(dt, input, level);
    }

    // Spawn dash afterimages while dashing
    if (player.State == PlayerState.Dashing)
    {
        dashTrailTimer -= dt;
        if (dashTrailTimer <= 0f)
        {
            dashTrails.Add(new DashAfterimage(player.Position, player.Size, 0.22f, 0.22f));
            dashTrailTimer = 0.025f;
        }
    }

    UpdateParticles(particles, dt);
    UpdateDashTrails(dashTrails, dt);
    checkpointBannerTimer = MathF.Max(0f, checkpointBannerTimer - dt);

    // Smooth vertical camera follow with screen shake
    float desiredY = MathF.Min(player.Position.Y - 40f, 270f);
    float smoothY = camera.Target.Y + (desiredY - camera.Target.Y) * MathF.Min(1f, dt * 8f);
    Vector2 shakeOffset = Vector2.Zero;

    if (shakeTimer > 0f)
    {
        shakeTimer -= dt;
        shakeOffset = new Vector2(
            (float)(random.NextDouble() * 2 - 1) * shakeIntensity,
            (float)(random.NextDouble() * 2 - 1) * shakeIntensity);
    }

    camera.Target = new Vector2(ScreenWidth * 0.5f + shakeOffset.X, smoothY + shakeOffset.Y);

    RenderScene(camera, level, player, particles, dashTrails, runElapsedSeconds, checkpointBannerTimer);
}

Raylib.CloseWindow();

static PlayerInput PollInput()
{
    float moveX = 0f;
    if (Raylib.IsKeyDown(KeyboardKey.A) || Raylib.IsKeyDown(KeyboardKey.Left)) moveX -= 1f;
    if (Raylib.IsKeyDown(KeyboardKey.D) || Raylib.IsKeyDown(KeyboardKey.Right)) moveX += 1f;

    float moveY = 0f;
    if (Raylib.IsKeyDown(KeyboardKey.W) || Raylib.IsKeyDown(KeyboardKey.Up)) moveY -= 1f;
    if (Raylib.IsKeyDown(KeyboardKey.S) || Raylib.IsKeyDown(KeyboardKey.Down)) moveY += 1f;

    bool jumpPressed = Raylib.IsKeyPressed(KeyboardKey.Space);
    bool dashPressed = Raylib.IsKeyPressed(KeyboardKey.LeftShift) || Raylib.IsKeyPressed(KeyboardKey.K);

    return new PlayerInput(moveX, moveY, jumpPressed, dashPressed);
}

static void RenderScene(
    Camera2D camera,
    LevelData level,
    PlayerController player,
    List<Particle> particles,
    List<DashAfterimage> dashTrails,
    float elapsedSeconds,
    float checkpointBannerTimer)
{
    Raylib.BeginDrawing();
    Raylib.ClearBackground(new Color(18, 20, 32, 255));

    Raylib.BeginMode2D(camera);

    // Background elevation grid lines for visual speed reference
    for (int y = -1400; y <= 500; y += 100)
    {
        Raylib.DrawLine(70, y, 890, y, new Color(30, 34, 52, 255));
    }

    // Render solid platforms and walls
    foreach (var solid in level.Solids)
    {
        Raylib.DrawRectangle((int)solid.X, (int)solid.Y, (int)solid.Width, (int)solid.Height, new Color(68, 79, 105, 255));
        Raylib.DrawRectangleLines((int)solid.X, (int)solid.Y, (int)solid.Width, (int)solid.Height, new Color(100, 116, 148, 255));
    }

    // Render triangular spikes for hazards
    foreach (var hazard in level.Hazards)
    {
        DrawSpikeCluster(hazard, new Color(244, 63, 94, 255));
    }

    // Render checkpoints
    for (int i = 0; i < level.Checkpoints.Count; i++)
    {
        RectF cp = level.Checkpoints[i];
        bool activated = i <= player.ActiveCheckpointIndex;
        Color flagColor = activated ? new Color(52, 211, 153, 255) : new Color(100, 116, 139, 255);

        Raylib.DrawRectangle((int)cp.X + 4, (int)cp.Y, 4, (int)cp.Height, new Color(203, 213, 225, 255));
        Raylib.DrawRectangle((int)cp.X + 8, (int)cp.Y + 2, 20, 14, flagColor);
    }

    // Render Summit Goal Beacon
    float pulse = 0.75f + 0.25f * MathF.Sin((float)Raylib.GetTime() * 5f);
    Color goalColor = new Color((byte)250, (byte)204, (byte)21, (byte)(255 * pulse));
    Raylib.DrawRectangle((int)level.GoalZone.X, (int)level.GoalZone.Y, (int)level.GoalZone.Width, (int)level.GoalZone.Height, goalColor);
    Raylib.DrawRectangleLines((int)level.GoalZone.X, (int)level.GoalZone.Y, (int)level.GoalZone.Width, (int)level.GoalZone.Height, Color.RayWhite);

    // Render dash afterimage trail
    foreach (var trail in dashTrails)
    {
        byte alpha = (byte)(160 * (trail.Life / trail.MaxLife));
        Raylib.DrawRectangleV(trail.Position, trail.Size, new Color((byte)56, (byte)189, (byte)248, alpha));
    }

    // Render player body and directional visor
    Color playerColor = player.CanDash
        ? new Color(56, 189, 248, 255)
        : new Color(148, 163, 184, 255);

    Raylib.DrawRectangleV(player.Position, player.Size, playerColor);
    float eyeX = player.FacingDirection > 0 ? player.Position.X + 16f : player.Position.X + 4f;
    Raylib.DrawRectangle((int)eyeX, (int)(player.Position.Y + 8f), 8, 5, new Color(15, 23, 42, 255));

    // Render particles
    foreach (var p in particles)
    {
        byte alpha = (byte)(255 * (p.Life / p.MaxLife));
        Color c = new(p.Color.R, p.Color.G, p.Color.B, alpha);
        Raylib.DrawRectangleV(p.Position, new Vector2(p.Size, p.Size), c);
    }

    Raylib.EndMode2D();

    // Fixed Screen-Space HUD
    int altitudeMeters = Math.Max(0, (int)((444f - player.Position.Y) / 10f));
    Raylib.DrawText("Move: A/D | Jump/Wall-Jump: Space | Dash: Shift/K | Restart: R", 18, 16, 16, Color.RayWhite);
    Raylib.DrawText($"State: {player.State} | Dash: {(player.CanDash ? "READY" : "EMPTY")} | Deaths: {player.DeathCount}", 18, 38, 16, playerColor);
    Raylib.DrawText($"Time: {elapsedSeconds:00.00}s   Altitude: {altitudeMeters}m / 160m", 630, 16, 18, new Color(250, 204, 21, 255));

    if (checkpointBannerTimer > 0f)
    {
        Raylib.DrawText("CHECKPOINT REACHED!", ScreenWidth / 2 - 115, 75, 20, new Color(52, 211, 153, 255));
    }

    if (player.IsGoalReached)
    {
        Raylib.DrawRectangle(200, 180, 560, 160, new Color(15, 23, 42, 235));
        Raylib.DrawRectangleLines(200, 180, 560, 160, new Color(250, 204, 21, 255));
        Raylib.DrawText("SUMMIT REACHED!", 345, 205, 28, new Color(250, 204, 21, 255));
        Raylib.DrawText($"Final Time: {elapsedSeconds:00.00}s  |  Deaths: {player.DeathCount}", 315, 250, 20, Color.RayWhite);
        Raylib.DrawText("Press [R] to play again", 365, 290, 18, new Color(56, 189, 248, 255));
    }

    Raylib.EndDrawing();
}

static void DrawSpikeCluster(RectF area, Color color)
{
    if (area.Width >= area.Height)
    {
        // Horizontal row of upward-pointing spikes
        const float spikeWidth = 14f;
        int count = Math.Max(1, (int)(area.Width / spikeWidth));
        float step = area.Width / count;

        for (int i = 0; i < count; i++)
        {
            float x0 = area.Left + i * step;
            float x1 = x0 + step;
            Vector2 tip = new((x0 + x1) * 0.5f, area.Top);
            Vector2 bottomLeft = new(x0, area.Bottom);
            Vector2 bottomRight = new(x1, area.Bottom);
            Raylib.DrawTriangle(tip, bottomLeft, bottomRight, color);
        }
    }
    else
    {
        // Vertical column of wall spikes pointing toward the inner shaft
        const float spikeHeight = 14f;
        int count = Math.Max(1, (int)(area.Height / spikeHeight));
        float step = area.Height / count;
        bool pointsLeft = area.Left > 450f;

        for (int i = 0; i < count; i++)
        {
            float y0 = area.Top + i * step;
            float y1 = y0 + step;
            if (pointsLeft)
            {
                Vector2 tip = new(area.Left, (y0 + y1) * 0.5f);
                Raylib.DrawTriangle(tip, new Vector2(area.Right, y1), new Vector2(area.Right, y0), color);
            }
            else
            {
                Vector2 tip = new(area.Right, (y0 + y1) * 0.5f);
                Raylib.DrawTriangle(tip, new Vector2(area.Left, y0), new Vector2(area.Left, y1), color);
            }
        }
    }
}

static void SpawnBurst(List<Particle> particles, Random rng, Vector2 origin, int count, Color color, float maxSpeed)
{
    for (int i = 0; i < count; i++)
    {
        double angle = rng.NextDouble() * Math.PI * 2.0;
        float speed = (float)(0.25 + rng.NextDouble() * 0.75) * maxSpeed;
        Vector2 vel = new(MathF.Cos((float)angle) * speed, MathF.Sin((float)angle) * speed);
        float life = 0.25f + (float)rng.NextDouble() * 0.25f;
        particles.Add(new Particle(origin, vel, life, life, color, 4f));
    }
}

static void UpdateParticles(List<Particle> particles, float dt)
{
    for (int i = particles.Count - 1; i >= 0; i--)
    {
        Particle p = particles[i];
        p.Life -= dt;
        if (p.Life <= 0f)
        {
            particles.RemoveAt(i);
            continue;
        }
        p.Position += p.Velocity * dt;
        p.Velocity *= 0.92f;
        particles[i] = p;
    }
}

static void UpdateDashTrails(List<DashAfterimage> trails, float dt)
{
    for (int i = trails.Count - 1; i >= 0; i--)
    {
        DashAfterimage t = trails[i];
        t.Life -= dt;
        if (t.Life <= 0f)
        {
            trails.RemoveAt(i);
            continue;
        }
        trails[i] = t;
    }
}

file struct Particle(Vector2 position, Vector2 velocity, float life, float maxLife, Color color, float size)
{
    public Vector2 Position = position;
    public Vector2 Velocity = velocity;
    public float Life = life;
    public float MaxLife = maxLife;
    public Color Color = color;
    public float Size = size;
}

file struct DashAfterimage(Vector2 position, Vector2 size, float life, float maxLife)
{
    public Vector2 Position = position;
    public Vector2 Size = size;
    public float Life = life;
    public float MaxLife = maxLife;
}