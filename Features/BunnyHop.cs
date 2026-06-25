using System;
using System.Threading;
using CS2Cheat.Core;
using CS2Cheat.Data.Game;
using CS2Cheat.Utils;
using Keys = Process.NET.Native.Types.Keys;

namespace CS2Cheat.Features;

public class BunnyHop : ThreadedServiceBase
{
    protected override string ThreadName => nameof(BunnyHop);

    private GameProcess? GameProcess { get; set; }
    private GameData? GameData { get; set; }

    public BunnyHop(GameProcess gameProcess, GameData gameData)
    {
        GameProcess = gameProcess;
        GameData = gameData;
    }

    public override void Dispose()
    {
        base.Dispose();
        GameData = null;
        GameProcess = null;
    }

    protected override void FrameAction()
    {
        try
        {
            if (GameProcess == null || !GameProcess.IsValid || GameData?.Player == null || !GameData.Player.IsAlive())
                return;

            var config = ConfigManager.Load();
            if (!config.BunnyHop)
                return;

            if (config.BunnyHopKey.IsKeyDown())
            {
                if (GameProcess.Process == null || GameProcess.ModuleClient == null) return;
                var flags = GameProcess.Process.Read<uint>(GameData.Player.AddressBase + Offsets.m_fFlags);
                bool onGround = (flags & (1 << 0)) != 0;

                if (onGround)
                {
                    GameProcess.ModuleClient.Write(Offsets.dwForceJump, 65537);
                }
                else
                {
                    GameProcess.ModuleClient.Write(Offsets.dwForceJump, 256);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[BunnyHop ERROR] {ex.Message}");
        }
    }
}
