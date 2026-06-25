using System;
using System.Collections.Generic;
using System.Numerics;
using CS2Cheat.Data.Game;
using CS2Cheat.Graphics;
using CS2Cheat.Utils;
using ImGuiNET;

namespace CS2Cheat.Features;

public static class Hitmarker
{
    private static ConfigManager? _config;
    private static ConfigManager Config => _config ??= ConfigManager.Load();

    private static float _prevDamage = -1;
    private static readonly List<HitInfo> Hits = new();
    private static readonly object Lock = new();

    private class HitInfo
    {
        public DateTime Time { get; }
        public float MaxDuration { get; }

        public HitInfo(float duration)
        {
            Time = DateTime.Now;
            MaxDuration = duration;
        }
    }

    public static void Update(GameProcess gameProcess, GameData gameData)
    {
        if (!Config.HitMarker || gameData.Player?.IsAlive() != true)
        {
            lock (Lock) Hits.Clear();
            _prevDamage = -1;
            return;
        }

        try
        {
            var controllerBase = gameProcess.ModuleClient?.Read<IntPtr>(Offsets.dwLocalPlayerController);
            if (controllerBase.HasValue && controllerBase.Value != IntPtr.Zero)
            {
                var actionTrackingServices = gameProcess.Process?.Read<IntPtr>(controllerBase.Value + Offsets.m_pActionTrackingServices);
                if (actionTrackingServices.HasValue && actionTrackingServices.Value != IntPtr.Zero)
                {
                    var damageDealt = gameProcess.Process!.Read<float>(actionTrackingServices.Value + Offsets.m_unTotalRoundDamageDealt);
                    if (_prevDamage != -1f && damageDealt > _prevDamage)
                    {
                        lock (Lock)
                        {
                            Hits.Add(new HitInfo(Config.HitMarkerDuration));
                        }
                    }
                    _prevDamage = damageDealt;
                }
            }
        }
        catch
        {
            // Ignore memory read errors
        }

        // Cleanup expired hits
        lock (Lock)
        {
            Hits.RemoveAll(h => (DateTime.Now - h.Time).TotalMilliseconds > h.MaxDuration);
        }
    }

    public static void Draw(ImDrawListPtr drawList)
    {
        if (!Config.HitMarker) return;

        List<HitInfo> currentHits;
        lock (Lock)
        {
            if (Hits.Count == 0) return;
            currentHits = new List<HitInfo>(Hits);
        }

        var io = ImGui.GetIO();
        var screenCenter = io.DisplaySize / 2f;
        
        foreach (var hit in currentHits)
        {
            var timePassed = (float)(DateTime.Now - hit.Time).TotalMilliseconds;
            var alpha = Math.Clamp(1f - (timePassed / hit.MaxDuration), 0f, 1f);
            
            if (alpha <= 0) continue;

            var size = Config.HitMarkerSize;
            var gap = Config.HitMarkerGap;
            var thickness = Config.HitMarkerThickness;
            var colorArr = Config.HitMarkerColor;
            var color = OverlayRenderer.ToColor(new Vector4(colorArr[0], colorArr[1], colorArr[2], colorArr[3] * alpha));

            // Top-Left to Bottom-Right
            drawList.AddLine(
                new Vector2(screenCenter.X - size, screenCenter.Y - size),
                new Vector2(screenCenter.X - gap, screenCenter.Y - gap),
                color, thickness);
            drawList.AddLine(
                new Vector2(screenCenter.X + gap, screenCenter.Y + gap),
                new Vector2(screenCenter.X + size, screenCenter.Y + size),
                color, thickness);
            
            // Top-Right to Bottom-Left
            drawList.AddLine(
                new Vector2(screenCenter.X + size, screenCenter.Y - size),
                new Vector2(screenCenter.X + gap, screenCenter.Y - gap),
                color, thickness);
            drawList.AddLine(
                new Vector2(screenCenter.X - gap, screenCenter.Y + gap),
                new Vector2(screenCenter.X - size, screenCenter.Y + size),
                color, thickness);
        }
    }
}
