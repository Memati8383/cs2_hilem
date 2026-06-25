using System.Diagnostics;
using System.Numerics;
using ClickableTransparentOverlay;
using CS2Cheat.Core;
using CS2Cheat.Data.Game;
using CS2Cheat.Features;
using CS2Cheat.Utils;
using System.Threading;
using System.Collections.Concurrent;
using ImGuiNET;
using Keys = Process.NET.Native.Types.Keys;

namespace CS2Cheat.Graphics;

public class OverlayRenderer : Overlay
{
    private readonly GameProcess _gameProcess;
    private readonly GameData _gameData;
    private ConfigManager _config;
    private bool _showMenu = true, _menuKeyWasDown, _rShiftWasDown, _endWasDown, _styleApplied, _firstFrame = true;
    private int _activeTab = 0;
    private string? _waitingForBind;
    private readonly BombTimer _bombTimer;
    private readonly VoteTeller _voteTeller;
    private readonly AimBot _aimBot;
    private readonly TriggerBot _triggerBot;
    private readonly BunnyHop _bunnyHop;
    private float _menuAlpha;
    private bool _streamProofApplied;
    private float _animTime;
    private readonly Stopwatch _fpsTimer = Stopwatch.StartNew();
    private int _fpsFrameCount;
    private float _currentFps = 60f;

    // Premium Animation State
    private float _targetTabY;
    private float _currentTabY;
    private float _contentFadeAlpha;
    private int _previousTab = -1;
    private readonly Dictionary<int, float> _tabHoverAnims = new();
    private readonly Dictionary<string, float> _cardHoverAnims = new();
    private readonly Dictionary<string, float> _cardEntryAnims = new();
    private readonly List<Particle> _particles = new();
    private readonly List<NeonLine> _neonLines = new();
    private readonly Stopwatch _animTimer = Stopwatch.StartNew();
    private float _lastFrameTime;
    private readonly Dictionary<string, float> _toggleAnimState = new();

