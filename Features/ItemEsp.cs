using System.Numerics;
using CS2Cheat.Data.Game;
using CS2Cheat.Graphics;
using CS2Cheat.Utils;
using ImGuiNET;

namespace CS2Cheat.Features;

public static class ItemEsp
{
    private static ConfigManager Config => ConfigManager.Load();

    public static void Draw(ImDrawListPtr drawList, GameData gameData, GameProcess gameProcess)
    {
        if (!Config.ItemEsp || gameData.Player == null) return;
        if (gameProcess.ModuleClient == null || gameProcess.Process == null) return;

        var entityListPtr = gameProcess.ModuleClient.Read<IntPtr>(Offsets.dwEntityList);
        if (entityListPtr == IntPtr.Zero) return;

        for (var i = 0; i < 512; i++)
        {
            var listEntry = gameProcess.Process.Read<IntPtr>(entityListPtr + 8 * (i >> 9) + 16);
            if (listEntry == IntPtr.Zero) continue;

            var entityAddr = gameProcess.Process.Read<IntPtr>(listEntry + 112 * (i & 0x1FF));
            if (entityAddr == IntPtr.Zero) continue;

            var identity = gameProcess.Process.Read<IntPtr>(entityAddr + 0x10);
            if (identity == IntPtr.Zero) continue;

            var namePtr = gameProcess.Process.Read<IntPtr>(identity + 0x20);
            if (namePtr == IntPtr.Zero) continue;

            var name = gameProcess.Process.ReadString(namePtr, 64);
            if (string.IsNullOrEmpty(name) || !name.StartsWith("weapon_", StringComparison.OrdinalIgnoreCase))
                continue;

            var gameSceneNode = gameProcess.Process.Read<IntPtr>(entityAddr + Offsets.m_pGameSceneNode);
            if (gameSceneNode == IntPtr.Zero) continue;

            var pos = gameProcess.Process.Read<Vector3>(gameSceneNode + Offsets.m_vecAbsOrigin);

            var screenPos = gameData.Player.MatrixViewProjectionViewport.Transform(pos);
            if (screenPos.Z >= 1) continue;

            var displayName = name.Replace("weapon_", "").ToUpperInvariant();
            var dist = Vector3.Distance(gameData.Player.Origin, pos) * 0.0254f;
            var text = $"{displayName} [{dist:0}m]";
            var textSize = ImGui.CalcTextSize(text);

            var textPos = new Vector2(screenPos.X - textSize.X * 0.5f, screenPos.Y);

            var textColor = Config.EspTextRainbow 
                ? OverlayRenderer.GetRainbowColor() 
                : OverlayRenderer.ToColor(new Vector4(Config.EspTextColor[0], Config.EspTextColor[1], Config.EspTextColor[2], Config.EspTextColor[3]));

            drawList.AddText(textPos + new Vector2(1, 1), OverlayRenderer.Colors.Black, text);
            drawList.AddText(textPos, textColor, text);
        }
    }
}
