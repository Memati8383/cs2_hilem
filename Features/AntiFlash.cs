using CS2Cheat.Data.Game;
using CS2Cheat.Utils;

namespace CS2Cheat.Features;

public static class AntiFlash
{
    public static void Run(GameData gameData, GameProcess gameProcess)
    {
        var config = ConfigManager.Load();
        if (!config.AntiFlash) return;

        var player = gameData.Player;
        if (player == null || !player.IsAlive()) return;

        var flashDuration = gameProcess.Process!.Read<float>(player.AddressBase + Offsets.m_flFlashDuration);
        if (flashDuration > 0)
        {
            gameProcess.Process!.Write(player.AddressBase + Offsets.m_flFlashDuration, 0.0f);
            gameProcess.Process!.Write(player.AddressBase + Offsets.m_flFlashMaxAlpha, 0.0f);
        }
    }
}
