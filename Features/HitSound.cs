using System.Runtime.InteropServices;
using System.Text;
using CS2Cheat.Data.Game;
using CS2Cheat.Utils;

namespace CS2Cheat.Features;

public static class HitSound
{
    private static ConfigManager? _config;
    private static ConfigManager Config => _config ??= ConfigManager.Load();

    private static float _prevDamage = -1;
    private const string Alias = "HitSoundAlias";
    private static string _currentSoundName = "";
    private static string SoundDirectory => ResourceHelper.SoundDir;

    [DllImport("winmm.dll")]
    private static extern long mciSendString(string strCommand, StringBuilder? strReturn, int iReturnLength, IntPtr hwndCallback);

    static HitSound()
    {
        EnsureSoundLoaded();
    }

    private static void EnsureSoundLoaded()
    {
        var config = Config;
        if (config.HitSoundName == _currentSoundName && _currentSoundName != "")
            return;

        mciSendString($"close {Alias}", null, 0, IntPtr.Zero);

        var soundPath = Path.Combine(SoundDirectory, config.HitSoundName);
        if (!File.Exists(soundPath))
        {
            _currentSoundName = config.HitSoundName;
            return;
        }

        mciSendString($"open \"{soundPath}\" type waveaudio alias {Alias}", null, 0, IntPtr.Zero);
        _currentSoundName = config.HitSoundName;
    }

    public static string[] GetAvailableSounds()
    {
        if (!Directory.Exists(SoundDirectory))
            return Array.Empty<string>();

        return Directory.GetFiles(SoundDirectory, "*.wav")
            .Select(Path.GetFileName)
            .Where(f => f != null)
            .Cast<string>()
            .OrderBy(f => f)
            .ToArray();
    }

    public static void Update(GameProcess gameProcess, GameData gameData)
    {
        if (!Config.HitSound || gameData.Player?.IsAlive() != true)
        {
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
                        Play();
                    }
                    _prevDamage = damageDealt;
                }
            }
        }
        catch
        {
            // Memory read failed
        }
    }

    public static void Play()
    {
        EnsureSoundLoaded();
        if (_currentSoundName == "") return;

        int volume = (int)(Config.HitSoundVolume * 1000);
        mciSendString($"setaudio {Alias} volume to {volume}", null, 0, IntPtr.Zero);
        mciSendString($"play {Alias} from 0", null, 0, IntPtr.Zero);
    }
}
