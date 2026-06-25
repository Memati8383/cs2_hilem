using System.Numerics;
using CS2Cheat.Data.Entity;
using CS2Cheat.Data.Game;
using CS2Cheat.Graphics;
using CS2Cheat.Utils;
using ImGuiNET;

namespace CS2Cheat.Features;

public static class OffscreenEnemy
{
    private const float ArrowSize = 10f;
    private const float EdgeMargin = 20f;

    public static void Draw(ImDrawListPtr drawList, GameData gameData)
    {
        var config = ConfigManager.Load();
        if (!config.OffscreenEnemy || gameData.Player == null) return;

        var io = ImGui.GetIO();
        var screenW = io.DisplaySize.X;
        var screenH = io.DisplaySize.Y;
        var center = new Vector2(screenW / 2, screenH / 2);

        if (gameData.Entities == null || gameData.Player == null) return;
        foreach (var entity in gameData.Entities)
        {
            if (!entity.IsAlive() || entity.AddressBase == gameData.Player.AddressBase) continue;
            if (config.TeamCheck && entity.Team == gameData.Player.Team) continue;

            var screenPos = gameData.Player.MatrixViewProjectionViewport.Transform(entity.Origin);
            if (screenPos.Z < 1 && screenPos.X >= 0 && screenPos.X <= screenW && screenPos.Y >= 0 && screenPos.Y <= screenH)
                continue;

            var dir = Vector2.Normalize(new Vector2(screenPos.X - center.X, screenPos.Y - center.Y));
            if (dir == Vector2.Zero) dir = new Vector2(0, -1);

            var edgePos = GetEdgePosition(center, dir, screenW, screenH);

            var color = entity.Team == Core.Data.Team.Terrorists
                ? OverlayRenderer.ToColor(255, 50, 50, 200)
                : OverlayRenderer.ToColor(50, 100, 255, 200);

            DrawArrow(drawList, edgePos, dir, color);

            var dist = Vector3.Distance(gameData.Player.Origin, entity.Origin) * 0.0254f;
            var distText = $"{dist:0}m";
            var distTextSize = ImGui.CalcTextSize(distText);
            var distPos = edgePos - new Vector2(distTextSize.X * 0.5f, 18f);
            drawList.AddText(distPos + new Vector2(1, 1), OverlayRenderer.Colors.Black, distText);
            drawList.AddText(distPos, OverlayRenderer.ToColor(255, 255, 255, 200), distText);
        }
    }

    private static Vector2 GetEdgePosition(Vector2 center, Vector2 dir, float screenW, float screenH)
    {
        var halfW = screenW / 2 - EdgeMargin;
        var halfH = screenH / 2 - EdgeMargin;

        if (dir.X == 0 && dir.Y == 0) return center;

        var tX = halfW / Math.Abs(dir.X);
        var tY = halfH / Math.Abs(dir.Y);
        var t = Math.Min(tX, tY);

        var pos = center + dir * t;

        pos.X = Math.Clamp(pos.X, EdgeMargin, screenW - EdgeMargin);
        pos.Y = Math.Clamp(pos.Y, EdgeMargin, screenH - EdgeMargin);

        return pos;
    }

    private static void DrawArrow(ImDrawListPtr drawList, Vector2 pos, Vector2 dir, uint color)
    {
        var angle = (float)Math.Atan2(dir.Y, dir.X);
        var a = ArrowSize;

        var p1 = pos + new Vector2((float)Math.Cos(angle) * a, (float)Math.Sin(angle) * a);

        var leftAngle = angle + 2.3f;
        var rightAngle = angle - 2.3f;
        var p2 = pos + new Vector2((float)Math.Cos(leftAngle) * a * 0.7f, (float)Math.Sin(leftAngle) * a * 0.7f);
        var p3 = pos + new Vector2((float)Math.Cos(rightAngle) * a * 0.7f, (float)Math.Sin(rightAngle) * a * 0.7f);

        drawList.AddTriangleFilled(p1, p2, p3, color);
        drawList.AddTriangle(p1, p2, p3, OverlayRenderer.Colors.Black, 1f);
    }
}