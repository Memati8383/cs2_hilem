# Plan: Fix Static Bomb Timer Progress Bar

## Objective
The goal is to fix the issue where the Bomb Timer's progress bar and time are not decreasing. This is likely due to incorrect memory offsets for `GlobalVars` or an issue with the time calculation logic.

## Research Findings
- `m_curTime` in CS2 `GlobalVars` is typically at offset `0x2C`.
- `m_realTime` is usually at `0x28`.
- The current implementation reads from `0x2C`.
- The project uses a 112-byte step for the entity list, which has been applied to `BombTimer.cs`.

## Proposed Solution
1. **Debug Logging:** Add console logging to `BombTimer.cs` to monitor `globalVarsPtr`, `currentTime`, and `TimeLeft` in real-time.
2. **Verify Offsets:** If `currentTime` is not changing, I will try reading from `0x28` (realtime) or other nearby offsets to see which one provides a moving value.
3. **Refine Logic:** Ensure `TimeLeft` is calculated correctly using the `m_flC4Blow` value minus the detected `currentTime`.
4. **Fallback Mechanism:** Ensure the manual subtraction logic in the `GameRules` fallback is working if the entity is not found.

## Implementation Steps
1. Modify `Features/BombTimer.cs` to include debug `Console.WriteLine` statements.
2. Build and run the project to observe the output.
3. If `currentTime` is static, try offset `0x28`.
4. If `currentTime` moves but `TimeLeft` stays static, investigate the `m_flC4Blow` reading.
5. Once fixed, remove debug logs and finalize the code.

## Verification
- Plant the bomb in CS2.
- Check if the time in the UI decreases.
- Check if the progress bar moves towards empty.
- Verify the beep sound triggers as the timer gets low.
