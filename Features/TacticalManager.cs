using System;
using System.Collections.Generic;
using System.Numerics;
using CS2Cheat.Data.Game;
using CS2Cheat.Data.Entity;
using CS2Cheat.Utils;

namespace CS2Cheat.Features;

public static class TacticalManager
{
    public static float GetDistanceInMeters(Vector3 p1, Vector3 p2) => Vector3.Distance(p1, p2) * 0.0254f;
    
    public static string FormatDistance(float distanceInMeters) => $"{(int)distanceInMeters}m";

    // Damage Tracking
    private static float _prevTotalDamage = -1;
    private static readonly int[] _prevHealths = new int[64];
    private static readonly List<DamageEntry> _recentDamage = new();
    private static readonly object _damageLock = new();

    public class DamageEntry
    {
        public int TargetId { get; set; }
        public float Amount { get; set; }
        public Vector3 Position { get; set; }
        public DateTime Time { get; set; }
        public bool IsKill { get; set; }
    }

    public static List<DamageEntry> TakeRecentDamage()
    {
        lock (_damageLock)
        {
            var result = new List<DamageEntry>(_recentDamage);
            _recentDamage.Clear();
            return result;
        }
    }

    // Bomb Tracking
    public static float BombTimeLeft { get; private set; }
    public static float DefuseTimeLeft { get; private set; }
    public static float BombTimerLength { get; private set; } = 40f;
    public static float DefuseTimerLength { get; private set; } = 10f;
    public static bool IsBombPlanted { get; private set; }
    public static bool IsBeingDefused { get; private set; }
    public static bool HasDefuserKit { get; private set; }
    public static Vector3 BombPosition { get; private set; }
    public static float BombDamageToPlayer { get; private set; }

    public static void Update(GameProcess gameProcess, GameData gameData)
    {
        UpdateDamage(gameProcess, gameData);
        UpdateBomb(gameProcess, gameData);
    }

    private static void UpdateDamage(GameProcess gameProcess, GameData gameData)
    {
        if (gameData.Player?.IsAlive() != true)
        {
            lock (_damageLock) _recentDamage.Clear();
            _prevTotalDamage = -1;
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
                    var totalDamage = gameProcess.Process!.Read<float>(actionTrackingServices.Value + Offsets.m_unTotalRoundDamageDealt);
                    
                    if (_prevTotalDamage != -1f && totalDamage > _prevTotalDamage)
                    {
                        if (gameData.Entities != null)
                        {
                            foreach (var entity in gameData.Entities)
                            {
                                if (entity.Id < 0 || entity.Id >= 64 || entity.AddressBase == gameData.Player.AddressBase) continue;

                                int hpDiff = _prevHealths[entity.Id] - entity.Health;
                                if (hpDiff > 0 && _prevHealths[entity.Id] > 0)
                                {
                    lock (_damageLock)
                    {
                        _recentDamage.Add(new DamageEntry
                        {
                            TargetId = entity.Id,
                            Amount = hpDiff,
                            Position = entity.BonePos.TryGetValue("head", out var head) ? head : entity.Origin,
                            Time = DateTime.Now,
                            IsKill = entity.Health <= 0
                        });
                    }
                                }
                                _prevHealths[entity.Id] = entity.Health;
                            }
                        }
                    }
                    else if (gameData.Entities != null)
                    {
                        foreach (var entity in gameData.Entities)
                        {
                            if (entity.Id >= 0 && entity.Id < 64) _prevHealths[entity.Id] = entity.Health;
                        }
                    }
                    _prevTotalDamage = totalDamage;
                }
            }
        }
        catch { }

        lock (_damageLock)
        {
            _recentDamage.RemoveAll(d => (DateTime.Now - d.Time).TotalSeconds > 3);
        }
    }

    private static void UpdateBomb(GameProcess gameProcess, GameData gameData)
    {
        IsBombPlanted = BombTimer.IsBombPlanted;
        BombPosition = BombTimer.BombPosition;
        BombTimeLeft = BombTimer.TimeLeft;
        DefuseTimeLeft = BombTimer.DefuseLeft;
        BombTimerLength = BombTimer.TimerLength;
        DefuseTimerLength = BombTimer.DefuseLength;
        IsBeingDefused = BombTimer.IsBeingDefused;
        HasDefuserKit = BombTimer.HasDefuser;

        if (IsBombPlanted && gameData.Player != null)
        {
            float distance = Vector3.Distance(gameData.Player.Origin, BombPosition);
            BombDamageToPlayer = CalculateBombDamage(distance, gameData.Player.Armor);
        }
        else
        {
            BombDamageToPlayer = 0;
        }
    }

    private static float CalculateBombDamage(float distance, int armor)
    {
        float maxDamage = 500f;
        float radius = maxDamage * 3.5f;
        float sigma = radius / 3.0f;
        float damage = maxDamage * (float)Math.Exp(-(distance * distance) / (2 * sigma * sigma));
        if (armor > 0) damage *= 0.5f;
        return damage;
    }
}
