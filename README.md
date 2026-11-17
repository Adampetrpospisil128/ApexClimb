# ApexClimb

A precision 2D vertical platformer built from scratch in C# and Raylib-cs with Clean Architecture, custom AABB physics, and xUnit tests.

<img width="965" height="608" alt="Animation" src="https://github.com/user-attachments/assets/44357ab2-ac7a-4a7f-bbd6-b87b96cd0285" />

## Features

- **Precision Movement & Physics:** Custom AABB collision resolution, variable jump height, coyote time, jump buffering, wall sliding, wall jumping, and an 8-directional dash.
- **Clean Architecture:** Complete decoupling between deterministic physics/game state (`ApexClimb.Core`) and rendering/input (`ApexClimb.Game`).
- **Visual Feedback ("Game Juice"):** Dynamic dash afterimage trails and particle effects running at a locked 60 FPS (`960x540`) using Raylib-cs.
- **Unit Tested:** Core movement mechanics and state transitions verified with automated xUnit tests (`ApexClimb.Tests`).

## Controls

| Action | Key(s) |
| :--- | :--- |
| **Move Left / Right** | `A` / `D` or `←` / `→` |
| **Jump / Wall Jump** | `Space` |
| **8-Directional Dash** | `Left Shift` or `K` (+ Direction) |
| **Quick Restart** | `R` |

## Project Structure

```text
ApexClimb/
├── ApexClimb.Core/      # Pure C# domain & physics engine (PlayerController, PhysicsConfig, LevelData, RectF)
├── ApexClimb.Game/      # Raylib-cs entry point, rendering loop, particle & trail effects
└── ApexClimb.Tests/     # xUnit test suite covering player movement and physics edge cases
```

## Getting Started

### Prerequisites
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download) (or newer)

### Run the Game
```bash
git clone [https://github.com/Adampetrpospisil128/ApexClimb.git](https://github.com/Adampetrpospisil128/ApexClimb.git)
cd ApexClimb
dotnet run --project ApexClimb.Game
```

### Run Unit Tests
```bash
dotnet test
```
