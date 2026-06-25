using System;
using System.Collections.Generic;
using System.Numerics;
using CS2Cheat.Data.Game;
using CS2Cheat.Graphics;
using CS2Cheat.Utils;
using ImGuiNET;

namespace CS2Cheat.Features;

public static class DamageText
{
    private static ConfigManager? _config;
    private static ConfigManager Config => _config ??= ConfigManager.Load();

    private static readonly List<DamageIndicator> Indicators = new();
    private static readonly object Lock = new();

    private class DamageIndicator
    {
        public Vector3 WorldPos { get; }
        public float Damage { get; }
        public DateTime StartTime { get; }
        public float MaxDuration { get; }
        public Vector2 Velocity { get; }
        public bool IsKill { get; }

        public DamageIndicator(Vector3 pos, float damage, float duration, bool isKill)
        {
            WorldPos = pos;
            Damage = damage;
            StartTime = DateTime.Now;
            MaxDuration = duration;
            IsKill = isKill;
            
            var rand = new Random();
            Velocity = new Vector2((float)(rand.NextDouble() * 80 - 40), (float)(rand.NextDouble() * -60 - 40));
        }
    }

    public static void Update(GameProcess gameProcess, GameData gameData)
    {
        if (!Config.DamageText)
        {
            lock (Lock) Indicators.Clear();
            return;
        }

        var newEntries = TacticalManager.TakeRecentDamage();
        foreach (var entry in newEntries)
        {
            lock (Lock)
            {
                Indicators.Add(new DamageIndicator(entry.Position, entry.Amount, Config.DamageTextDuration, entry.IsKill));
            }
        }

        lock (Lock)
        {
            Indicators.RemoveAll(i => (DateTime.Now - i.StartTime).TotalMilliseconds > i.MaxDuration);
        }
    }

    public static void Draw(ImDrawListPtr drawList, GameData gameData)
    {
        if (!Config.DamageText || gameData.Player == null) return;

        List<DamageIndicator> current;
        lock (Lock) current = new List<DamageIndicator>(Indicators);

        foreach (var indicator in current)
        {
            var timePassed = (float)(DateTime.Now - indicator.StartTime).TotalMilliseconds;
            var progress = timePassed / indicator.MaxDuration;
            var alpha = Math.Clamp(1f - (float)Math.Pow(progress, 1.5), 0f, 1f);
            
            Vector3 screenPos = gameData.Player.MatrixViewProjectionViewport.Transform(indicator.WorldPos);
            if (screenPos.Z < 1)
            {
                float xOff = indicator.Velocity.X * progress;
                float yOff = indicator.Velocity.Y * progress + (0.5f * 120f * (float)Math.Pow(progress, 2));

                string text = indicator.IsKill ? "KILL" : $"-{indicator.Damage:0}";
                var colorArr = indicator.IsKill ? new float[] { 1f, 0.8f, 0f, 1f } : Config.DamageTextColor;
                uint color = OverlayRenderer.ToColor(new Vector4(colorArr[0], colorArr[1], colorArr[2], colorArr[3] * alpha));
                
                Vector2 pos2D = new Vector2(screenPos.X + xOff, screenPos.Y + yOff);
                float fontSize = Config.DamageTextSize * Math.Clamp(800f / Vector3.Distance(gameData.Player.Origin, indicator.WorldPos), 0.5f, 1.5f);

                drawList.AddText(null, fontSize, pos2D + new Vector2(1, 1), OverlayRenderer.ToColor(0, 0, 0, (byte)(200 * alpha)), text);
                drawList.AddText(null, fontSize, pos2D, color, text);
            }
        }
    }
}
