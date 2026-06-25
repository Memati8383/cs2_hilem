using System.Numerics;
using CS2Cheat.Data.Game;
using CS2Cheat.Graphics;
using CS2Cheat.Utils;
using ImGuiNET;

namespace CS2Cheat.Features;

public static class VelocityGraph
{
    private static ConfigManager? _config;
    private static ConfigManager Config => _config ??= ConfigManager.Load();
    private static readonly float[] Velocities = new float[120];
    private static int _index;
    private static float _maxVel;

    public static void Draw(ImDrawListPtr drawList, GameData gameData)
    {
        if (!Config.VelocityGraph || gameData.Player == null) return;

        var vel = gameData.Player.Velocity.Length();
        Velocities[_index % Velocities.Length] = vel;
        _index++;

        if (vel > _maxVel) _maxVel = vel;
        _maxVel = Math.Max(_maxVel, 1f);

        var io = ImGui.GetIO();
        var graphPos = new Vector2(io.DisplaySize.X - 260, io.DisplaySize.Y - 120);
        var graphSize = new Vector2(250, 100);

        drawList.AddRectFilled(graphPos, graphPos + graphSize, OverlayRenderer.ToColor(0, 0, 0, 180), 4f);
        drawList.AddRect(graphPos, graphPos + graphSize, OverlayRenderer.Colors.DarkGray, 4f);

        var count = Math.Min(_index, Velocities.Length);
        var startIdx = _index >= Velocities.Length ? _index % Velocities.Length : 0;

        for (int i = 1; i < count; i++)
        {
            var idx0 = (startIdx + i - 1) % Velocities.Length;
            var idx1 = (startIdx + i) % Velocities.Length;

            var x0 = graphPos.X + (i - 1) * graphSize.X / (count - 1);
            var x1 = graphPos.X + i * graphSize.X / (count - 1);
            var y0 = graphPos.Y + graphSize.Y - (Velocities[idx0] / _maxVel) * graphSize.Y;
            var y1 = graphPos.Y + graphSize.Y - (Velocities[idx1] / _maxVel) * graphSize.Y;

            drawList.AddLine(new Vector2(x0, y0), new Vector2(x1, y1), OverlayRenderer.ToColor(0, 200, 255), 1.5f);
        }

        var infoText = $"Vel: {vel:0.0} u/s  Max: {_maxVel:0.0}";
        drawList.AddText(new Vector2(graphPos.X + 5, graphPos.Y + graphSize.Y + 2), OverlayRenderer.Colors.White, infoText);
    }
}