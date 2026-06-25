using System;
using System.Numerics;
using CS2Cheat.Data.Game;
using CS2Cheat.Graphics;
using ImGuiNET;
using CS2Cheat.Utils;
using CS2Cheat.Core.Data;
using CS2Cheat.Data.Entity;

namespace CS2Cheat.Features;

public static class Radar
{
    public static void Draw(GameData gameData)
    {
        var config = ConfigManager.Load();
        if (!config.Radar || gameData.Player == null) return;

        ImGui.SetNextWindowSize(new Vector2(250, 250), ImGuiCond.FirstUseEver);
        if (ImGui.Begin("Radar", ImGuiWindowFlags.NoCollapse))
        {
            var drawList = ImGui.GetWindowDrawList();
            var pos = ImGui.GetWindowPos();
            var size = ImGui.GetWindowSize();

            // Center of the radar
            var center = pos + size * 0.5f;
            drawList.AddLine(new Vector2(center.X, pos.Y), new Vector2(center.X, pos.Y + size.Y), OverlayRenderer.Colors.DarkGray, 1.0f);
            drawList.AddLine(new Vector2(pos.X, center.Y), new Vector2(pos.X + size.X, center.Y), OverlayRenderer.Colors.DarkGray, 1.0f);

            var localPlayer = gameData.Player;
            if (localPlayer.IsAlive())
            {
                drawList.AddCircleFilled(center, 4.0f, OverlayRenderer.Colors.Green);

                if (gameData.Entities != null) foreach (var entity in gameData.Entities)
                {
                    if (!entity.IsAlive() || entity.AddressBase == localPlayer.AddressBase) continue;
                    if (config.TeamCheck && entity.Team == localPlayer.Team) continue;

                    var distance = Vector3.Distance(localPlayer.Origin, entity.Origin);
                    var scale = config.RadarRange;

                    var diff = entity.Origin - localPlayer.Origin;
                    var angleY = localPlayer.ViewAngles.Y * Math.PI / 180.0;

                    var x = (float)(diff.Y * Math.Cos(angleY) - diff.X * Math.Sin(angleY)) * scale;
                    var y = (float)(diff.X * Math.Cos(angleY) + diff.Y * Math.Sin(angleY)) * scale;

                    var radarPos = center + new Vector2(x, -y);

                    // Clamp to radar window
                    if (radarPos.X < pos.X) radarPos.X = pos.X;
                    if (radarPos.X > pos.X + size.X) radarPos.X = pos.X + size.X;
                    if (radarPos.Y < pos.Y) radarPos.Y = pos.Y;
                    if (radarPos.Y > pos.Y + size.Y) radarPos.Y = pos.Y + size.Y;

                    uint color = entity.Team == Team.Terrorists ? OverlayRenderer.Colors.Red : OverlayRenderer.Colors.Blue;
                    drawList.AddCircleFilled(radarPos, 3.5f, color);
                }

            // Draw bomb marker on radar
            if (BombTimer.IsBombPlanted && BombTimer.BombPosition != Vector3.Zero)
            {
                var diff = BombTimer.BombPosition - localPlayer.Origin;
                var angleY = localPlayer.ViewAngles.Y * Math.PI / 180.0;

                var bx = (float)(diff.Y * Math.Cos(angleY) - diff.X * Math.Sin(angleY)) * config.RadarRange;
                var by = (float)(diff.X * Math.Cos(angleY) + diff.Y * Math.Sin(angleY)) * config.RadarRange;

                var bombRadarPos = center + new Vector2(bx, -by);

                if (bombRadarPos.X >= pos.X && bombRadarPos.X <= pos.X + size.X &&
                    bombRadarPos.Y >= pos.Y && bombRadarPos.Y <= pos.Y + size.Y)
                {
                    drawList.AddTriangleFilled(
                        bombRadarPos + new Vector2(0, -5),
                        bombRadarPos + new Vector2(-4, 4),
                        bombRadarPos + new Vector2(4, 4),
                        OverlayRenderer.Colors.OrangeRed
                    );
                }
            }
            }

            ImGui.End();
        }
    }
}
