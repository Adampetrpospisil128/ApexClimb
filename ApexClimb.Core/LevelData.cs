using System.Numerics;

namespace ApexClimb.Core;

/// <summary>
/// Encapsulates static geometry, lethal hazards, checkpoints, and the goal zone for a stage.
/// </summary>
public sealed class LevelData
{
    public Vector2 SpawnPoint { get; init; }
    public IReadOnlyList<RectF> Solids { get; init; } = [];
    public IReadOnlyList<RectF> Hazards { get; init; } = [];
    public IReadOnlyList<RectF> Checkpoints { get; init; } = [];
    public RectF GoalZone { get; init; }

    /// <summary>
    /// Builds the default vertical tower level featuring platforming, a wall-jump chimney,
    /// hazards, checkpoints, and a summit goal.
    /// </summary>
    public static LevelData CreateVerticalTower()
    {
        RectF[] solids =
        [
            // Outer tower boundaries and ground floor
            new RectF(0f, 480f, 960f, 60f),       // Base floor
            new RectF(40f, -1350f, 30f, 1830f),   // Left outer tower wall
            new RectF(890f, -1350f, 30f, 1830f),  // Right outer tower wall

            // Section 1: Lower Ascent (Y: 480 -> 130)
            new RectF(240f, 395f, 180f, 20f),
            new RectF(500f, 310f, 180f, 20f),
            new RectF(220f, 220f, 180f, 20f),
            new RectF(460f, 130f, 220f, 20f),     // Checkpoint 1 platform

            // Section 2: Wall-Jump Chimney (Y: 40 -> -480)
            new RectF(320f, -420f, 28f, 460f),    // Inner left chimney wall
            new RectF(540f, -480f, 28f, 520f),    // Inner right chimney wall
            new RectF(568f, -480f, 220f, 24f),    // Checkpoint 2 platform

            // Section 3: Summit Gauntlet (Y: -480 -> -1120)
            new RectF(270f, -575f, 160f, 20f),
            new RectF(160f, -820f, 26f, 200f),    // Wall-jump pillar
            new RectF(320f, -820f, 160f, 20f),
            new RectF(560f, -915f, 150f, 20f),
            new RectF(760f, -1060f, 26f, 120f),   // Final dash-kick pillar
            new RectF(360f, -1120f, 240f, 24f)    // Summit platform
        ];

        RectF[] hazards =
        [
            new RectF(450f, 464f, 440f, 16f),     // Floor spikes under lower ascent
            new RectF(526f, -140f, 14f, 70f),     // Chimney right-wall spikes
            new RectF(348f, -320f, 14f, 70f),     // Chimney left-wall spikes
            new RectF(330f, -591f, 40f, 16f)      // Island center spikes
        ];

        RectF[] checkpoints =
        [
            new RectF(555f, 90f, 30f, 40f),       // Checkpoint 1
            new RectF(660f, -520f, 30f, 40f)      // Checkpoint 2
        ];

        return new LevelData
        {
            SpawnPoint = new Vector2(120f, 444f),
            Solids = solids,
            Hazards = hazards,
            Checkpoints = checkpoints,
            GoalZone = new RectF(455f, -1180f, 50f, 60f)
        };
    }
}