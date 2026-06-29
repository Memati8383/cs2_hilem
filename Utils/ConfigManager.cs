using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Keys = Process.NET.Native.Types.Keys;

namespace CS2Cheat.Utils;

public class ConfigManager
{
    private const string ConfigFile = "config.json";

    private static ConfigManager? _cachedInstance;
    private static readonly object _cacheLock = new();
    private static FileSystemWatcher? _watcher;

    public bool HeadShootLine { get; set; }
    public bool EspBox { get; set; }
    public bool EspName { get; set; }
    public bool EspWeapon { get; set; }
    public bool EspFlags { get; set; }
    public float[] EspBoxColor { get; set; } = new float[] { 1f, 0f, 0f, 1f };
    public bool SkeletonEsp { get; set; }
    public bool EspAimCrosshair { get; set; }
    public bool EspSnaplines { get; set; }
    public bool EspDistance { get; set; }
    public bool EspHealthBar { get; set; }
    public bool EspHeadDot { get; set; }
    public bool EspBoxCorner { get; set; }
    public bool BombTimer { get; set; }
    public float[] BombTimerColPanel { get; set; } = new float[] { 0.08f, 0.08f, 0.1f, 0.8f };
    public float[] BombTimerColText { get; set; } = new float[] { 1f, 1f, 1f, 1f };
    public float[] BombTimerColMarker { get; set; } = new float[] { 1f, 1f, 0f, 1f };
    public bool BombTimerRainbow { get; set; }
    public bool VoteTeller { get; set; }
    public bool TeamCheck { get; set; }
    public bool Radar { get; set; }
    public bool Watermark { get; set; }
    public bool AntiFlash { get; set; }
    public bool EspGlow { get; set; }
    public float[] GlowColorEnemy { get; set; } = new float[] { 1f, 0f, 0f, 0.5f };
    public float[] GlowColorTeam { get; set; } = new float[] { 0f, 1f, 0f, 0.5f };
    public bool GlowHealthBased { get; set; }
    public int GlowStyle { get; set; } = 3;
    public bool EspSpottedOnly { get; set; }
    public bool EspArmorBar { get; set; }
    public bool EspAmmo { get; set; }
    public bool EspMoney { get; set; }
    public bool EspPing { get; set; }
    public bool EspReloading { get; set; }
    public bool EspDefusing { get; set; }
    public bool EspWeaponIcon { get; set; }
    public bool SpectatorList { get; set; }
    public bool ItemEsp { get; set; }
    public bool VelocityGraph { get; set; }
    public bool FreeCpu { get; set; }
    public bool VSync { get; set; }
    public bool StreamProof { get; set; }
    public float[] EspBoxColorTeam { get; set; } = new float[] { 0f, 1f, 0f, 1f };
    public float[] EspTextColor { get; set; } = new float[] { 1f, 1f, 1f, 1f };
    public bool EspTextRainbow { get; set; }
    public float[] WatermarkTextColor { get; set; } = new float[] { 1f, 1f, 1f, 1f };
    public bool WatermarkTextRainbow { get; set; }
    public float RadarRange { get; set; } = 0.05f;

    public bool AimBot { get; set; }
    public bool AimDynamicFov { get; set; }
    public bool AimFovCircle { get; set; }
    public float AimFov { get; set; }
    public float AimSmoothing { get; set; }
    public int AimBoneIndex { get; set; }
    public bool AimRcs { get; set; }
    public float AimRcsStrength { get; set; }

    public bool HitSound { get; set; }
    public float HitSoundVolume { get; set; } = 0.5f;
    public string HitSoundName { get; set; } = "beep.wav";
    public bool HitMarker { get; set; }
    public float[] HitMarkerColor { get; set; } = new float[] { 1f, 1f, 1f, 1f };
    public float HitMarkerSize { get; set; } = 12f;
    public float HitMarkerGap { get; set; } = 2f;
    public float HitMarkerDuration { get; set; } = 300f;
    public float HitMarkerThickness { get; set; } = 2f;
    public bool DamageText { get; set; }
    public float[] DamageTextColor { get; set; } = new float[] { 1f, 0.2f, 0.2f, 1f };
    public float DamageTextDuration { get; set; } = 1000f;
    public float DamageTextSize { get; set; } = 16f;
    public bool OffscreenEnemy { get; set; }
    public bool EspEyeTraces { get; set; }
    public bool GrenadeHelper { get; set; }
    public int GrenadeHelperWeaponFilter { get; set; } = -1;