    // Easing
    private static float EaseOutCubic(float t) => 1f - (float)Math.Pow(1 - t, 3);
    private static float EaseOutExpo(float t) => t >= 1f ? 1f : 1f - (float)Math.Pow(2, -10 * t);
    private static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1;
        return 1 + c3 * (float)Math.Pow(t - 1, 3) + c1 * (float)Math.Pow(t - 1, 2);
    }
    private static float Lerp(float a, float b, float t) => a + (b - a) * Math.Clamp(t, 0f, 1f);

    private struct Particle
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public float Size;
        public float Alpha;
        public float Life;
        public float MaxLife;
        public Vector4 Color;
        public float PulsePhase;
    }

    private struct NeonLine
    {
        public Vector2 Start;
        public Vector2 End;
        public float Speed;
        public float Progress;
        public Vector4 Color;
    }

    public enum LogLevel { Debug, Info, Warning, Error, Success }
    public struct LogEntry
    {
        public DateTime Time;
        public LogLevel Level;
        public string Category;
        public string Message;
        public float Progress;
        public string ProgressLabel;
    }
    private readonly ConcurrentQueue<LogEntry> _logQueue = new();
    private readonly List<LogEntry> _logs = new();
    private readonly object _logsLock = new();

    static readonly Vector2 MenuSize = new(860, 600);
    const float SidebarW = 220f;
    const float HeaderH = 60f;
    const float BottomH = 34f;

    // ─── Premium Cyberpunk Theme ───
    static readonly Vector4 C_Bg = new(0.035f, 0.035f, 0.065f, 1f);
    static readonly Vector4 C_Sidebar = new(0.045f, 0.045f, 0.08f, 1f);
    static readonly Vector4 C_Card = new(0.06f, 0.06f, 0.11f, 0.93f);
    static readonly Vector4 C_Accent = new(0f, 0.85f, 1f, 1f);
    static readonly Vector4 C_Accent2 = new(0.7f, 0.2f, 1f, 1f);
    static readonly Vector4 C_AccentDim = new(0f, 0.55f, 0.7f, 1f);
    static readonly Vector4 C_Text = new(0.92f, 0.94f, 0.98f, 1f);
    static readonly Vector4 C_TextSub = new(0.62f, 0.67f, 0.78f, 1f);
    static readonly Vector4 C_TextMuted = new(0.32f, 0.36f, 0.46f, 1f);
    static readonly Vector4 C_Border = new(0.11f, 0.12f, 0.19f, 1f);
    static readonly Vector4 C_TogBg = new(0.09f, 0.09f, 0.15f, 1f);
    static readonly Vector4 C_Red = new(1f, 0.3f, 0.35f, 1f);
    static readonly Vector4 C_Yellow = new(1f, 0.80f, 0.20f, 1f);
    static readonly Vector4 C_Green = new(0.2f, 0.90f, 0.50f, 1f);

    static readonly string[][] TabLabels = {
        new[] { "D", "DASHBOARD" },
        new[] { "A", "COMBAT" },
        new[] { "V", "VISUALS" },
        new[] { "M", "MISC" },
        new[] { "S", "SETTINGS" },
    };

    public OverlayRenderer(GameProcess gp, GameData gd) : base(true)
    {
        _gameProcess = gp; _gameData = gd;
        _config = ConfigManager.Load();
        _bombTimer = new BombTimer(gp);
        _voteTeller = new VoteTeller(gp);
        _aimBot = new AimBot(gp, gd);
        _triggerBot = new TriggerBot(gp, gd);
        _bunnyHop = new BunnyHop(gp, gd);
        _targetTabY = 90f;
        _currentTabY = 90f;
        InitParticles();
        InitNeonLines();
    }

    private void InitParticles()
    {
        var rng = new Random();
        for (int i = 0; i < 35; i++)
            _particles.Add(new Particle
            {
                Position = new Vector2((float)rng.NextDouble() * MenuSize.X, (float)rng.NextDouble() * MenuSize.Y),
                Velocity = new Vector2((float)(rng.NextDouble() - 0.5) * 0.35f, (float)(rng.NextDouble() - 0.5) * 0.35f),
                Size = (float)rng.NextDouble() * 2.2f + 0.5f,
                Alpha = (float)rng.NextDouble() * 0.35f + 0.08f,
                Life = (float)rng.NextDouble() * 8f,
                MaxLife = (float)rng.NextDouble() * 6f + 4f,
                Color = rng.NextDouble() > 0.5 ? new Vector4(0f, 0.85f, 1f, 1f) : new Vector4(0.7f, 0.2f, 1f, 1f),
                PulsePhase = (float)rng.NextDouble() * MathF.PI * 2
            });
    }

    private void InitNeonLines()
    {
        var rng = new Random();
        for (int i = 0; i < 5; i++)
            _neonLines.Add(new NeonLine
            {
                Start = new Vector2((float)rng.NextDouble() * MenuSize.X, (float)rng.NextDouble() * MenuSize.Y),
                End = new Vector2((float)rng.NextDouble() * MenuSize.X, (float)rng.NextDouble() * MenuSize.Y),
                Speed = (float)rng.NextDouble() * 0.25f + 0.08f,
                Progress = (float)rng.NextDouble(),
                Color = rng.NextDouble() > 0.6 ? new Vector4(0f, 0.85f, 1f, 1f) : new Vector4(0.7f, 0.2f, 1f, 1f)
            });
    }

    protected override Task PostInitialized()
    {
        var h = this.window.Handle;
        var ex = User32.GetWindowLong(h, User32.GWL_EXSTYLE);
        User32.SetWindowLong(h, User32.GWL_EXSTYLE, ex | User32.WS_EX_NOACTIVATE | User32.WS_EX_TOOLWINDOW);
        UpdateOverlayGeometry();
        if (System.IO.File.Exists(@"C:\Windows\Fonts\verdanab.ttf"))
            try { ReplaceFont(@"C:\Windows\Fonts\verdanab.ttf", 15, FontGlyphRangeType.English); } catch { }
        return Task.CompletedTask;
    }

    protected override void Render()
    {
        if (Keys.F6.IsKeyDown()) Environment.Exit(0);
        UpdateOverlayGeometry();
        if (!_gameProcess.IsValid) return;
        _config = ConfigManager.Load();
        ApplyStreamProof();

        var mk = _config.MenuToggleKey;
        var mkd = mk.IsKeyDown();
        if (mkd && !_menuKeyWasDown) _showMenu = !_showMenu;
        var rShift = Keys.RShiftKey.IsKeyDown();
        if (rShift && !_rShiftWasDown) _showMenu = !_showMenu;
        _rShiftWasDown = rShift;
        var end = Keys.End.IsKeyDown();
        if (end && !_endWasDown && _showMenu) { _showMenu = false; ConfigManager.Save(_config); }
        _endWasDown = end;

        float targetAlpha = _showMenu ? 1f : 0f;
        _menuAlpha += (targetAlpha - _menuAlpha) * (_showMenu ? 0.07f : 0.09f);
        if (_menuAlpha < 0.001f) _menuAlpha = 0f; if (_menuAlpha > 0.999f) _menuAlpha = 1f;

        float currentTime = (float)_animTimer.Elapsed.TotalSeconds;
        float dt = currentTime - _lastFrameTime;
        _lastFrameTime = currentTime;
        _animTime += dt;

        var io = ImGui.GetIO();
        io.MouseDrawCursor = _showMenu;

        if ((mkd && !_menuKeyWasDown) || (_firstFrame && _showMenu))
        {
            _firstFrame = false;
            var h = this.window.Handle;
            var ex = User32.GetWindowLong(h, User32.GWL_EXSTYLE);
            if (_showMenu) { User32.SetWindowLong(h, User32.GWL_EXSTYLE, ex & ~0x20); User32.SetForegroundWindow(h); }
            else { User32.SetWindowLong(h, User32.GWL_EXSTYLE, ex | 0x20); User32.SetForegroundWindow(_gameProcess.Process?.MainWindowHandle ?? IntPtr.Zero); }
        }
        _menuKeyWasDown = mkd;

        TacticalManager.Update(_gameProcess, _gameData);
        Hitmarker.Update(_gameProcess, _gameData);
        HitSound.Update(_gameProcess, _gameData);
        DamageText.Update(_gameProcess, _gameData);

        _fpsFrameCount++;
        if (_fpsTimer.ElapsedMilliseconds >= 500)
        {
            _currentFps = _fpsFrameCount / (float)_fpsTimer.Elapsed.TotalSeconds;
            _fpsFrameCount = 0;
            _fpsTimer.Restart();
        }

        if (_config.GrenadeHelper && Keys.LButton.IsKeyDown() && !_menuKeyWasDown)
        {
            bool isMenuHovered = _showMenu && ImGui.IsWindowHovered(ImGuiHoveredFlags.AnyWindow);
            if (!isMenuHovered && _gameProcess.IsGameForeground)
                if (GrenadeHelper.ApplyViewangles(_gameProcess, out var ta))
                    _gameProcess.ModuleClient?.Write(Offsets.dwViewAngles, ta);
        }

        var dl = ImGui.GetBackgroundDrawList();
        RenderVisuals(dl);
        DrawActiveFeatures(dl);

        if (_menuAlpha > 0.01f)
        {
            ImGui.SetNextWindowPos(Vector2.Zero);
            ImGui.SetNextWindowSize(io.DisplaySize);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
            ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(0, 0, 0, 0.2f * _menuAlpha));
            ImGui.Begin("##bg", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoNav);
            ImGui.End();
            ImGui.PopStyleColor();
            ImGui.PopStyleVar();
            RenderMenu();
        }
    }

    void ApplyStyle()
    {
        if (_styleApplied) return;
        var s = ImGui.GetStyle();
        s.WindowRounding = 16f; s.ChildRounding = 12f; s.FrameRounding = 10f;
        s.PopupRounding = 12f; s.GrabRounding = 10f; s.ScrollbarRounding = 16f;
        s.WindowBorderSize = 0f; s.FrameBorderSize = 0f; s.ChildBorderSize = 1f;
        s.ItemSpacing = new Vector2(12, 10);
        s.WindowPadding = Vector2.Zero;
        s.FramePadding = new Vector2(12, 8);
        s.ScrollbarSize = 5;
        s.Colors[(int)ImGuiCol.WindowBg] = C_Bg;
        s.Colors[(int)ImGuiCol.Border] = C_Border;
        s.Colors[(int)ImGuiCol.ChildBg] = new Vector4(0, 0, 0, 0);
        s.Colors[(int)ImGuiCol.FrameBg] = C_TogBg;
        s.Colors[(int)ImGuiCol.FrameBgHovered] = new Vector4(0.11f, 0.11f, 0.19f, 1f);
        s.Colors[(int)ImGuiCol.FrameBgActive] = new Vector4(0.14f, 0.14f, 0.24f, 1f);
        s.Colors[(int)ImGuiCol.Button] = new Vector4(0.07f, 0.07f, 0.13f, 1f);
        s.Colors[(int)ImGuiCol.ButtonHovered] = new Vector4(0f, 0.85f, 1f, 0.15f);
        s.Colors[(int)ImGuiCol.ButtonActive] = new Vector4(0f, 0.85f, 1f, 0.25f);
        s.Colors[(int)ImGuiCol.CheckMark] = C_Accent;
        s.Colors[(int)ImGuiCol.SliderGrab] = C_Accent;
        s.Colors[(int)ImGuiCol.SliderGrabActive] = C_Accent2;
        s.Colors[(int)ImGuiCol.Text] = C_Text;
        s.Colors[(int)ImGuiCol.ScrollbarBg] = Vector4.Zero;
        s.Colors[(int)ImGuiCol.ScrollbarGrab] = new Vector4(0.11f, 0.12f, 0.19f, 1f);
        s.Colors[(int)ImGuiCol.ScrollbarGrabHovered] = new Vector4(0f, 0.85f, 1f, 0.3f);
        s.Colors[(int)ImGuiCol.ScrollbarGrabActive] = new Vector4(0f, 0.85f, 1f, 0.5f);
        _styleApplied = true;
    }

    uint AccentGrad(float t) => ToColor(new Vector4(
        C_Accent.X + (C_Accent2.X - C_Accent.X) * t,
        C_Accent.Y + (C_Accent2.Y - C_Accent.Y) * t,
        C_Accent.Z + (C_Accent2.Z - C_Accent.Z) * t, 1f));

    // ═══════════════════════════════════════════════════════
    //  MAIN MENU RENDER
    // ═══════════════════════════════════════════════════════
    void RenderMenu()
    {
        ApplyStyle();
        ImGui.PushStyleVar(ImGuiStyleVar.Alpha, _menuAlpha);
        var io = ImGui.GetIO();
        var pos = (io.DisplaySize - MenuSize) * 0.5f;
        ImGui.SetNextWindowPos(pos, ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowSize(MenuSize, ImGuiCond.FirstUseEver);
        ImGui.Begin("##main", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoTitleBar);

        var p = ImGui.GetWindowPos();
        var dl = ImGui.GetWindowDrawList();

        // ── Background ──
        dl.AddRectFilled(p, p + MenuSize, ToColor(C_Bg), 16f);

        // ── Animated Grid ──
        float gridOff = (_animTime * 8f) % 50f;
        for (float i = -50f; i < MenuSize.X + 50f; i += 50f)
            dl.AddLine(new Vector2(p.X + i + gridOff, p.Y), new Vector2(p.X + i + gridOff, p.Y + MenuSize.Y),
                ToColor(new Vector4(1, 1, 1, 0.012f * _menuAlpha)), 0.5f);
        for (float i = -50f; i < MenuSize.Y + 50f; i += 50f)
            dl.AddLine(new Vector2(p.X, p.Y + i + gridOff), new Vector2(p.X + MenuSize.X, p.Y + i + gridOff),
                ToColor(new Vector4(1, 1, 1, 0.012f * _menuAlpha)), 0.5f);

        // ── Particles ──
        for (int i = 0; i < _particles.Count; i++)
        {
            var pt = _particles[i];
            pt.Position += pt.Velocity;
            pt.Life += 0.016f;
            pt.PulsePhase += 0.032f;
            if (pt.Life >= pt.MaxLife)
            { var rng = new Random(); pt.Position = new Vector2((float)rng.NextDouble() * MenuSize.X, (float)rng.NextDouble() * MenuSize.Y); pt.Life = 0; }
            if (pt.Position.X < 0 || pt.Position.X > MenuSize.X) pt.Velocity.X *= -1;
            if (pt.Position.Y < 0 || pt.Position.Y > MenuSize.Y) pt.Velocity.Y *= -1;
            float lr = pt.Life / pt.MaxLife;
            float fade = lr < 0.2f ? lr / 0.2f : lr > 0.8f ? (1f - lr) / 0.2f : 1f;
            float pulse = (float)Math.Sin(pt.PulsePhase) * 0.3f + 0.7f;
            float a = pt.Alpha * fade * pulse * _menuAlpha;
            dl.AddCircleFilled(p + pt.Position, pt.Size, ToColor(new Vector4(pt.Color.X, pt.Color.Y, pt.Color.Z, a * 0.5f)));
            dl.AddCircleFilled(p + pt.Position, pt.Size * 0.5f, ToColor(new Vector4(pt.Color.X, pt.Color.Y, pt.Color.Z, a)));
            _particles[i] = pt;
        }

        // ── Neon Scan Lines ──
        for (int i = 0; i < _neonLines.Count; i++)
        {
            var nl = _neonLines[i];
            nl.Progress += nl.Speed * 0.016f;
            if (nl.Progress > 1.2f) nl.Progress = -0.2f;
            if (nl.Progress > 0f && nl.Progress < 1f)
            {
                float a = (float)Math.Sin(nl.Progress * Math.PI) * 0.12f * _menuAlpha;
                Vector2 cur = Vector2.Lerp(nl.Start, nl.End, nl.Progress);
                Vector2 dir = Vector2.Normalize(nl.End - nl.Start);
                dl.AddLine(cur - dir * 25f, cur + dir * 25f, ToColor(new Vector4(nl.Color.X, nl.Color.Y, nl.Color.Z, a)), 1f);
            }
            _neonLines[i] = nl;
        }

        // ── Glow Border (Cyan) ──
        float gp = (float)Math.Sin(_animTime * 0.6f) * 0.5f + 0.5f;
        for (int i = 4; i >= 0; i--)
            dl.AddRect(p - new Vector2(i), p + MenuSize + new Vector2(i),
                ToColor(new Vector4(C_Accent.X, C_Accent.Y, C_Accent.Z, (0.07f + gp * 0.06f) / (i + 1) * _menuAlpha)),
                16f + i, ImDrawFlags.None, 1f);
        // Glow Border (Magenta)
        float g2 = (float)Math.Sin(_animTime * 0.4f + 1.5f) * 0.5f + 0.5f;
        for (int i = 2; i >= 0; i--)
            dl.AddRect(p - new Vector2(i), p + MenuSize + new Vector2(i),
                ToColor(new Vector4(C_Accent2.X, C_Accent2.Y, C_Accent2.Z, (0.035f + g2 * 0.025f) / (i + 1) * _menuAlpha)),
                16f + i, ImDrawFlags.None, 1f);
        dl.AddRect(p, p + MenuSize, ToColor(new Vector4(C_Border.X, C_Border.Y, C_Border.Z, _menuAlpha)), 16f, ImDrawFlags.None, 1.5f);

        // ── Sidebar ──
        dl.AddRectFilled(p, p + new Vector2(SidebarW, MenuSize.Y), ToColor(C_Sidebar), 16f, ImDrawFlags.RoundCornersLeft);
        dl.AddRectFilled(p + new Vector2(SidebarW - 35, 0), p + new Vector2(SidebarW, MenuSize.Y),
            ToColor(new Vector4(C_Accent.X, C_Accent.Y, C_Accent.Z, 0.025f)));
        dl.AddLine(p + new Vector2(SidebarW, 0), p + new Vector2(SidebarW, MenuSize.Y),
            ToColor(new Vector4(C_Border.X, C_Border.Y, C_Border.Z, _menuAlpha)), 1f);

        // Animated accent line on sidebar edge
        float accentTravel = (_animTime * 35f) % (MenuSize.Y + 80f);
        dl.AddLine(p + new Vector2(SidebarW - 1, accentTravel - 50f), p + new Vector2(SidebarW - 1, accentTravel),
            ToColor(new Vector4(C_Accent.X, C_Accent.Y, C_Accent.Z, 0.55f * _menuAlpha)), 2f);
        dl.AddLine(p + new Vector2(SidebarW - 2, accentTravel - 50f), p + new Vector2(SidebarW - 2, accentTravel),
            ToColor(new Vector4(C_Accent.X, C_Accent.Y, C_Accent.Z, 0.12f * _menuAlpha)), 5f);

        // ── Header Branding ──
        float hp = (float)Math.Sin(_animTime * 1.2f) * 0.3f + 0.7f;
        dl.AddRectFilled(p + new Vector2(16, 14), p + new Vector2(200, 50),
            ToColor(new Vector4(C_Accent.X, C_Accent.Y, C_Accent.Z, 0.04f * hp)), 10f);
        dl.AddText(null, 24f, p + new Vector2(22, 16), AccentGrad(hp), "NEXUS");
        dl.AddText(null, 10f, p + new Vector2(23, 42), ToColor(C_TextMuted), "EXTERNAL v2.0");

        // Version badge
        var bp = p + new Vector2(125, 19);
        dl.AddRectFilled(bp, bp + new Vector2(58, 16), ToColor(new Vector4(C_Accent.X, C_Accent.Y, C_Accent.Z, 0.13f)), 8f);
        dl.AddText(null, 9f, bp + new Vector2(9, 3), ToColor(C_Accent), "PREMIUM");

        // ── Tab Navigation ──
        float tabStartY = 78f;
        float tabH = 42f;
        float tabGap = 4f;

        _targetTabY = tabStartY + (_activeTab * (tabH + tabGap));
        _currentTabY = Lerp(_currentTabY, _targetTabY, 0.13f);

        // Active tab bg glow
        float tGlow = (float)Math.Sin(_animTime * 2.5f) * 0.07f + 0.11f;
        dl.AddRectFilled(p + new Vector2(6, _currentTabY - 1), p + new Vector2(SidebarW - 6, _currentTabY + tabH + 1),
            ToColor(new Vector4(C_Accent.X, C_Accent.Y, C_Accent.Z, 0.06f)), 10f);
        dl.AddRectFilled(p + new Vector2(8, _currentTabY), p + new Vector2(SidebarW - 8, _currentTabY + tabH),
            ToColor(new Vector4(C_Accent.X, C_Accent.Y, C_Accent.Z, 0.08f)), 8f);

        // Indicator bar
        float indH = tabH - 14f;
        float indY = _currentTabY + 7f;
        dl.AddRectFilled(p + new Vector2(2, indY), p + new Vector2(5, indY + indH), ToColor(C_Accent), 3f);
        dl.AddRectFilled(p + new Vector2(0, indY - 3), p + new Vector2(7, indY + indH + 3),
            ToColor(new Vector4(C_Accent.X, C_Accent.Y, C_Accent.Z, 0.22f)), 4f);

        float ty = tabStartY;
        for (int i = 0; i < TabLabels.Length; i++)
        {
            bool act = _activeTab == i;
            var tPos = p + new Vector2(0, ty);
            var tSize = new Vector2(SidebarW, tabH);
            bool hov = io.MousePos.X >= tPos.X && io.MousePos.X <= tPos.X + tSize.X &&
                       io.MousePos.Y >= tPos.Y && io.MousePos.Y <= tPos.Y + tSize.Y;
            if (!_tabHoverAnims.ContainsKey(i)) _tabHoverAnims[i] = 0f;
            _tabHoverAnims[i] = Lerp(_tabHoverAnims[i], hov && !act ? 1f : 0f, 0.14f);

            if (_tabHoverAnims[i] > 0.01f)
                dl.AddRectFilled(tPos + new Vector2(14, 4), tPos + new Vector2(SidebarW - 14, tabH - 4),
                    ToColor(new Vector4(1, 1, 1, 0.03f * _tabHoverAnims[i])), 8f);

            ImGui.SetCursorPos(new Vector2(0, ty));
            ImGui.InvisibleButton($"##tab_{i}", tSize);
            if (ImGui.IsItemClicked()) { _previousTab = _activeTab; _activeTab = i; _contentFadeAlpha = 0f; }

            var iCol = act ? ToColor(C_Accent) : ToColor(C_TextMuted);
            var tCol = act ? ToColor(C_Text) : ToColor(C_TextSub);
            float iScale = act ? 1f + (float)Math.Sin(_animTime * 3f) * 0.04f : 1f;
            dl.AddText(null, 15f * iScale, tPos + new Vector2(24, (tabH - 15) * 0.5f), iCol, TabLabels[i][0]);
            dl.AddText(null, 11f, tPos + new Vector2(50, (tabH - 11) * 0.5f), tCol, TabLabels[i][1]);
            ty += tabH + tabGap;
        }

        // ── Sidebar Status ──
        float sPulse = (float)Math.Sin(_animTime * 1.8f) * 0.3f + 0.7f;
        dl.AddText(null, 9f, p + new Vector2(18, MenuSize.Y - 48), ToColor(C_TextMuted), "STATUS:");
        dl.AddCircleFilled(p + new Vector2(60, MenuSize.Y - 44), 4f, ToColor(C_Green));
        dl.AddCircle(p + new Vector2(60, MenuSize.Y - 44), 7f + (float)Math.Sin(_animTime * 2.5f) * 2f,
            ToColor(new Vector4(C_Green.X, C_Green.Y, C_Green.Z, 0.22f * sPulse)), 16, 1.5f);
        dl.AddCircle(p + new Vector2(60, MenuSize.Y - 44), 10f + (float)Math.Sin(_animTime * 2.5f + 1f) * 3f,
            ToColor(new Vector4(C_Green.X, C_Green.Y, C_Green.Z, 0.08f * sPulse)), 16, 1f);
        dl.AddText(null, 9f, p + new Vector2(70, MenuSize.Y - 48), ToColor(C_Green), "ONLINE");

        // ── Content Header ──
        float hx = p.X + SidebarW;
        float hw = MenuSize.X - SidebarW;
        dl.AddRectFilled(new Vector2(hx, p.Y), new Vector2(hx + hw, p.Y + HeaderH),
            ToColor(new Vector4(0.035f, 0.035f, 0.065f, 0.5f)));
        dl.AddLine(new Vector2(hx, p.Y + HeaderH), new Vector2(hx + hw, p.Y + HeaderH),
            ToColor(new Vector4(C_Border.X, C_Border.Y, C_Border.Z, _menuAlpha)));
        float haw = (float)Math.Sin(_animTime * 0.4f) * 25f + 70f;
        dl.AddRectFilled(new Vector2(hx + 22, p.Y + HeaderH - 3), new Vector2(hx + 22 + haw, p.Y + HeaderH - 1),
            ToColor(new Vector4(C_Accent.X, C_Accent.Y, C_Accent.Z, 0.5f)), 2f);
        dl.AddRectFilled(new Vector2(hx + 22 + haw + 8, p.Y + HeaderH - 2), new Vector2(hx + 22 + haw + 35, p.Y + HeaderH - 1),
            ToColor(new Vector4(C_Accent2.X, C_Accent2.Y, C_Accent2.Z, 0.3f)), 1f);
        dl.AddText(null, 17f, new Vector2(hx + 22, p.Y + 13), ToColor(C_Text), TabLabels[_activeTab][1]);
        dl.AddText(null, 9f, new Vector2(hx + 22, p.Y + 34), ToColor(C_TextMuted), "NEXUS EXTERNAL INTERFACE");

        // ── Content Viewport ──
        float cx = SidebarW + 16f;
        float cy = HeaderH + 10f;
        float cw = MenuSize.X - SidebarW - 32f;
        float ch = MenuSize.Y - HeaderH - BottomH - 20f;
        ImGui.SetCursorPos(new Vector2(cx, cy));
        ImGui.BeginChild("##content_vp", new Vector2(cw, ch), ImGuiChildFlags.None, ImGuiWindowFlags.None);

        if (_previousTab != _activeTab) { _contentFadeAlpha = Math.Min(_contentFadeAlpha + 0.06f, 1f); if (_contentFadeAlpha >= 1f) _previousTab = _activeTab; }
        else _contentFadeAlpha = 1f;

        ImGui.PushStyleVar(ImGuiStyleVar.Alpha, _contentFadeAlpha);
        switch (_activeTab) { case 0: TabDashboard(); break; case 1: TabAimbot(); break; case 2: TabVisuals(); break; case 3: TabMisc(); break; case 4: TabSettings(); break; }
        ImGui.PopStyleVar();
        ImGui.EndChild();

        // ── Footer ──
        float by = MenuSize.Y - BottomH;
        dl.AddRectFilled(new Vector2(hx, p.Y + by), new Vector2(hx + hw, p.Y + by + BottomH), ToColor(C_Sidebar), 16f, ImDrawFlags.RoundCornersBottomRight);
        dl.AddLine(new Vector2(hx, p.Y + by), new Vector2(hx + hw, p.Y + by), ToColor(new Vector4(C_Border.X, C_Border.Y, C_Border.Z, _menuAlpha)));
        float fGlow = (float)Math.Sin(_animTime * 1.5f) * 0.07f + 0.11f;
        dl.AddRectFilled(new Vector2(hx, p.Y + by), new Vector2(hx + hw, p.Y + by + 1), ToColor(new Vector4(C_Accent.X, C_Accent.Y, C_Accent.Z, fGlow)));
        dl.AddText(null, 10f, new Vector2(hx + 16, p.Y + by + (BottomH - 10) * 0.5f), ToColor(C_TextMuted),
            $"  {_currentFps:0} Hz  |  STABLE  |  {_gameProcess.WindowRectangleClient.Width}x{_gameProcess.WindowRectangleClient.Height}");

        ImGui.End();
        ImGui.PopStyleVar();
    }

    // ═══════════════════════════════════════════
    //  TAB CONTENT
    // ═══════════════════════════════════════════
    void TabDashboard()
    {
        BeginCard("SYSTEM STATUS");
        Info("Game", "Counter-Strike 2"); Info("Build", "v1.7.13");
        Info("Status", "Operational", C_Green); Info("User", Environment.UserName);
        EndCard();
        SameLine();
        BeginCard("ACTIVE FEATURES");
        int c = 0;
        if (_config.AimBot) c++; if (_config.EspBox) c++; if (_config.TriggerBot) c++;
        if (_config.BunnyHop) c++; if (_config.EspGlow) c++;
        Info("Total Active", c.ToString(), c > 0 ? C_Accent : C_TextSub);
        EndCard();
        BeginCard("NEWS & UPDATES");
        ImGui.TextWrapped("Welcome to Nexus External Premium. Use Insert or RShift to toggle menu.");
        ImGui.Dummy(new Vector2(0, 8));
        if (Button("JOIN DISCORD", C_Accent2))
            try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("https://discord.gg/yourlink") { UseShellExecute = true }); } catch { }
        EndCard();
    }

    void TabAimbot()
    {
        BeginCard("COMBAT ASSIST");
        bool v; v = _config.AimBot; if (Toggle("Master Aimbot", ref v, _config.AimBotKey)) _config.AimBot = v;
        v = _config.AimFovCircle; if (Toggle("Draw FOV Circle", ref v)) _config.AimFovCircle = v;
        v = _config.AimDynamicFov; if (Toggle("Dynamic FOV", ref v)) _config.AimDynamicFov = v;
        EndCard();
        SameLine();
        BeginCard("AIM SETTINGS");
        float fv = _config.AimFov; if (Slider("Field of View", "##fov", ref fv, 1f, 60f, "%.1f")) _config.AimFov = fv;
        fv = _config.AimSmoothing; if (Slider("Smooth Speed", "##sm", ref fv, 1f, 30f, "%.1f")) _config.AimSmoothing = fv;
        int bone = _config.AimBoneIndex; if (Combo("Target Bone", "##bone", ref bone, ConfigManager.BoneDisplayNames)) _config.AimBoneIndex = bone;
        EndCard();
        BeginCard("RECOIL CONTROL");
        v = _config.AimRcs; if (Toggle("Enable RCS", ref v)) _config.AimRcs = v;
        if (_config.AimRcs) { fv = _config.AimRcsStrength; if (Slider("RCS Strength", "##rcs", ref fv, 0f, 100f, "%.0f%%")) _config.AimRcsStrength = fv; }
        EndCard();
    }

    void TabVisuals()
    {
        BeginCard("PLAYER ESP");
        bool v; v = _config.EspBox; if (Toggle("Box ESP", ref v)) _config.EspBox = v;
        if (_config.EspBox) { v = _config.EspBoxCorner; if (Toggle("    Cornered", ref v, null, 16f)) _config.EspBoxCorner = v; }
        v = _config.EspName; if (Toggle("Names", ref v)) _config.EspName = v;
        v = _config.EspWeapon; if (Toggle("Weapons", ref v)) _config.EspWeapon = v;
        v = _config.EspWeaponIcon; if (Toggle("Weapon Icons", ref v)) _config.EspWeaponIcon = v;
        v = _config.EspHealthBar; if (Toggle("Health Bar", ref v)) _config.EspHealthBar = v;
        v = _config.EspArmorBar; if (Toggle("Armor Bar", ref v)) _config.EspArmorBar = v;
        v = _config.SkeletonEsp; if (Toggle("Skeletons", ref v)) _config.SkeletonEsp = v;
        v = _config.EspEyeTraces; if (Toggle("Eye Traces", ref v)) _config.EspEyeTraces = v;
        v = _config.EspSnaplines; if (Toggle("Snaplines", ref v)) _config.EspSnaplines = v;
        EndCard();
        SameLine();
        BeginCard("WORLD & MISC");
        v = _config.EspAimCrosshair; if (Toggle("Crosshair", ref v)) _config.EspAimCrosshair = v;
        v = _config.AntiFlash; if (Toggle("No-Flash", ref v)) _config.AntiFlash = v;
        v = _config.EspGlow; if (Toggle("Glow ESP", ref v)) _config.EspGlow = v;
        if (_config.EspGlow)
        {
            v = _config.GlowHealthBased; if (Toggle("    Health Based", ref v)) _config.GlowHealthBased = v;
            int style = _config.GlowStyle; if (Combo("Glow Style", "##gs", ref style, new[] { "Default", "Pulse", "Outline", "Solid" })) _config.GlowStyle = style;
            if (!_config.GlowHealthBased) { ColorPick("Glow Enemy", "##ge", _config.GlowColorEnemy); ColorPick("Glow Team", "##gt", _config.GlowColorTeam); }
        }
        v = _config.OffscreenEnemy; if (Toggle("Out-of-FOV Arrows", ref v)) _config.OffscreenEnemy = v;
        v = _config.Radar; if (Toggle("2D Radar", ref v)) _config.Radar = v;
        if (_config.Radar) { float rr = _config.RadarRange; if (Slider("Radar Zoom", "##rz", ref rr, 0.01f, 0.2f, "%.3f")) _config.RadarRange = rr; }
        v = _config.ItemEsp; if (Toggle("Item ESP", ref v)) _config.ItemEsp = v;
        v = _config.BombTimer; if (Toggle("Bomb Timer", ref v)) _config.BombTimer = v;
        v = _config.SpectatorList; if (Toggle("Spectator List", ref v)) _config.SpectatorList = v;
        EndCard();
        BeginCard("HITMARKER SETTINGS");
        v = _config.HitMarker; if (Toggle("Enable Hitmarker", ref v)) _config.HitMarker = v;
        v = _config.HitSound; if (Toggle("Enable Hit Sound", ref v)) _config.HitSound = v;
        if (_config.HitSound)
        {
            float hsv = _config.HitSoundVolume; if (Slider("Sound Volume", "##hsv", ref hsv, 0f, 1f, "%.2f")) _config.HitSoundVolume = hsv;
            var snds = HitSound.GetAvailableSounds(); var idx = Array.IndexOf(snds, _config.HitSoundName); if (idx < 0) idx = 0; int sel = idx;
            if (Combo("Hit Sound", "##hs", ref sel, snds)) { _config.HitSoundName = snds[sel]; HitSound.Play(); }
            if (Button("TEST SOUND", C_Accent)) HitSound.Play();
        }
        if (_config.HitMarker)
        {
            float hms = _config.HitMarkerSize; if (Slider("Size", "##hms", ref hms, 1f, 30f, "%.0f")) _config.HitMarkerSize = hms;
            hms = _config.HitMarkerGap; if (Slider("Gap", "##hmg", ref hms, 0f, 10f, "%.0f")) _config.HitMarkerGap = hms;
            hms = _config.HitMarkerDuration; if (Slider("Duration (ms)", "##hmd", ref hms, 100f, 2000f, "%.0f")) _config.HitMarkerDuration = hms;
            hms = _config.HitMarkerThickness; if (Slider("Thickness", "##hmt", ref hms, 0.5f, 5f, "%.1f")) _config.HitMarkerThickness = hms;
            ColorPick("Color", "##hmc", _config.HitMarkerColor);
        }
        v = _config.DamageText; if (Toggle("Floating Damage Text", ref v)) _config.DamageText = v;
        if (_config.DamageText)
        {
            float dts = _config.DamageTextSize; if (Slider("Text Size", "##dts", ref dts, 10f, 40f, "%.0f")) _config.DamageTextSize = dts;
            dts = _config.DamageTextDuration; if (Slider("Duration (ms)", "##dtd", ref dts, 500f, 3000f, "%.0f")) _config.DamageTextDuration = dts;
            ColorPick("Text Color", "##dtc", _config.DamageTextColor);
        }
        EndCard();
        BeginCard("COLOR CONFIG");
        ColorPick("Enemy Color", "##ec", _config.EspBoxColor); ColorPick("Team Color", "##tc", _config.EspBoxColorTeam);
        ColorPick("ESP Text Color", "##etc", _config.EspTextColor);
        v = _config.EspTextRainbow; if (Toggle("ESP Text Rainbow", ref v)) _config.EspTextRainbow = v;
        ColorPick("Watermark Color", "##wmc", _config.WatermarkTextColor);
        v = _config.WatermarkTextRainbow; if (Toggle("Watermark Rainbow", ref v)) _config.WatermarkTextRainbow = v;
        ImGui.Separator();
        ColorPick("Bomb Panel", "##bp", _config.BombTimerColPanel); ColorPick("Bomb Text", "##bt", _config.BombTimerColText);
        ColorPick("Bomb Marker", "##bm", _config.BombTimerColMarker);
        v = _config.BombTimerRainbow; if (Toggle("Bomb Timer Rainbow", ref v)) _config.BombTimerRainbow = v;
        EndCard();
    }

    void TabMisc()
    {
        BeginCard("MOVEMENT");
        bool v; v = _config.BunnyHop; if (Toggle("Auto-Bhop", ref v, _config.BunnyHopKey)) _config.BunnyHop = v;
        v = _config.VelocityGraph; if (Toggle("Velocity Graph", ref v)) _config.VelocityGraph = v;
        EndCard();
        SameLine();
        BeginCard("UTILITIES");
        v = _config.TriggerBot; if (Toggle("Triggerbot", ref v, _config.TriggerBotKey)) _config.TriggerBot = v;
        v = _config.TeamCheck; if (Toggle("Team Check", ref v)) _config.TeamCheck = v;
        v = _config.VoteTeller; if (Toggle("Vote Reveal", ref v)) _config.VoteTeller = v;
        EndCard();
        BeginCard("GRENADE HELPER");
        v = _config.GrenadeHelper; if (Toggle("Enable Grenade Helper", ref v)) _config.GrenadeHelper = v;
        if (_config.GrenadeHelper)
        {
            bool aw = GrenadeHelper.AutoWeaponFilter;
            ImGui.TextColored(aw ? C_Green : C_Text, "Auto-detect weapon: " + (aw ? "ON" : "OFF"));
            var wl = new[] { "Auto (held grenade)" }.Concat(GrenadeHelper.GetWeaponLabels()).ToArray();
            int si = _config.GrenadeHelperWeaponFilter + 1; if (si < 0) si = 0;
            if (Combo("Weapon Filter", "##ghw", ref si, wl)) { _config.GrenadeHelperWeaponFilter = si - 1; if (si == 0) GrenadeHelper.ClearWeaponFilter(); else GrenadeHelper.SetWeapon(si - 1); }
            string mi = string.IsNullOrEmpty(GrenadeHelper.CurrentMap) ? "Detecting..." : GrenadeHelper.CurrentMap;
            ImGui.TextColored(C_TextSub, "Detected Map: " + mi);
            if (!string.IsNullOrEmpty(GrenadeHelper.CurrentMap) && GrenadeHelper.GetAvailableMaps().Contains(GrenadeHelper.CurrentMap)) ImGui.TextColored(C_Green, "Data loaded");
            else ImGui.TextColored(C_Yellow, "No lineup data for this map");
            var maps = GrenadeHelper.GetAvailableMaps();
            if (maps.Length > 0) { int mIdx = GrenadeHelper.SelectedMapIndex; if (mIdx < 0) mIdx = 0; if (Combo("Map Select", "##ghm", ref mIdx, maps)) GrenadeHelper.SetManualMap(mIdx); }
            ImGui.Spacing(); ImGui.TextWrapped("Lineups shown as colored dots. Walk near one for throw instructions. LMB applies angles.");
        }
        EndCard();
        BeginCard("INTERFACE");
        v = _config.Watermark; if (Toggle("Show Watermark", ref v)) _config.Watermark = v;
        v = _config.StreamProof; if (Toggle("Stream-Proof (OBS)", ref v)) _config.StreamProof = v;
        v = _config.FreeCpu; if (Toggle("Optimize CPU", ref v)) _config.FreeCpu = v;
        EndCard();
    }

    void TabSettings()
    {
        BeginCard("BINDINGS");
        KeyBind("Aim Key", "AimBotKey", _config.AimBotKey, k => _config.AimBotKey = k);
        KeyBind("Trigger Key", "TriggerBotKey", _config.TriggerBotKey, k => _config.TriggerBotKey = k);
        KeyBind("Bhop Key", "BunnyHopKey", _config.BunnyHopKey, k => _config.BunnyHopKey = k);
        KeyBind("Menu Toggle", "MenuToggleKey", _config.MenuToggleKey, k => _config.MenuToggleKey = k);
        EndCard();
        SameLine();
        BeginCard("CONFIGURATION");
        if (Button("SAVE CONFIG", C_Accent)) ConfigManager.Save(_config);
        if (Button("LOAD CONFIG")) { ConfigManager.Reload(); _config = ConfigManager.Load(); }
        if (Button("RESET DEFAULTS", C_Red)) { _config = ConfigManager.Default(); ConfigManager.Save(_config); }
        EndCard();
    }

    // ═══════════════════════════════════════════
    //  PREMIUM WIDGETS
    // ═══════════════════════════════════════════

    void Info(string label, string value, Vector4? valCol = null)
    {
        ImGui.TextColored(C_TextSub, label);
        ImGui.SameLine();
        float tw = ImGui.CalcTextSize(value).X;
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X - tw - 4);
        ImGui.TextColored(valCol ?? C_Text, value);
    }

    void BeginCard(string title)
    {
        string id = $"##c_{title}_{_activeTab}";
        if (!_cardHoverAnims.ContainsKey(id)) _cardHoverAnims[id] = 0f;
        if (!_cardEntryAnims.ContainsKey(id)) _cardEntryAnims[id] = 0f;
        _cardEntryAnims[id] = Math.Min(_cardEntryAnims[id] + 0.045f, 1f);
        float entryEase = EaseOutCubic(_cardEntryAnims[id]);

        float availW = ImGui.GetContentRegionAvail().X;
        bool hov = ImGui.IsMouseHoveringRect(ImGui.GetCursorScreenPos(), ImGui.GetCursorScreenPos() + new Vector2(availW, 100f));
        _cardHoverAnims[id] = Lerp(_cardHoverAnims[id], hov ? 1f : 0f, 0.12f);

        ImGui.PushStyleColor(ImGuiCol.ChildBg, C_Card);
        ImGui.BeginChild(id, new Vector2(availW, 0),
            ImGuiChildFlags.AlwaysAutoResize | ImGuiChildFlags.AutoResizeY,
            ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoScrollbar);

        var dl = ImGui.GetWindowDrawList();
        var cur = ImGui.GetCursorScreenPos();
        float ha = _cardHoverAnims[id];

        // Card header gradient
        dl.AddRectFilled(cur - new Vector2(8, 8), cur + new Vector2(ImGui.GetContentRegionAvail().X + 8, 24),
            ToColor(new Vector4(C_Accent.X, C_Accent.Y, C_Accent.Z, 0.07f + ha * 0.04f)), 8f, ImDrawFlags.RoundCornersTop);

        // Animated accent line
        float lw = (float)Math.Sin(_animTime * 1.8f + title.GetHashCode()) * 8f + 35f;
        dl.AddRectFilled(cur - new Vector2(8, 22), cur + new Vector2(-8 + lw, 24),
            ToColor(new Vector4(C_Accent.X, C_Accent.Y, C_Accent.Z, 0.55f)), 2f);

        dl.AddText(null, 12f, cur + new Vector2(4, -4), ToColor(C_Accent), title);
        ImGui.Dummy(new Vector2(0, 22));
        ImGui.Indent(4);
    }

    void EndCard() { ImGui.Unindent(4); ImGui.EndChild(); ImGui.PopStyleColor(); }

    void SameLine() => ImGui.SameLine(0, 8);

    bool Toggle(string label, ref bool value, Keys? bindKey = null, float indent = 0f)
    {
        var dl = ImGui.GetWindowDrawList();
        var cursor = ImGui.GetCursorScreenPos();
        float width = ImGui.GetContentRegionAvail().X;
        float height = 26f;

        ImGui.InvisibleButton(label, new Vector2(width, height));
        bool clicked = ImGui.IsItemClicked();
        if (clicked) { value = !value; ConfigManager.UpdateCache(_config); }

        string animKey = label + _activeTab;
        if (!_toggleAnimState.ContainsKey(animKey)) _toggleAnimState[animKey] = value ? 1f : 0f;
        _toggleAnimState[animKey] = Lerp(_toggleAnimState[animKey], value ? 1f : 0f, 0.18f);
        float animT = EaseOutCubic(_toggleAnimState[animKey]);

        float tw = 38f, th = 20f;
        var tPos = cursor + new Vector2(width - tw - 4, (height - th) * 0.5f);

        // Toggle background
        Vector4 bgCol = value
            ? Vector4.Lerp(new Vector4(C_TogBg.X, C_TogBg.Y, C_TogBg.Z, 1f), new Vector4(C_Accent.X, C_Accent.Y, C_Accent.Z, 1f), animT)
            : C_TogBg;
        dl.AddRectFilled(tPos, tPos + new Vector2(tw, th), ToColor(bgCol), th * 0.5f);

        // Glow when active
        if (animT > 0.01f)
        {
            float glowA = (float)Math.Sin(_animTime * 3f) * 0.08f + 0.18f;
            dl.AddRectFilled(tPos - new Vector2(2), tPos + new Vector2(tw + 2, th + 2),
                ToColor(new Vector4(C_Accent.X, C_Accent.Y, C_Accent.Z, glowA * animT)), th * 0.5f + 2);
            for (int i = 1; i <= 2; i++)
                dl.AddRect(tPos - new Vector2(i), tPos + new Vector2(tw + i, th + i),
                    ToColor(new Vector4(C_Accent.X, C_Accent.Y, C_Accent.Z, 0.12f / i * animT)), th * 0.5f + i);
        }

        // Animated knob
        float cr = (th - 6) * 0.5f;
        float knobX = tPos.X + cr + 3f + (tw - 2 * cr - 6f) * animT;
        dl.AddCircleFilled(new Vector2(knobX, tPos.Y + th * 0.5f), cr, ToColor(C_Text));
        if (animT > 0.5f)
            dl.AddCircle(new Vector2(knobX, tPos.Y + th * 0.5f), cr + 2,
                ToColor(new Vector4(1, 1, 1, 0.25f * animT)), 12, 1f);

        string display = label.StartsWith("\t") ? "    " + label.TrimStart('\t') : label;
        if (bindKey.HasValue) display += $"  [{ConfigManager.GetKeyName(bindKey.Value)}]";
        dl.AddText(cursor + new Vector2(indent, (height - 13) * 0.5f), ToColor(ImGui.IsItemHovered() ? C_Text : C_TextSub), display);
        return clicked;
    }

    bool Slider(string label, string id, ref float value, float min, float max, string format)
    {
        ImGui.TextColored(C_TextSub, label);
        ImGui.SameLine();
        string vs = value.ToString(format.Replace("%", " ").Replace("f", "0").Replace("d", "0"));
        float vw = ImGui.CalcTextSize(vs).X;
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X - vw - 4);
        ImGui.TextColored(C_Accent, vs);
        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(0, 2));
        ImGui.PushStyleColor(ImGuiCol.FrameBg, ToColor(new Vector4(0.05f, 0.05f, 0.08f, 1f)));
        ImGui.SetNextItemWidth(-1);
        bool changed = ImGui.SliderFloat(id, ref value, min, max, " ");
        ImGui.PopStyleColor();
        ImGui.PopStyleVar();
        if (changed) ConfigManager.UpdateCache(_config);
        ImGui.Spacing();
        return changed;
    }

    bool Combo(string label, string id, ref int value, string[] items)
    {
        ImGui.TextColored(C_TextSub, label);
        ImGui.SetNextItemWidth(-1);
        bool changed = ImGui.Combo(id, ref value, items, items.Length);
        if (changed) ConfigManager.UpdateCache(_config);
        ImGui.Spacing();
        return changed;
    }

    void ColorPick(string label, string id, float[] color)
    {
        var cv = new Vector4(color[0], color[1], color[2], color[3]);
        ImGui.TextColored(C_TextSub, label);
        ImGui.SetNextItemWidth(-1);
        if (ImGui.ColorEdit4(id, ref cv, ImGuiColorEditFlags.NoInputs))
        { color[0] = cv.X; color[1] = cv.Y; color[2] = cv.Z; color[3] = cv.W; ConfigManager.UpdateCache(_config); }
        ImGui.Spacing();
    }

    void KeyBind(string label, string bindId, Keys currentKey, Action<Keys> setter)
    {
        bool waiting = _waitingForBind == bindId;
        string txt = waiting ? "PRESS ANY KEY" : ConfigManager.GetKeyName(currentKey).ToUpper();
        ImGui.TextColored(C_TextSub, label);
        ImGui.SameLine();
        float bw = 110f;
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X - bw - 4);
        ImGui.PushStyleColor(ImGuiCol.Button, waiting ? C_Accent : new Vector4(0.10f, 0.10f, 0.16f, 1f));
        ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 8f);
        if (ImGui.Button($"{txt}##{bindId}", new Vector2(bw, 24))) _waitingForBind = waiting ? null : bindId;
        ImGui.PopStyleVar(); ImGui.PopStyleColor();
        if (waiting)
        {
            var k = ScanKey();
            if (k != Keys.None)
            {
                if (k == Keys.Escape) _waitingForBind = null;
                else { setter(k); ConfigManager.UpdateCache(_config); _waitingForBind = null; }
            }
        }
    }

    bool Button(string label, Vector4? color = null)
    {
        if (color.HasValue)
        {
            var c = color.Value;
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(c.X, c.Y, c.Z, 0.12f));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(c.X, c.Y, c.Z, 0.22f));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(c.X, c.Y, c.Z, 0.32f));
            ImGui.PushStyleColor(ImGuiCol.Text, c);
        }
        ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, 1f);
        ImGui.PushStyleColor(ImGuiCol.Border, color.HasValue ? new Vector4(color.Value.X, color.Value.Y, color.Value.Z, 0.4f) : C_Border);
        bool r = ImGui.Button(label, new Vector2(-1, 34));
        ImGui.PopStyleColor(); ImGui.PopStyleVar();
        if (color.HasValue) ImGui.PopStyleColor(4);
        return r;
    }

    static Keys ScanKey()
    {
        for (int i = 1; i <= 255; i++) { var k = (Keys)i; if (k.IsKeyDown()) return k; }
        return Keys.None;
    }

    void ApplyStreamProof()
    {
        var h = this.window.Handle;
        if (_config.StreamProof) { if (!_streamProofApplied) { User32.SetWindowDisplayAffinity(h, User32.WDA_MONITOR); _streamProofApplied = true; } }
        else if (_streamProofApplied) { User32.SetWindowDisplayAffinity(h, User32.WDA_NONE); _streamProofApplied = false; }
    }

    void DrawHeadShootLine(ImDrawListPtr dl)
    {
        if (!_config.HeadShootLine || _gameData.Player == null || _gameData.Entities == null) return;
        var io = ImGui.GetIO();
        Vector2 sc = new(io.DisplaySize.X / 2, io.DisplaySize.Y / 2);
        CS2Cheat.Data.Entity.Entity? closest = null; float minD = float.MaxValue; Vector2 tHead = Vector2.Zero;
        foreach (var e in _gameData.Entities)
        {
            if (!e.IsAlive() || e.AddressBase == _gameData.Player.AddressBase) continue;
            if (_config.TeamCheck && e.Team == _gameData.Player.Team) continue;
            if (!e.BonePos.TryGetValue("head", out var hp)) continue;
            Vector3 sH = _gameData.Player.MatrixViewProjectionViewport.Transform(hp);
            if (sH.Z < 1) { float d = Vector2.Distance(new Vector2(sH.X, sH.Y), sc); if (d < minD) { minD = d; closest = e as CS2Cheat.Data.Entity.Entity; tHead = new Vector2(sH.X, sH.Y); } }
        }
        if (closest != null && minD < 400)
        {
            dl.AddLine(sc, tHead, ToColor(C_Accent), 1.2f);
            dl.AddCircle(tHead, 5f, ToColor(C_Accent), 12, 1.5f);
        }
    }

    void DrawEyeTraces(ImDrawListPtr dl)
    {
        if (_gameData.Player == null || _gameData.Entities == null) return;
        var p = _gameData.Player;
        foreach (var e in _gameData.Entities)
        {
            if (!e.IsAlive() || e.AddressBase == p.AddressBase) continue;
            if (_config.TeamCheck && e.Team == p.Team) continue;
            if (!e.BonePos.TryGetValue("head", out var hp)) continue;
            float pitch = e.ViewAngles.X.DegreeToRadian(), yaw = e.ViewAngles.Y.DegreeToRadian();
            float cp = (float)Math.Cos(pitch), sp = (float)Math.Sin(pitch), cy = (float)Math.Cos(yaw), sy = (float)Math.Sin(yaw);
            Vector3 fwd = new(cp * cy, cp * sy, -sp);
            Vector3 end = hp + fwd * 150f;
            Vector3 sS = p.MatrixViewProjectionViewport.Transform(hp), sE = p.MatrixViewProjectionViewport.Transform(end);
            if (sS.Z < 1 && sE.Z < 1)
            {
                var ca = e.Team != p.Team ? _config.EspBoxColor : _config.EspBoxColorTeam;
                uint col = ToColor(new Vector4(ca[0], ca[1], ca[2], 0.8f));
                dl.AddLine(new Vector2(sS.X, sS.Y), new Vector2(sE.X, sE.Y), col, 1.5f);
                dl.AddCircleFilled(new Vector2(sE.X, sE.Y), 2.5f, col);
            }
        }
    }

    void UpdateOverlayGeometry()
    {
        var r = _gameProcess.WindowRectangleClient;
        if (r.Width <= 0 || r.Height <= 0) return;
        try { var ts = new System.Drawing.Size(r.Width, r.Height); var tp = new System.Drawing.Point(r.X, r.Y); if (this.Size != ts) this.Size = ts; if (this.Position != tp) this.Position = tp; } catch { }
    }

    void RenderVisuals(ImDrawListPtr dl)
    {
        EspBox.Draw(dl, _gameData);
        if (_config.SkeletonEsp) SkeletonEsp.Draw(dl, _gameData);
        if (_config.EspAimCrosshair) EspAimCrosshair.Draw(dl, _gameData, _gameProcess);
        if (_config.AntiFlash) AntiFlash.Run(_gameData, _gameProcess);
        if (_config.EspGlow) EspGlow.Run(_gameData, _gameProcess);
        if (_config.Watermark) Watermark.Draw(dl);
        if (_config.BombTimer) BombTimer.Draw(dl, _gameData);
        if (_config.VoteTeller) VoteTeller.Draw(dl);
        if (_config.Radar) Radar.Draw(_gameData);
        if (_config.ItemEsp) ItemEsp.Draw(dl, _gameData, _gameProcess);
        if (_config.SpectatorList) SpectatorList.Draw(dl, _gameData, _gameProcess);
        if (_config.VelocityGraph) VelocityGraph.Draw(dl, _gameData);
        if (_config.OffscreenEnemy) OffscreenEnemy.Draw(dl, _gameData);
        if (_config.GrenadeHelper) GrenadeHelper.Draw(dl, _gameData, _gameProcess);
        if (_config.EspEyeTraces) DrawEyeTraces(dl);
        if (_config.HeadShootLine) DrawHeadShootLine(dl);
        if (_config.FreeCpu) Thread.Sleep(5);
        Hitmarker.Draw(dl);
        DamageText.Draw(dl, _gameData);
        if (_config.AimFovCircle)
        {
            var io = ImGui.GetIO(); var center = new Vector2(io.DisplaySize.X / 2, io.DisplaySize.Y / 2);
            var radius = (float)(Math.Tan((_config.AimFov * Math.PI / 180.0) / 2.0) / Math.Tan((90.0 * Math.PI / 180.0) / 2.0) * (io.DisplaySize.X / 2.0));
            dl.AddCircle(center, radius, Colors.WhiteSmoke, 64, 1.0f);
        }
    }

    void DrawActiveFeatures(ImDrawListPtr dl)
    {
        float x = 8, y = 8; int cnt = 0;
        var tc = _config.WatermarkTextRainbow ? GetRainbowColor() : ToColor(new Vector4(_config.WatermarkTextColor[0], _config.WatermarkTextColor[1], _config.WatermarkTextColor[2], _config.WatermarkTextColor[3]));
        void A(string n, bool on, string? k = null)
        {
            if (!on) return;
            dl.AddCircleFilled(new Vector2(x + 3, y + cnt * 15 + 6), 3, ToColor(new Vector4(0.15f, 0.85f, 0.40f, 0.9f)));
            dl.AddText(new Vector2(x + 10, y + cnt * 15), tc, k != null ? $"{n} [{k}]" : n);
            cnt++;
        }
        A("Aimbot", _config.AimBot, ConfigManager.GetKeyName(_config.AimBotKey));
        A("Triggerbot", _config.TriggerBot, ConfigManager.GetKeyName(_config.TriggerBotKey));
        A("BunnyHop", _config.BunnyHop, ConfigManager.GetKeyName(_config.BunnyHopKey));
        A("RCS", _config.AimRcs); A("Box ESP", _config.EspBox); A("Skeleton ESP", _config.SkeletonEsp);
        A("Name ESP", _config.EspName); A("Weapon ESP", _config.EspWeapon); A("Health Bar", _config.EspHealthBar);
        A("Armor Bar", _config.EspArmorBar); A("Head Tracker", _config.EspHeadDot); A("Snaplines", _config.EspSnaplines);
        A("Distance", _config.EspDistance); A("Flags", _config.EspFlags); A("Eye Traces", _config.EspEyeTraces);
        A("Crosshair", _config.EspAimCrosshair); A("Radar", _config.Radar); A("Item ESP", _config.ItemEsp);
        A("Anti-Flash", _config.AntiFlash); A("Glow ESP", _config.EspGlow); A("Hit Marker", _config.HitMarker);
        A("Damage Text", _config.DamageText); A("Offscreen", _config.OffscreenEnemy); A("Watermark", _config.Watermark);
        A("Velocity", _config.VelocityGraph); A("Streamproof", _config.StreamProof); A("Team Check", _config.TeamCheck);
        A("Hit Sound", _config.HitSound); A("FOV Circle", _config.AimFovCircle); A("Vote Teller", _config.VoteTeller);
        A("Grenade Helper", _config.GrenadeHelper);
    }

    public void StartFeatures() { _bombTimer.Start(); _voteTeller.Start(); _triggerBot.Start(); _aimBot.Start(); _bunnyHop.Start(); }
    public void StopFeatures() { _bombTimer.Dispose(); _voteTeller.Dispose(); _triggerBot.Dispose(); _aimBot.Dispose(); _bunnyHop.Dispose(); }

    public static uint GetRainbowColor(float speed = 1.0f)
    {
        float t = (float)Stopwatch.GetTimestamp() / Stopwatch.Frequency * speed;
        return ToColor(new Vector4((float)Math.Sin(t) * 0.5f + 0.5f, (float)Math.Sin(t + 2.094f) * 0.5f + 0.5f, (float)Math.Sin(t + 4.189f) * 0.5f + 0.5f, 1f));
    }

    public static uint ToColor(byte r, byte g, byte b, byte a = 255) => ImGui.ColorConvertFloat4ToU32(new Vector4(r / 255f, g / 255f, b / 255f, a / 255f));
    public static uint ToColor(Vector4 c) => ImGui.ColorConvertFloat4ToU32(c);

    public static class Colors
    {
        public static readonly uint White = ToColor(255, 255, 255);
        public static readonly uint Red = ToColor(255, 0, 0);
        public static readonly uint DarkRed = ToColor(139, 0, 0);
        public static readonly uint Green = ToColor(0, 255, 0);
        public static readonly uint LimeGreen = ToColor(50, 205, 50);
        public static readonly uint Blue = ToColor(0, 0, 255);
        public static readonly uint DarkBlue = ToColor(0, 0, 139);
        public static readonly uint Yellow = ToColor(255, 255, 0);
        public static readonly uint OrangeRed = ToColor(255, 69, 0);
        public static readonly uint DeepSkyBlue = ToColor(0, 191, 255);
        public static readonly uint WhiteSmoke = ToColor(245, 245, 245);
        public static readonly uint Black = ToColor(0, 0, 0);
        public static readonly uint DarkGray = ToColor(169, 169, 169);
    }
}
