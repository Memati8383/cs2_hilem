using CS2Cheat.Data.Game;
using CS2Cheat.Utils;

namespace CS2Cheat.Features;

public static class EspGlow
{
    private static ConfigManager Config => ConfigManager.Load();

    public static void Run(GameData gameData, GameProcess gameProcess)
    {
        var config = Config;
        if (!config.EspGlow) return;

        var player = gameData.Player;
        if (player == null || !player.IsAlive() || gameData.Entities == null) return;

        var localTeam = player.Team;
        var entityList = gameProcess.ModuleClient?.Read<IntPtr>(Offsets.dwEntityList);
        if (entityList == null || entityList == IntPtr.Zero) return;

        for (int i = 1; i < 64; i++)
        {
            var listEntry = gameProcess.Process!.Read<IntPtr>(entityList.Value + ((8 * (i & 0x7FFF) >> 9) + 16));
            if (listEntry == IntPtr.Zero) continue;

            var entityController = gameProcess.Process!.Read<IntPtr>(listEntry + 112 * (i & 0x1FF));
            if (entityController == IntPtr.Zero) continue;

            var pawnHandle = gameProcess.Process!.Read<int>(entityController + Offsets.m_hPawn);
            if (pawnHandle == 0 || pawnHandle == -1) continue;

            var pawnEntry = gameProcess.Process!.Read<IntPtr>(entityList.Value + (8 * ((pawnHandle & 0x7FFF) >> 9) + 16));
            if (pawnEntry == IntPtr.Zero) continue;

            var entityPawn = gameProcess.Process!.Read<IntPtr>(pawnEntry + 112 * (pawnHandle & 0x1FF));
            if (entityPawn == IntPtr.Zero || entityPawn == player.AddressBase) continue;

            var lifeState = gameProcess.Process!.Read<int>(entityPawn + Offsets.m_lifeState);
            if (lifeState != 256) continue;

            var team = gameProcess.Process!.Read<int>(entityPawn + Offsets.m_iTeamNum);
            if (config.TeamCheck && team == (int)localTeam) continue;

            var health = gameProcess.Process!.Read<int>(entityPawn + Offsets.m_iHealth);
            var glow = entityPawn + Offsets.m_Glow;

            int colorArgb;
            if (config.GlowHealthBased)
            {
                float hpPercent = Math.Clamp(health / 100f, 0f, 1f);
                float r = hpPercent < 0.5f ? 1f : 2f * (1f - hpPercent);
                float g = hpPercent > 0.5f ? 1f : 2f * hpPercent;
                colorArgb = ColorToArgb(r, g, 0f, 0.6f);
            }
            else
            {
                var col = team == (int)localTeam ? config.GlowColorTeam : config.GlowColorEnemy;
                colorArgb = ColorToArgb(col[0], col[1], col[2], col[3]);
            }

            gameProcess.Process!.Write<int>(glow + Offsets.m_glowColorOverride, colorArgb);
            gameProcess.Process!.Write<bool>(glow + Offsets.m_bGlowing, true);
            gameProcess.Process!.Write<int>(glow + Offsets.m_iGlowType, config.GlowStyle);
        }
    }

    private static int ColorToArgb(float r, float g, float b, float a)
    {
        return (byte)(a * 255) << 24 | (byte)(r * 255) << 16 | (byte)(g * 255) << 8 | (byte)(b * 255);
    }
}