    public bool TriggerBot { get; set; }
    public bool BunnyHop { get; set; }

    [JsonConverter(typeof(KeysJsonConverter))]
    public Keys AimBotKey { get; set; }

    [JsonConverter(typeof(KeysJsonConverter))]
    public Keys TriggerBotKey { get; set; }

    [JsonConverter(typeof(KeysJsonConverter))]
    public Keys MenuToggleKey { get; set; }

    [JsonConverter(typeof(KeysJsonConverter))]
    public Keys BunnyHopKey { get; set; }

    [JsonIgnore]
    public static readonly string[] BoneNames = { "head", "neck_0", "spine_1", "pelvis" };

    [JsonIgnore]
    public static string[] BoneDisplayNames => [Language.Get("bone_head"), Language.Get("bone_neck"), Language.Get("bone_chest"), Language.Get("bone_pelvis")];


    public static ConfigManager Load()
    {
        lock (_cacheLock)
        {
            if (_cachedInstance != null) return _cachedInstance;

            _cachedInstance = LoadFromDisk();
            InitializeWatcher();
            return _cachedInstance;
        }
    }

    public static void Reload()
    {
        lock (_cacheLock)
        {
            _cachedInstance = LoadFromDisk();
            Console.WriteLine("[INFO] Config reloaded.");
        }
    }

    public static void UpdateCache(ConfigManager config)
    {
        lock (_cacheLock)
        {
            _cachedInstance = config;
        }
    }

    private static ConfigManager LoadFromDisk()
    {
        try
        {
            if (!File.Exists(ConfigFile))
            {
                var defaultOptions = Default();
                Save(defaultOptions);
                return defaultOptions;
            }

            var json = File.ReadAllText(ConfigFile);
            var options = JsonSerializer.Deserialize<ConfigManager>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            var result = options ?? Default();
            ApplyMissingDefaults(result, json);
            SanitizeKeys(result);
            return result;
        }
        catch (JsonException)
        {
            return Default();
        }
        catch (IOException)
        {
            return _cachedInstance ?? Default();
        }
    }

