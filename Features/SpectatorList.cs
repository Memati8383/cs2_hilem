using System.Numerics;
using CS2Cheat.Data.Entity;
using CS2Cheat.Data.Game;
using CS2Cheat.Graphics;
using CS2Cheat.Utils;
using ImGuiNET;

namespace CS2Cheat.Features;

public static class SpectatorList
{
    private static ConfigManager Config => ConfigManager.Load();

    public static void Draw(ImDrawListPtr drawList, GameData gameData, GameProcess gameProcess)
    {
        if (!Config.SpectatorList || gameData.Player == null || gameData.Entities == null) return;
        if (gameProcess.Process == null) return;

        var localPlayerPawnHandle = gameData.Player.ControllerAddress != IntPtr.Zero
            ? gameProcess.Process.Read<int>(gameData.Player.ControllerAddress + Offsets.m_hPawn)
            : 0;

        if (localPlayerPawnHandle <= 0) return;

        var spectators = new List<string>();

        foreach (var entity in gameData.Entities)
        {
            if (entity.ControllerAddress == IntPtr.Zero) continue;
            if (entity.IsAlive()) continue;

            var targetHandle = entity.ObserverTarget;
            if (targetHandle <= 0) continue;

            if (targetHandle == localPlayerPawnHandle)
            {
                var name = entity.Name ?? "Unknown";
                if (!string.IsNullOrEmpty(name))
                    spectators.Add(name);
            }
        }

        if (spectators.Count == 0) return;

        var io = ImGui.GetIO();
        var text = $"Spectators ({spectators.Count}):\n" + string.Join("\n", spectators);
        var pos = new Vector2(io.DisplaySize.X - 250, 100);
        var textSize = ImGui.CalcTextSize(text);

        var bgColor = OverlayRenderer.ToColor(20, 20, 20, 160);
        var textColor = Config.WatermarkTextRainbow 
            ? OverlayRenderer.GetRainbowColor() 
            : OverlayRenderer.ToColor(new Vector4(Config.WatermarkTextColor[0], Config.WatermarkTextColor[1], Config.WatermarkTextColor[2], Config.WatermarkTextColor[3]));

        drawList.AddRectFilled(pos - new Vector2(10, 10), pos + textSize + new Vector2(10, 10), bgColor, 8f);
        drawList.AddRect(pos - new Vector2(10, 10), pos + textSize + new Vector2(10, 10), textColor, 8f);
        drawList.AddText(pos, textColor, text);
    }
}