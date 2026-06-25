## Summary

### Current Goal
Fix wallhack not showing ESP boxes for any entities. Debug log showed all entities with `HP=0, Life=False, Team=Unknown`.

### Root Cause
Entity list traversal formula was outdated/invalid for current CS2 patch:

1. **`ReadControllerBase` (Entity.cs:38)**: Used stale hi/lo formula (`listEntry = *(CGameEntitySystem + 16 + 8*entryIndex)`) with a C# operator precedence bug: `Id & 0x7FFF >> 9` parsed as `Id & (0x7FFF>>9) = Id & 0x3F`, making `entryIndex = Id` instead of 0 for all player indices (0-63). This read from arbitrary CGameEntitySystem internal fields instead of the entity list chunk pointer array.

2. **Stride 120 → 112**: Entity slot stride changed from 120 (0x78) to 112 (0x70) in CS2. Three locations used old stride 120.

### Changes Made

**Entity.cs:**
- `ReadControllerBase`: Replaced hi/lo formula with flat-array approach (matches hzqst/CS2_External working reference). Reads `*(CGameEntitySystem + 0x10)` as list base, then reads entity at `base + (Id+1)*112`. (entity indices are 1-based)
- `ReadAddressBase`: Fixed stride 120 → 112; added `playerPawn == -1` guard

**SpectatorList.cs:**
- `ResolveHandleToPawn`: Fixed stride 120 → 112

**EntityBase.cs:**
- `m_lifeState`: Changed from `Read<bool>` to `Read<byte>() == 0` (earlier fix; `Marshal.SizeOf<bool> = 4` was reading 4 bytes)

### Verified Correct (no change needed)
- **Player.cs**: Local player reads from `dwLocalPlayerController` / `dwLocalPlayerPawn` directly — confirmed working (Hitmarker/HitSound succeed)
- **TriggerBot.cs**: Already uses stride 112 and correct hi/lo formula
- **EntityBase.cs:ReadEntityFromHandle**: Already uses stride 112 and correct hi/lo formula

### Working Reference
**hzqst/CS2_External** (GitHub, branch `master`):
- Entity loop: `base = *(CGameEntitySystem + 0x10)` once → `*(base + (i+1)*0x70)` per entity
- Handle resolution: `*(CGameEntitySystem + 0x10 + 8*hi)` → `*(chunk + 0x70*lo)`
- `Game.cpp:UpdateEntityListEntry()`: `entry = *(client_base + dwEntityList)`; `entry = *(entry + 0x10)`