    private static void ApplyMissingDefaults(ConfigManager config, string json)
    {
        var defaults = Default();
        if (!json.Contains(nameof(EspBox), StringComparison.OrdinalIgnoreCase)) config.EspBox = defaults.EspBox;
        if (!json.Contains(nameof(EspName), StringComparison.OrdinalIgnoreCase)) config.EspName = defaults.EspName;
        if (!json.Contains(nameof(EspWeapon), StringComparison.OrdinalIgnoreCase)) config.EspWeapon = defaults.EspWeapon;
        if (!json.Contains(nameof(EspFlags), StringComparison.OrdinalIgnoreCase)) config.EspFlags = defaults.EspFlags;
        if (!json.Contains(nameof(EspTextColor), StringComparison.OrdinalIgnoreCase)) config.EspTextColor = defaults.EspTextColor;
        if (!json.Contains(nameof(EspTextRainbow), StringComparison.OrdinalIgnoreCase)) config.EspTextRainbow = defaults.EspTextRainbow;
        if (!json.Contains(nameof(WatermarkTextColor), StringComparison.OrdinalIgnoreCase)) config.WatermarkTextColor = defaults.WatermarkTextColor;
        if (!json.Contains(nameof(WatermarkTextRainbow), StringComparison.OrdinalIgnoreCase)) config.WatermarkTextRainbow = defaults.WatermarkTextRainbow;
        if (!json.Contains(nameof(VoteTeller), StringComparison.OrdinalIgnoreCase)) config.VoteTeller = defaults.VoteTeller;
        if (!json.Contains(nameof(BunnyHop), StringComparison.OrdinalIgnoreCase)) config.BunnyHop = defaults.BunnyHop;
        if (!json.Contains(nameof(EspSnaplines), StringComparison.OrdinalIgnoreCase)) config.EspSnaplines = defaults.EspSnaplines;
        if (!json.Contains(nameof(EspDistance), StringComparison.OrdinalIgnoreCase)) config.EspDistance = defaults.EspDistance;
        if (!json.Contains(nameof(EspHealthBar), StringComparison.OrdinalIgnoreCase)) config.EspHealthBar = defaults.EspHealthBar;
        if (!json.Contains(nameof(EspHeadDot), StringComparison.OrdinalIgnoreCase)) config.EspHeadDot = defaults.EspHeadDot;
        if (!json.Contains(nameof(Radar), StringComparison.OrdinalIgnoreCase)) config.Radar = defaults.Radar;
        if (!json.Contains(nameof(Watermark), StringComparison.OrdinalIgnoreCase)) config.Watermark = defaults.Watermark;
        if (!json.Contains(nameof(AntiFlash), StringComparison.OrdinalIgnoreCase)) config.AntiFlash = defaults.AntiFlash;
        if (!json.Contains(nameof(EspGlow), StringComparison.OrdinalIgnoreCase)) config.EspGlow = defaults.EspGlow;
        if (!json.Contains(nameof(EspBoxCorner), StringComparison.OrdinalIgnoreCase)) config.EspBoxCorner = defaults.EspBoxCorner;
        if (!json.Contains(nameof(EspSpottedOnly), StringComparison.OrdinalIgnoreCase)) config.EspSpottedOnly = defaults.EspSpottedOnly;
        if (!json.Contains(nameof(EspArmorBar), StringComparison.OrdinalIgnoreCase)) config.EspArmorBar = defaults.EspArmorBar;
        if (!json.Contains(nameof(EspAmmo), StringComparison.OrdinalIgnoreCase)) config.EspAmmo = defaults.EspAmmo;
        if (!json.Contains(nameof(EspMoney), StringComparison.OrdinalIgnoreCase)) config.EspMoney = defaults.EspMoney;
        if (!json.Contains(nameof(EspPing), StringComparison.OrdinalIgnoreCase)) config.EspPing = defaults.EspPing;
        if (!json.Contains(nameof(EspReloading), StringComparison.OrdinalIgnoreCase)) config.EspReloading = defaults.EspReloading;
        if (!json.Contains(nameof(EspDefusing), StringComparison.OrdinalIgnoreCase)) config.EspDefusing = defaults.EspDefusing;
        if (!json.Contains(nameof(EspWeaponIcon), StringComparison.OrdinalIgnoreCase)) config.EspWeaponIcon = defaults.EspWeaponIcon;
        if (!json.Contains(nameof(SpectatorList), StringComparison.OrdinalIgnoreCase)) config.SpectatorList = defaults.SpectatorList;
        if (!json.Contains(nameof(ItemEsp), StringComparison.OrdinalIgnoreCase)) config.ItemEsp = defaults.ItemEsp;
        if (!json.Contains(nameof(VelocityGraph), StringComparison.OrdinalIgnoreCase)) config.VelocityGraph = defaults.VelocityGraph;
        if (!json.Contains(nameof(BombTimerColPanel), StringComparison.OrdinalIgnoreCase)) config.BombTimerColPanel = defaults.BombTimerColPanel;
        if (!json.Contains(nameof(BombTimerColText), StringComparison.OrdinalIgnoreCase)) config.BombTimerColText = defaults.BombTimerColText;
        if (!json.Contains(nameof(BombTimerColMarker), StringComparison.OrdinalIgnoreCase)) config.BombTimerColMarker = defaults.BombTimerColMarker;
        if (!json.Contains(nameof(BombTimerRainbow), StringComparison.OrdinalIgnoreCase)) config.BombTimerRainbow = defaults.BombTimerRainbow;
        if (!json.Contains(nameof(FreeCpu), StringComparison.OrdinalIgnoreCase)) config.FreeCpu = defaults.FreeCpu;
        if (!json.Contains(nameof(VSync), StringComparison.OrdinalIgnoreCase)) config.VSync = defaults.VSync;
        if (!json.Contains(nameof(StreamProof), StringComparison.OrdinalIgnoreCase)) config.StreamProof = defaults.StreamProof;
        if (!json.Contains(nameof(AimDynamicFov), StringComparison.OrdinalIgnoreCase)) config.AimDynamicFov = defaults.AimDynamicFov;
        if (!json.Contains(nameof(HitSound), StringComparison.OrdinalIgnoreCase)) config.HitSound = defaults.HitSound;
        if (!json.Contains(nameof(HitSoundName), StringComparison.OrdinalIgnoreCase)) config.HitSoundName = defaults.HitSoundName;
        if (!json.Contains(nameof(HitMarker), StringComparison.OrdinalIgnoreCase)) config.HitMarker = defaults.HitMarker;
        if (!json.Contains(nameof(OffscreenEnemy), StringComparison.OrdinalIgnoreCase)) config.OffscreenEnemy = defaults.OffscreenEnemy;
        if (!json.Contains(nameof(DamageText), StringComparison.OrdinalIgnoreCase)) config.DamageText = defaults.DamageText;
        if (!json.Contains(nameof(DamageTextDuration), StringComparison.OrdinalIgnoreCase)) config.DamageTextDuration = defaults.DamageTextDuration;
        if (!json.Contains(nameof(DamageTextSize), StringComparison.OrdinalIgnoreCase)) config.DamageTextSize = defaults.DamageTextSize;
        if (!json.Contains(nameof(EspEyeTraces), StringComparison.OrdinalIgnoreCase)) config.EspEyeTraces = defaults.EspEyeTraces;
        if (!json.Contains(nameof(GrenadeHelper), StringComparison.OrdinalIgnoreCase)) config.GrenadeHelper = defaults.GrenadeHelper;
        if (!json.Contains(nameof(GrenadeHelperWeaponFilter), StringComparison.OrdinalIgnoreCase)) config.GrenadeHelperWeaponFilter = defaults.GrenadeHelperWeaponFilter;
        if (!json.Contains(nameof(BunnyHopKey), StringComparison.OrdinalIgnoreCase)) config.BunnyHopKey = defaults.BunnyHopKey;
    }

