# MematiHack v1.0

MematiHack is a high-performance external CS2 project featuring a modern ImGui overlay, built-in menu, and advanced features for Counter-Strike 2.

## Features

### Aimbot
- **AimBot** — Automatic aim at enemies with bone targeting, recoil control (RCS), smoothing, and dynamic FOV.

### TriggerBot
- **TriggerBot** — Auto-fires when crosshair is on a valid enemy while holding the trigger key.

### Movement
- **BunnyHop** — Auto-jump with force-jump for bunny hopping.

### ESP
- **Box ESP** — 2D bounding boxes around entities with health bar, armor bar, weapon info, name, distance, flags, and snaplines.
- **Skeleton ESP** — Bone-connected skeleton rendering on living entities.
- **Item ESP** — Dropped weapon names with distance on screen.
- **Glow ESP** — Writes glow color/type/style to entity memory for visual highlighting.
- **Offscreen ESP** — Directional arrows at screen edges pointing to off-screen enemies.
- **Aim Crosshair** — Static crosshair + recoil dot showing bullet landing point.

### Visuals
- **Radar** — 2D rotating radar window showing nearby enemies and bomb position.
- **Anti-Flash** — Instantly nullifies flashbang effects.
- **Bomb Timer** — C4 timer panel with progress bar, defuse bar, and world marker.
- **Vote Teller** — Displays in-progress vote details (issue, team, yes/potential counts).
- **Watermark** — Top-right overlay with cheat name, FPS, and current time.
- **Spectator List** — Lists players spectating you in a top-right panel.
- **Velocity Graph** — Real-time line graph of player velocity over last 120 frames.
- **Grenade Helper** — Map markers with throw instructions and aim-angle application (loads from JSON lineups).

### Feedback
- **Hitmarker** — Animated X-shaped hit markers at screen center on damage.
- **Damage Text** — Floating damage numbers or "KILL" text at hit positions with fade-out.
- **HitSound** — Configurable .wav hit sound via MCI on enemy damage.

## Installation
1. Download and install [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).
2. Clone this repository.
3. Open the solution in Visual Studio 2022 or use the command line:
   ```bash
   dotnet build
   ```

## Usage
1. Launch Counter-Strike 2.
2. Run `CS2Cheat.exe`.
3. Press **INSERT** to toggle the menu.

## Disclaimer
This project is for educational purposes only. Use at your own risk. I am not responsible for any bans or issues resulting from the use of this software.
