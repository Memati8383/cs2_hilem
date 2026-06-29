using System;
using System.Numerics;
using CS2Cheat.Graphics;
using ImGuiNET;

using CS2Cheat.Utils;

namespace CS2Cheat.Features;

public static class Watermark
{
    public static void Draw(ImDrawListPtr drawList)
    {
        var config = ConfigManager.Load();
        var io = ImGui.GetIO();
        float fps = io.Framerate;
        string time = DateTime.Now.ToString("HH:mm:ss");
        string text = $"MematiHack | {Language.Get("watermark_fps")}: {fps:0} | {time}";

        var textSize = ImGui.CalcTextSize(text);
        var padding = new Vector2(10, 5);
        
        // Top right position
        var boxPos = new Vector2(io.DisplaySize.X - textSize.X - padding.X * 2 - 20, 20);
        var boxSize = textSize + padding * 2;

        var textColor = config.WatermarkTextRainbow 
            ? OverlayRenderer.GetRainbowColor() 
            : OverlayRenderer.ToColor(new Vector4(config.WatermarkTextColor[0], config.WatermarkTextColor[1], config.WatermarkTextColor[2], config.WatermarkTextColor[3]));

        // Draw background
        drawList.AddRectFilled(boxPos, boxPos + boxSize, OverlayRenderer.ToColor(15, 15, 15, 230), 4f);
        
        // Draw top line
        drawList.AddLine(boxPos, new Vector2(boxPos.X + boxSize.X, boxPos.Y), textColor, 2f);

        // Draw text
        drawList.AddText(boxPos + padding, textColor, text);
    }
}
