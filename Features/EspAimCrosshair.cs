using System.Numerics;
using CS2Cheat.Data.Entity;
using CS2Cheat.Data.Game;
using CS2Cheat.Graphics;
using CS2Cheat.Utils;
using ImGuiNET;

namespace CS2Cheat.Features;

public static class EspAimCrosshair
{
    private static Vector3 _pointClip = Vector3.Zero;

    private static Vector2 GetPositionScreen(GameProcess gameProcess, GameData gameData)
    {
        var screenSize = gameProcess.WindowRectangleClient.Size;
        var aspectRatio = (double)screenSize.Width / screenSize.Height;
        var player = gameData.Player;
        if (player == null) return Vector2.Zero;

        var fovY = ((double)Player.Fov).DegreeToRadian();
        var fovX = fovY * aspectRatio;
        var doPunch = player.ShotsFired > 0;
        var punchX = doPunch ? ((double)player.AimPunchAngle.X * Offsets.WeaponRecoilScale).DegreeToRadian() : 0;
        var punchY = doPunch ? ((double)player.AimPunchAngle.Y * Offsets.WeaponRecoilScale).DegreeToRadian() : 0;
        _pointClip = new Vector3
        (
            (float)(-punchY / fovX),
            (float)(-punchX / fovY),
            0
        );
        var pointScreen = player.MatrixViewport.Transform(_pointClip);
        return new Vector2(pointScreen.X, pointScreen.Y);
    }

    public static void Draw(ImDrawListPtr drawList, GameData gameData, GameProcess gameProcess)
    {
        if (gameData.Player == null) return;
        var recoilPos = GetPositionScreen(gameProcess, gameData);
        var io = ImGui.GetIO();
        var screenCenter = io.DisplaySize / 2f;

        // Static Crosshair
        DrawCrosshair(drawList, screenCenter, 5, OverlayRenderer.ToColor(255, 255, 255, 150));

        // Recoil Dot (where bullets go)
        if (gameData.Player.ShotsFired > 0)
        {
            drawList.AddCircleFilled(recoilPos, 2f, OverlayRenderer.ToColor(255, 50, 50, 255));
            drawList.AddCircle(recoilPos, 3f, OverlayRenderer.Colors.Black, 12, 1f);
        }
    }

    private static void DrawCrosshair(ImDrawListPtr drawList, Vector2 center, int radius, uint color)
    {
        drawList.AddLine(center - new Vector2(radius, 0), center + new Vector2(radius, 0), color, 1.2f);
        drawList.AddLine(center - new Vector2(0, radius), center + new Vector2(0, radius), color, 1.2f);
    }
}