    private static void SanitizeKeys(ConfigManager config)
    {
        var defaults = Default();
        if (config.MenuToggleKey == Keys.None) config.MenuToggleKey = defaults.MenuToggleKey;
        if (config.AimBotKey == Keys.None) config.AimBotKey = defaults.AimBotKey;
        if (config.TriggerBotKey == Keys.None) config.TriggerBotKey = defaults.TriggerBotKey;
        if (config.BunnyHopKey == Keys.None) config.BunnyHopKey = defaults.BunnyHopKey;
        if (config.AimFov <= 0) config.AimFov = defaults.AimFov;
        if (config.AimSmoothing <= 0) config.AimSmoothing = defaults.AimSmoothing;
    }

    private static void InitializeWatcher()
    {
        if (_watcher != null) return;

        try
        {
            var directory = Path.GetDirectoryName(Path.GetFullPath(ConfigFile)) ?? ".";
            var fileName = Path.GetFileName(ConfigFile);

            _watcher = new FileSystemWatcher(directory, fileName)
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size,
                EnableRaisingEvents = true
            };

            _watcher.Changed += (_, _) =>
            {
                Thread.Sleep(100);
                Reload();
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WARN] Could not initialize config watcher: {ex.Message}");
        }
    }

    public static void Save(ConfigManager options)
    {
        try
        {
            var json = JsonSerializer.Serialize(options, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(ConfigFile, json);
            UpdateCache(options);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Failed to save config: {ex.Message}");
        }
    }

    public static ConfigManager Default()
    {
        return new ConfigManager
        {
            HeadShootLine = false,
            AimBot = false,
            AimDynamicFov = false,
            AimFovCircle = false,
            AimFov = 15f,
            AimSmoothing = 3f,
            AimBoneIndex = 0,
            AimRcs = false,
            AimRcsStrength = 100f,
            BombTimer = true,
            BombTimerColPanel = new float[] { 0.08f, 0.08f, 0.1f, 0.8f },
            BombTimerColText = new float[] { 1f, 1f, 1f, 1f },
            BombTimerColMarker = new float[] { 1f, 1f, 0f, 1f },
            BombTimerRainbow = false,
            VoteTeller = false,
            EspAimCrosshair = false,
            EspBox = false,
            EspName = false,
            EspWeapon = false,
            EspWeaponIcon = false,
            EspFlags = false,
            EspSnaplines = false,
            EspDistance = false,
            EspHealthBar = false,
            EspHeadDot = false,
            EspBoxCorner = false,
            EspSpottedOnly = false,
            EspArmorBar = false,
            EspAmmo = false,
            EspMoney = false,
            EspPing = false,
            EspReloading = false,
            EspDefusing = false,
            EspBoxColor = new float[] { 1f, 0f, 0f, 1f },
            EspBoxColorTeam = new float[] { 0f, 1f, 0f, 1f },
            EspTextColor = new float[] { 1f, 1f, 1f, 1f },
            EspTextRainbow = false,
            WatermarkTextColor = new float[] { 1f, 1f, 1f, 1f },
            WatermarkTextRainbow = false,
            SkeletonEsp = false,
            TriggerBot = false,
            BunnyHop = false,
            AimBotKey = Keys.XButton2,
            TriggerBotKey = Keys.LMenu,
            MenuToggleKey = Keys.Insert,
            BunnyHopKey = Keys.Space,
            TeamCheck = true,
            Radar = false,
            RadarRange = 0.05f,
            Watermark = true,
            AntiFlash = false,
            EspGlow = false,
            GlowColorEnemy = new float[] { 1f, 0f, 0f, 0.5f },
            GlowColorTeam = new float[] { 0f, 1f, 0f, 0.5f },
            GlowHealthBased = false,
            GlowStyle = 3,
            SpectatorList = false,
            ItemEsp = false,
            VelocityGraph = false,
            FreeCpu = false,
            VSync = false,
            StreamProof = false,
            HitSound = true,
            HitSoundVolume = 0.5f,
            HitSoundName = "beep.wav",
            HitMarker = true,
            HitMarkerColor = new float[] { 1f, 1f, 1f, 1f },
            HitMarkerSize = 12f,
            HitMarkerGap = 2f,
            HitMarkerDuration = 300f,
            HitMarkerThickness = 2f,
            DamageText = true,
            DamageTextColor = new float[] { 1f, 0.2f, 0.2f, 1f },
            DamageTextDuration = 1000f,
            DamageTextSize = 16f,
            OffscreenEnemy = false,
            EspEyeTraces = false,
            GrenadeHelper = false,
            GrenadeHelperWeaponFilter = -1
        };
    }

    public static string GetKeyName(Keys key)
    {
        return key switch
        {
            Keys.LButton => "LMB",
            Keys.RButton => "RMB",
            Keys.MButton => "MMB",
            Keys.XButton1 => "Mouse4",
            Keys.XButton2 => "Mouse5",
            Keys.LMenu => "LAlt",
            Keys.RMenu => "RAlt",
            Keys.LShiftKey => "LShift",
            Keys.RShiftKey => "RShift",
            Keys.LControlKey => "LCtrl",
            Keys.RControlKey => "RCtrl",
            Keys.Insert => "Insert",
            Keys.Delete => "Delete",
            Keys.Home => "Home",
            Keys.End => "End",
            Keys.Capital => "CapsLock",
            _ => key.ToString()
        };
    }
}

public class KeysJsonConverter : JsonConverter<Keys>
{
    public override Keys Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var str = reader.GetString();
            if (Enum.TryParse<Keys>(str, true, out var result))
                return result;
        }
        else if (reader.TokenType == JsonTokenType.Number)
        {
            return (Keys)reader.GetInt32();
        }

        return Keys.None;
    }

    public override void Write(Utf8JsonWriter writer, Keys value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}