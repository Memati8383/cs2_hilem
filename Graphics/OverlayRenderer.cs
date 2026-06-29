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
    private readonly Stopwatch _animTimer = Stopwatch.StartNew();
    private float _lastFrameTime;
    private readonly Dictionary<string, float> _toggleAnimState = new();
    private float _featuresStaggerStart;
    private float _menuStaggerStart;

    float StaggerAlpha(float delay)
    {
        float t = (_animTime - _menuStaggerStart) - delay;
        return Math.Clamp(t < 0f ? 0f : t / 0.3f, 0f, 1f);
    }

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

    static readonly Vector2 MenuSize = new(1050, 660);
    const float SidebarW = 180f;
    const float HeaderH = 56f;
    const float BottomH = 30f;

    // ─── Clean Dark Theme ───
    static readonly Vector4 C_Bg = new(0.07f, 0.07f, 0.09f, 1f);
    static readonly Vector4 C_Sidebar = new(0.09f, 0.09f, 0.11f, 1f);
    static readonly Vector4 C_Card = new(0.11f, 0.11f, 0.14f, 1f);
    static readonly Vector4 C_Accent = new(0.30f, 0.55f, 1f, 1f);
    static readonly Vector4 C_Accent2 = new(0.65f, 0.40f, 1f, 1f);
    static readonly Vector4 C_AccentDim = new(0.20f, 0.40f, 0.80f, 1f);
    static readonly Vector4 C_Text = new(0.90f, 0.92f, 0.95f, 1f);
    static readonly Vector4 C_TextSub = new(0.58f, 0.60f, 0.68f, 1f);
    static readonly Vector4 C_TextMuted = new(0.35f, 0.37f, 0.45f, 1f);
    static readonly Vector4 C_Border = new(0.16f, 0.16f, 0.20f, 1f);
    static readonly Vector4 C_TogBg = new(0.12f, 0.12f, 0.16f, 1f);
    static readonly Vector4 C_Red = new(1f, 0.30f, 0.35f, 1f);
    static readonly Vector4 C_Yellow = new(1f, 0.80f, 0.20f, 1f);
    static readonly Vector4 C_Green = new(0.20f, 0.80f, 0.50f, 1f);

    string[][] TabLabels() => new[] {
        new[] { "\uE871", Language.Get("tab_dashboard") },
        new[] { "\uEA28", Language.Get("tab_aimbot") },
        new[] { "\uE8F4", Language.Get("tab_visuals") },
        new[] { "\uE429", Language.Get("tab_misc") },
        new[] { "\uE8B8", Language.Get("tab_settings") },
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
        _targetTabY = 70f;
        _currentTabY = 70f;
    }

    protected override async Task PostInitialized()
    {
        var h = this.window.Handle;
        var ex = User32.GetWindowLong(h, User32.GWL_EXSTYLE);
        User32.SetWindowLong(h, User32.GWL_EXSTYLE, ex | User32.WS_EX_NOACTIVATE | User32.WS_EX_TOOLWINDOW);
        UpdateOverlayGeometry();
        string? iconPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "MaterialIcons-Regular.ttf");
        string? brandPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "fa-brands-400.ttf");
        if (!System.IO.File.Exists(iconPath))
        {
            try
            {
                using var hc = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(5) };
                var data = await hc.GetByteArrayAsync("https://raw.githubusercontent.com/google/material-design-icons/master/font/MaterialIcons-Regular.ttf");
                System.IO.File.WriteAllBytes(iconPath, data);
            }
            catch { iconPath = null; }
        }
        if (!System.IO.File.Exists(brandPath))
        {
            try
            {
                using var hc = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(5) };
                var data = await hc.GetByteArrayAsync("https://cdn.jsdelivr.net/npm/@fortawesome/fontawesome-free/webfonts/fa-brands-400.ttf");
                System.IO.File.WriteAllBytes(brandPath, data);
            }
            catch { brandPath = null; }
        }
        LoadFonts(iconPath, brandPath);
    }
    unsafe void LoadFonts(string? iconPath, string? brandPath)
    {
        ushort[] trRange = { 0x0020, 0x00FF, 0x011E, 0x011F, 0x0130, 0x0131, 0x015E, 0x015F, 0 };
        ushort[] iconRange = { 0xE000, 0xF8FF, 0 };
        ushort[] brandRange = { 0xF09B, 0xF09B, 0xF16D, 0xF16D, 0 };
        string fontPath = @"C:\Windows\Fonts\segoeui.ttf";
        if (!System.IO.File.Exists(fontPath)) fontPath = @"C:\Windows\Fonts\tahoma.ttf";
        if (!System.IO.File.Exists(fontPath)) fontPath = @"C:\Windows\Fonts\verdanab.ttf";
        bool hasIcon = iconPath != null && System.IO.File.Exists(iconPath);
        bool hasBrand = brandPath != null && System.IO.File.Exists(brandPath);
        if (hasIcon || hasBrand)
        {
            ReplaceFont(config =>
            {
                var io = ImGui.GetIO();
                fixed (ushort* p = trRange) io.Fonts.AddFontFromFileTTF(fontPath, 15, config, new IntPtr(p));
                if (hasIcon)
                {
                    config->MergeMode = 1;
                    fixed (ushort* p2 = iconRange) io.Fonts.AddFontFromFileTTF(iconPath, 15, config, new IntPtr(p2));
                }
                if (hasBrand)
                {
                    config->MergeMode = 1;
                    fixed (ushort* p3 = brandRange) io.Fonts.AddFontFromFileTTF(brandPath, 15, config, new IntPtr(p3));
                }
                config->MergeMode = 0;
                ImGuiNative.igGetIO()->FontDefault = null;
            });
        }
        else
        {
            try { ReplaceFont(fontPath, 15, trRange); } catch { }
        }
    }

    private void LogCrash(string msg)
    {
        try { System.IO.File.AppendAllText(
            System.IO.Path.Combine(System.IO.Path.GetTempPath(), "MematiHack", "crash.log"),
            $"[{DateTime.Now:HH:mm:ss}] {msg}\n");
        } catch { }
    }

    protected override void Render()
    {
        try {
        if (Keys.F6.IsKeyDown()) Environment.Exit(0);
        UpdateOverlayGeometry();
        if (!_gameProcess.IsValid) return;
        _config = ConfigManager.Load();
        ApplyStreamProof();
        if (_firstFrame) { _menuStaggerStart = _animTime; }

        var mk = _config.MenuToggleKey;
        var mkd = mk.IsKeyDown();
        if (mkd && !_menuKeyWasDown) { _showMenu = !_showMenu; if (_showMenu) _menuStaggerStart = _animTime; }
        var rShift = Keys.RShiftKey.IsKeyDown();
        if (rShift && !_rShiftWasDown) { _showMenu = !_showMenu; if (_showMenu) _menuStaggerStart = _animTime; }
        _rShiftWasDown = rShift;
        var end = Keys.End.IsKeyDown();
        if (end && !_endWasDown && _showMenu) { _showMenu = false; ConfigManager.Save(_config); }
        _endWasDown = end;

        float targetAlpha = _showMenu ? 1f : 0f;
        _menuAlpha += (targetAlpha - _menuAlpha) * (_showMenu ? 0.10f : 0.35f);
        if (_menuAlpha < 0.01f) _menuAlpha = 0f; if (_menuAlpha > 0.99f) _menuAlpha = 1f;

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
        } catch (Exception ex) {
            LogCrash($"Render hatasi: {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}");
        }
    }

    void ApplyStyle()
    {
        if (_styleApplied) return;
        var s = ImGui.GetStyle();
        s.WindowRounding = 12f; s.ChildRounding = 8f; s.FrameRounding = 6f;
        s.PopupRounding = 8f; s.GrabRounding = 6f; s.ScrollbarRounding = 8f;
        s.WindowBorderSize = 0f; s.FrameBorderSize = 0f; s.ChildBorderSize = 1f;
        s.ItemSpacing = new Vector2(6, 4);
        s.WindowPadding = Vector2.Zero;
        s.FramePadding = new Vector2(8, 5);
        s.ScrollbarSize = 4;
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
        s.TabRounding = 6f;
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
        ImGui.SetNextWindowPos(pos, ImGuiCond.Always);
        ImGui.SetNextWindowSize(MenuSize, ImGuiCond.Always);
        ImGui.Begin("##main", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoTitleBar);

        var p = ImGui.GetWindowPos();
        var dl = ImGui.GetWindowDrawList();

        float sgSb = StaggerAlpha(0f);
        float sgStatus = StaggerAlpha(0.06f);
        float sgHeader = StaggerAlpha(0.10f);
        float sgContent = StaggerAlpha(0.15f);
        float sgFooter = StaggerAlpha(0.12f);

        // ── Background ──
        dl.AddRectFilled(p, p + MenuSize, ToColor(C_Bg), 12f);
        dl.AddRect(p, p + MenuSize, ToColor(new Vector4(C_Border.X, C_Border.Y, C_Border.Z, _menuAlpha)), 12f, ImDrawFlags.None, 1f);

        // ── Sidebar ──
        dl.AddRectFilled(p, p + new Vector2(SidebarW, MenuSize.Y), ToColor(new Vector4(C_Sidebar.X, C_Sidebar.Y, C_Sidebar.Z, C_Sidebar.W * sgSb)), 12f, ImDrawFlags.RoundCornersLeft);
        dl.AddLine(p + new Vector2(SidebarW, 0), p + new Vector2(SidebarW, MenuSize.Y),
            ToColor(new Vector4(C_Border.X, C_Border.Y, C_Border.Z, _menuAlpha)), 1f);

        // ── Header Branding ──
        dl.AddText(null, 20f, p + new Vector2(18, 16), ToColor(new Vector4(C_Text.X, C_Text.Y, C_Text.Z, C_Text.W * sgSb)), Language.Get("sidebar_title"));
        dl.AddText(null, 9f, p + new Vector2(19, 38), ToColor(new Vector4(C_TextMuted.X, C_TextMuted.Y, C_TextMuted.Z, C_TextMuted.W * sgSb)), Language.Get("sidebar_subtitle"));

        // ── Tab Navigation ──
        float tabStartY = 70f;
        float tabH = 40f;
        float tabGap = 2f;

        _targetTabY = tabStartY + (_activeTab * (tabH + tabGap));
        _currentTabY = Lerp(_currentTabY, _targetTabY, 0.12f);

        float tabPillStagger = StaggerAlpha(0.04f);
        dl.AddRectFilled(p + new Vector2(12, _currentTabY), p + new Vector2(SidebarW - 8, _currentTabY + tabH),
            ToColor(new Vector4(C_Accent.X, C_Accent.Y, C_Accent.Z, 0.10f * tabPillStagger)), 6f);
        dl.AddRectFilled(p + new Vector2(8, _currentTabY + 6), p + new Vector2(10, _currentTabY + tabH - 6),
            ToColor(new Vector4(C_Accent.X, C_Accent.Y, C_Accent.Z, tabPillStagger)), 2f);

        float ty = tabStartY;
        var tabLabels = TabLabels();
        for (int i = 0; i < tabLabels.Length; i++)
        {
            float sgTab = StaggerAlpha(0.08f + i * 0.05f);
            bool act = _activeTab == i;
            var tPos = p + new Vector2(0, ty);
            var tSize = new Vector2(SidebarW, tabH);
            bool hov = io.MousePos.X >= tPos.X && io.MousePos.X <= tPos.X + tSize.X &&
                       io.MousePos.Y >= tPos.Y && io.MousePos.Y <= tPos.Y + tSize.Y;
            if (!_tabHoverAnims.ContainsKey(i)) _tabHoverAnims[i] = 0f;
            _tabHoverAnims[i] = Lerp(_tabHoverAnims[i], hov && !act ? 1f : 0f, 0.14f);

            if (_tabHoverAnims[i] > 0.01f)
                dl.AddRectFilled(tPos + new Vector2(14, 2), tPos + new Vector2(SidebarW - 14, tabH - 2),
                    ToColor(new Vector4(1, 1, 1, 0.03f * _tabHoverAnims[i] * sgTab)), 6f);

            ImGui.SetCursorPos(new Vector2(0, ty));
            ImGui.InvisibleButton($"##tab_{i}", tSize);
            if (ImGui.IsItemClicked()) { _previousTab = _activeTab; _activeTab = i; _contentFadeAlpha = 0f; }

            var iCol = act ? ToColor(new Vector4(C_Accent.X, C_Accent.Y, C_Accent.Z, C_Accent.W * sgTab)) : ToColor(new Vector4(C_TextMuted.X, C_TextMuted.Y, C_TextMuted.Z, C_TextMuted.W * sgTab));
            dl.AddText(null, 12f, tPos + new Vector2(22, (tabH - 12) * 0.5f), iCol, tabLabels[i][0]);
            dl.AddText(null, 11f, tPos + new Vector2(46, (tabH - 11) * 0.5f), act ? ToColor(new Vector4(C_Text.X, C_Text.Y, C_Text.Z, C_Text.W * sgTab)) : ToColor(new Vector4(C_TextSub.X, C_TextSub.Y, C_TextSub.Z, C_TextSub.W * sgTab)), tabLabels[i][1]);
            ty += tabH + tabGap;
        }

        // ── Status Indicator ──
        dl.AddCircleFilled(p + new Vector2(18, MenuSize.Y - 22), 3f, ToColor(new Vector4(C_Green.X, C_Green.Y, C_Green.Z, C_Green.W * sgStatus)));
        dl.AddText(null, 9f, p + new Vector2(27, MenuSize.Y - 26), ToColor(new Vector4(C_TextSub.X, C_TextSub.Y, C_TextSub.Z, C_TextSub.W * sgStatus)), "\uE894 " + Language.Get("sidebar_connected"));

        // ── Content Header ──
        float hx = p.X + SidebarW;
        float hw = MenuSize.X - SidebarW;
        dl.AddRectFilled(new Vector2(hx, p.Y), new Vector2(hx + hw, p.Y + HeaderH),
            ToColor(new Vector4(C_Sidebar.X, C_Sidebar.Y, C_Sidebar.Z, 0.5f * sgHeader)));
        dl.AddLine(new Vector2(hx, p.Y + HeaderH), new Vector2(hx + hw, p.Y + HeaderH),
            ToColor(new Vector4(C_Border.X, C_Border.Y, C_Border.Z, _menuAlpha * sgHeader)));
        dl.AddText(null, 15f, new Vector2(hx + 20, p.Y + 18), ToColor(new Vector4(C_Text.X, C_Text.Y, C_Text.Z, C_Text.W * sgHeader)), TabLabels()[_activeTab][1]);
        dl.AddText(null, 9f, new Vector2(hx + 20, p.Y + 36), ToColor(new Vector4(C_TextMuted.X, C_TextMuted.Y, C_TextMuted.Z, C_TextMuted.W * sgHeader)), Language.Get("header_subtitle"));

        // ── Content Viewport ──
        float cx = SidebarW + 12f;
        float cy = HeaderH + 8f;
        float cw = MenuSize.X - SidebarW - 24f;
        float ch = MenuSize.Y - HeaderH - BottomH - 14f;
        ImGui.SetCursorPos(new Vector2(cx, cy));
        ImGui.PushStyleColor(ImGuiCol.ChildBg, C_Card);
        ImGui.BeginChild("##content_vp", new Vector2(cw, ch), ImGuiChildFlags.None, ImGuiWindowFlags.AlwaysVerticalScrollbar);

        if (_previousTab != _activeTab) { _contentFadeAlpha = Math.Min(_contentFadeAlpha + 0.06f, 1f); if (_contentFadeAlpha >= 1f) _previousTab = _activeTab; }
        else _contentFadeAlpha = 1f;

        ImGui.PushStyleVar(ImGuiStyleVar.Alpha, _contentFadeAlpha * (sgContent > 0.01f ? sgContent : 0.01f));
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(6, 4));
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(10, 10));
        try {
        switch (_activeTab) { case 0: TabDashboard(); break; case 1: TabAimbot(); break; case 2: TabVisuals(); break; case 3: TabMisc(); break; case 4: TabSettings(); break; }
        } catch (Exception ex_tab) { LogCrash($"Tab[{_activeTab}] hatasi: {ex_tab.GetType().Name}: {ex_tab.Message}"); }
        ImGui.PopStyleVar(3);
        ImGui.EndChild();
        ImGui.PopStyleColor();

        // ── Footer ──
        float by = MenuSize.Y - BottomH;
        dl.AddRectFilled(new Vector2(hx, p.Y + by), new Vector2(hx + hw, p.Y + by + BottomH), ToColor(new Vector4(C_Sidebar.X, C_Sidebar.Y, C_Sidebar.Z, C_Sidebar.W * sgFooter)), 12f, ImDrawFlags.RoundCornersBottomRight);
        dl.AddLine(new Vector2(hx, p.Y + by), new Vector2(hx + hw, p.Y + by), ToColor(new Vector4(C_Border.X, C_Border.Y, C_Border.Z, _menuAlpha * sgFooter)));
        dl.AddText(null, 9f, new Vector2(hx + 16, p.Y + by + (BottomH - 9) * 0.5f), ToColor(new Vector4(C_TextMuted.X, C_TextMuted.Y, C_TextMuted.Z, C_TextMuted.W * sgFooter)),
            $"{_currentFps:0} {Language.Get("card_fps")}  |  {_gameProcess.WindowRectangleClient.Width}x{_gameProcess.WindowRectangleClient.Height}");

        ImGui.End();
        ImGui.PopStyleVar();
    }

    // ═══════════════════════════════════════════
    //  TAB CONTENT
    // ═══════════════════════════════════════════
    void TabDashboard()
    {
        float aw = ImGui.GetContentRegionAvail().X;

        // ── Welcome header ──
        ImGui.TextColored(C_Accent, "\uE8B8 " + Language.Get("dashboard_title"));
        ImGui.TextColored(C_TextSub, Language.Get("dashboard_subtitle"));
        ImGui.Spacing();

        // ── Status cards row ──
        float cardW = (aw - 8f) / 3f;
        for (int i = 0; i < 3; i++)
        {
            if (i > 0) ImGui.SameLine(0, 4);
            ImGui.BeginChild($"##dsc{i}", new Vector2(cardW, 64), ImGuiChildFlags.None, ImGuiWindowFlags.None);
            var p = ImGui.GetCursorScreenPos();
            var dl = ImGui.GetWindowDrawList();
            dl.AddRectFilled(p, p + new Vector2(cardW, 64), ToColor(new Vector4(C_Card.X, C_Card.Y, C_Card.Z, 0.7f)), 8f);
            dl.AddRect(p, p + new Vector2(cardW, 64), ToColor(new Vector4(C_Border.X, C_Border.Y, C_Border.Z, 0.5f)), 8f, ImDrawFlags.None, 1f);
            string[] labels = { "\uE87C " + Language.Get("card_status"), "\uE1DB " + Language.Get("card_fps"), "\uE8F4 " + Language.Get("card_features") };
            string[] values = { Language.Get("card_active"), $"{_currentFps:0}", $"{CountActiveFeatures()}" };
            Vector4[] colors = { C_Green, C_Accent, C_Yellow };
            dl.AddText(null, 9f, p + new Vector2(12, 8), ToColor(C_TextMuted), labels[i]);
            dl.AddText(null, 18f, p + new Vector2(12, 26), ToColor(colors[i]), values[i]);
            dl.AddCircleFilled(p + new Vector2(cardW - 16, 16), 4f, ToColor(colors[i]));
            ImGui.Dummy(new Vector2(cardW, 64));
            ImGui.EndChild();
        }

        ImGui.Spacing();

        // ── Active features section ──
        ImGui.TextColored(C_TextSub, Language.Get("features_header"));
        ImGui.Spacing();
        float halfW = (aw - 4f) / 2f;
        var allFeatures = new (string Name, bool Active)[]
        {
            (Language.Get("feature_aimbot"), _config.AimBot), (Language.Get("feature_triggerbot"), _config.TriggerBot), (Language.Get("feature_bunnyhop"), _config.BunnyHop),
            (Language.Get("feature_box_esp"), _config.EspBox), (Language.Get("feature_name_esp"), _config.EspName), (Language.Get("feature_health_bar"), _config.EspHealthBar),
            (Language.Get("feature_armor_bar"), _config.EspArmorBar), (Language.Get("feature_distance"), _config.EspDistance), (Language.Get("feature_weapon_esp"), _config.EspWeapon),
            (Language.Get("checkbox_money"), _config.EspMoney), (Language.Get("checkbox_ammo"), _config.EspAmmo), (Language.Get("checkbox_skeleton"), _config.SkeletonEsp),
            (Language.Get("checkbox_snaplines"), _config.EspSnaplines), (Language.Get("checkbox_visible_only"), _config.EspSpottedOnly), (Language.Get("checkbox_crosshair"), _config.EspAimCrosshair),
            (Language.Get("checkbox_flash_protection"), _config.AntiFlash), (Language.Get("checkbox_glow_esp"), _config.EspGlow), (Language.Get("checkbox_offscreen_arrows"), _config.OffscreenEnemy),
            (Language.Get("checkbox_radar"), _config.Radar), (Language.Get("checkbox_item_esp"), _config.ItemEsp), (Language.Get("checkbox_bomb_timer"), _config.BombTimer),
            (Language.Get("checkbox_spectator_list"), _config.SpectatorList), (Language.Get("checkbox_hitmarker"), _config.HitMarker), (Language.Get("checkbox_hit_sound"), _config.HitSound),
            (Language.Get("checkbox_damage_text"), _config.DamageText), (Language.Get("feature_eye_traces"), _config.EspEyeTraces), (Language.Get("feature_head_tracker"), _config.HeadShootLine),
            (Language.Get("checkbox_vote_teller"), _config.VoteTeller), (Language.Get("checkbox_team_check"), _config.TeamCheck), (Language.Get("feature_watermark"), _config.Watermark),
            (Language.Get("checkbox_stream_proof"), _config.StreamProof), (Language.Get("checkbox_cpu_optimize"), _config.FreeCpu), (Language.Get("checkbox_velocity_graph"), _config.VelocityGraph),
            (Language.Get("feature_grenade_helper"), _config.GrenadeHelper), (Language.Get("checkbox_dynamic_fov"), _config.AimDynamicFov), (Language.Get("checkbox_fov_circle"), _config.AimFovCircle),
            (Language.Get("feature_rcs"), _config.AimRcs), (Language.Get("feature_flags"), _config.EspFlags), (Language.Get("feature_head_dot"), _config.EspHeadDot),
            (Language.Get("feature_box_esp"), _config.EspBoxCorner), (Language.Get("feature_ping"), _config.EspPing), (Language.Get("feature_reloading"), _config.EspReloading),
            (Language.Get("feature_defusing"), _config.EspDefusing), (Language.Get("feature_weapon_icon"), _config.EspWeaponIcon),
        };
        for (int i = 0; i < allFeatures.Length; i++)
        {
            if (i % 2 == 1) ImGui.SameLine(0, 4);
            var f = allFeatures[i];
            ImGui.BeginChild($"##df{i}", new Vector2(halfW, 26), ImGuiChildFlags.None, ImGuiWindowFlags.None);
            var pp = ImGui.GetCursorScreenPos();
            var dll = ImGui.GetWindowDrawList();
            dll.AddRectFilled(pp, pp + new Vector2(halfW, 26),
                ToColor(f.Active ? new Vector4(C_Accent.X, C_Accent.Y, C_Accent.Z, 0.08f) : new Vector4(0, 0, 0, 0.15f)), 6f);
            dll.AddCircleFilled(pp + new Vector2(12, 13), 4f, ToColor(f.Active ? C_Green : C_TextMuted));
            dll.AddText(null, 12f, pp + new Vector2(24, 7), ToColor(f.Active ? C_Text : C_TextMuted), f.Name);
            ImGui.Dummy(new Vector2(halfW, 26));
            ImGui.EndChild();
        }

        ImGui.Spacing();
        ImGui.Spacing();

        // ── Quick actions ──
        ImGui.TextColored(C_TextSub, Language.Get("quick_actions"));
        ImGui.Spacing();
        float btnW = (aw - 8f) / 3f;
        void SocialBtn(string label, Vector4 col, string url)
        {
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(col.X, col.Y, col.Z, 0.12f));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(col.X, col.Y, col.Z, 0.22f));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(col.X, col.Y, col.Z, 0.32f));
            ImGui.PushStyleColor(ImGuiCol.Text, col);
            ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, 1f);
            ImGui.PushStyleColor(ImGuiCol.Border, new Vector4(col.X, col.Y, col.Z, 0.4f));
            if (ImGui.Button(label, new Vector2(btnW, 34)))
                try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true }); } catch { }
            ImGui.PopStyleColor(4 + 1);
            ImGui.PopStyleVar();
        }
        SocialBtn("\uF16D " + Language.Get("social_instagram") + " ", new Vector4(0.88f, 0.25f, 0.44f, 1f), "https://www.instagram.com/ferit22901/");
        ImGui.SameLine(0, 4);
        SocialBtn("\uF09B " + Language.Get("social_github") + " ", new Vector4(0.35f, 0.35f, 0.40f, 1f), "https://github.com/Memati8383");
        ImGui.SameLine(0, 4);
        SocialBtn("\uE896 " + Language.Get("social_source") + " ", new Vector4(0.15f, 0.65f, 1f, 1f), "https://github.com/Memati8383/cs2_hilem");
    }

    int CountActiveFeatures()
    {
        int c = 0;
        if (_config.AimBot) c++; if (_config.TriggerBot) c++; if (_config.BunnyHop) c++;
        if (_config.EspBox) c++; if (_config.EspName) c++; if (_config.EspHealthBar) c++;
        if (_config.EspArmorBar) c++; if (_config.EspDistance) c++; if (_config.EspWeapon) c++;
        if (_config.EspMoney) c++; if (_config.EspAmmo) c++; if (_config.SkeletonEsp) c++;
        if (_config.EspSnaplines) c++; if (_config.EspSpottedOnly) c++; if (_config.EspAimCrosshair) c++;
        if (_config.AntiFlash) c++; if (_config.EspGlow) c++; if (_config.OffscreenEnemy) c++;
        if (_config.Radar) c++; if (_config.ItemEsp) c++; if (_config.BombTimer) c++;
        if (_config.SpectatorList) c++; if (_config.HitMarker) c++; if (_config.HitSound) c++;
        if (_config.DamageText) c++; if (_config.EspEyeTraces) c++; if (_config.HeadShootLine) c++;
        if (_config.VoteTeller) c++; if (_config.TeamCheck) c++; if (_config.Watermark) c++;
        if (_config.StreamProof) c++; if (_config.FreeCpu) c++; if (_config.VelocityGraph) c++;
        if (_config.GrenadeHelper) c++; if (_config.AimDynamicFov) c++; if (_config.AimFovCircle) c++;
        if (_config.AimRcs) c++; if (_config.EspFlags) c++; if (_config.EspHeadDot) c++;
        if (_config.EspBoxCorner) c++; if (_config.EspPing) c++; if (_config.EspReloading) c++;
        if (_config.EspDefusing) c++; if (_config.EspWeaponIcon) c++;
        return c;
    }

    void TabAimbot()
    {
        if (ImGui.BeginTabBar("##aim_tabs"))
        {
            if (ImGui.BeginTabItem("\uE88E " + Language.Get("aimbot_help")))
            {
                bool b = _config.AimBot; if (ImGui.Checkbox("\uEA28 " + Language.Get("checkbox_aimbot") + "##aim", ref b)) { _config.AimBot = b; ConfigManager.UpdateCache(_config); }
                b = _config.AimFovCircle; if (ImGui.Checkbox("\uE430 " + Language.Get("checkbox_fov_circle"), ref b)) { _config.AimFovCircle = b; ConfigManager.UpdateCache(_config); }
                b = _config.AimDynamicFov; if (ImGui.Checkbox("\uE3A0 " + Language.Get("checkbox_dynamic_fov"), ref b)) { _config.AimDynamicFov = b; ConfigManager.UpdateCache(_config); }
                ImGui.EndTabItem();
            }
            if (ImGui.BeginTabItem("\uE8B8 " + Language.Get("aimbot_settings")))
            {
                float fv = _config.AimFov; if (Slider("\uE430 " + Language.Get("slider_fov"), "##fov", ref fv, 1f, 60f, "%.1f")) _config.AimFov = fv;
                fv = _config.AimSmoothing; if (Slider("\uE8BA " + Language.Get("slider_smoothing"), "##sm", ref fv, 1f, 30f, "%.1f")) _config.AimSmoothing = fv;
                int bone = _config.AimBoneIndex; if (Combo("\uE0D0 " + Language.Get("combo_bone"), "##bone", ref bone, ConfigManager.BoneDisplayNames)) _config.AimBoneIndex = bone;
                ImGui.EndTabItem();
            }
            if (ImGui.BeginTabItem("\uE3E7 " + Language.Get("aimbot_recoil")))
            {
                bool b = _config.AimRcs; if (ImGui.Checkbox("\uE3E7 " + Language.Get("checkbox_rcs"), ref b)) { _config.AimRcs = b; ConfigManager.UpdateCache(_config); }
                if (_config.AimRcs) { float fv = _config.AimRcsStrength; if (Slider("\uE3A0 " + Language.Get("slider_rcs_strength"), "##rcs", ref fv, 0f, 100f, "%.0f%%")) _config.AimRcsStrength = fv; }
                ImGui.EndTabItem();
            }
            ImGui.EndTabBar();
        }
    }

    void TabVisuals()
    {
        if (ImGui.BeginTabBar("##vis_tabs"))
        {
            if (ImGui.BeginTabItem("\uE7FD " + Language.Get("visuals_player")))
            {
                bool b = _config.EspBox; if (ImGui.Checkbox("\uE8F4 " + Language.Get("checkbox_box_esp"), ref b)) { _config.EspBox = b; ConfigManager.UpdateCache(_config); }
                b = _config.EspName; if (ImGui.Checkbox("\uE84D " + Language.Get("checkbox_name_esp"), ref b)) { _config.EspName = b; ConfigManager.UpdateCache(_config); }
                b = _config.EspHealthBar; if (ImGui.Checkbox("\uE004 " + Language.Get("checkbox_health_bar"), ref b)) { _config.EspHealthBar = b; ConfigManager.UpdateCache(_config); }
                b = _config.EspArmorBar; if (ImGui.Checkbox("\uE31A " + Language.Get("checkbox_armor_bar"), ref b)) { _config.EspArmorBar = b; ConfigManager.UpdateCache(_config); }
                b = _config.EspDistance; if (ImGui.Checkbox("\uE893 " + Language.Get("checkbox_distance"), ref b)) { _config.EspDistance = b; ConfigManager.UpdateCache(_config); }
                b = _config.EspWeapon; if (ImGui.Checkbox("\uE8E8 " + Language.Get("checkbox_weapon"), ref b)) { _config.EspWeapon = b; ConfigManager.UpdateCache(_config); }
                b = _config.EspMoney; if (ImGui.Checkbox("\uE263 " + Language.Get("checkbox_money"), ref b)) { _config.EspMoney = b; ConfigManager.UpdateCache(_config); }
                b = _config.EspAmmo; if (ImGui.Checkbox("\uE3E7 " + Language.Get("checkbox_ammo"), ref b)) { _config.EspAmmo = b; ConfigManager.UpdateCache(_config); }
                b = _config.SkeletonEsp; if (ImGui.Checkbox("\uE0D0 " + Language.Get("checkbox_skeleton"), ref b)) { _config.SkeletonEsp = b; ConfigManager.UpdateCache(_config); }
                b = _config.EspSnaplines; if (ImGui.Checkbox("\uE0CD " + Language.Get("checkbox_snaplines"), ref b)) { _config.EspSnaplines = b; ConfigManager.UpdateCache(_config); }
                b = _config.EspSpottedOnly; if (ImGui.Checkbox("\uE8F4 " + Language.Get("checkbox_visible_only"), ref b)) { _config.EspSpottedOnly = b; ConfigManager.UpdateCache(_config); }
                ImGui.EndTabItem();
            }
            if (ImGui.BeginTabItem("\uE80B " + Language.Get("visuals_world")))
            {
                bool b = _config.EspAimCrosshair; if (ImGui.Checkbox("\uE3B7 " + Language.Get("checkbox_crosshair"), ref b)) { _config.EspAimCrosshair = b; ConfigManager.UpdateCache(_config); }
                b = _config.AntiFlash; if (ImGui.Checkbox("\uE663 " + Language.Get("checkbox_flash_protection"), ref b)) { _config.AntiFlash = b; ConfigManager.UpdateCache(_config); }
                b = _config.EspGlow; if (ImGui.Checkbox("\uE3E7 " + Language.Get("checkbox_glow_esp"), ref b)) { _config.EspGlow = b; ConfigManager.UpdateCache(_config); }
                if (_config.EspGlow)
                {
                    b = _config.GlowHealthBased;
                    if (ImGui.Checkbox("\uE004 " + Language.Get("checkbox_health_based"), ref b)) { _config.GlowHealthBased = b; ConfigManager.UpdateCache(_config); }
                    int style = _config.GlowStyle; if (Combo("\uE3B7 " + Language.Get("combo_glow_style"), "##gs", ref style, new[] { Language.Get("glow_default"), Language.Get("glow_pulse"), Language.Get("glow_outline"), Language.Get("glow_solid") })) _config.GlowStyle = style;
                    if (!_config.GlowHealthBased) { ColorPick("\uE3B7 " + Language.Get("color_enemy_glow"), "##ge", _config.GlowColorEnemy); ColorPick("\uE3B7 " + Language.Get("color_team_glow"), "##gt", _config.GlowColorTeam); }
                }
                b = _config.OffscreenEnemy; if (ImGui.Checkbox("\uE5E1 " + Language.Get("checkbox_offscreen_arrows"), ref b)) { _config.OffscreenEnemy = b; ConfigManager.UpdateCache(_config); }
                b = _config.Radar; if (ImGui.Checkbox("\uE80B " + Language.Get("checkbox_radar"), ref b)) { _config.Radar = b; ConfigManager.UpdateCache(_config); }
                if (_config.Radar) { float rr = _config.RadarRange; if (Slider("\uE80B " + Language.Get("slider_radar_zoom"), "##rz", ref rr, 0.01f, 0.2f, "%.3f")) _config.RadarRange = rr; }
                b = _config.ItemEsp; if (ImGui.Checkbox("\uE2C7 " + Language.Get("checkbox_item_esp"), ref b)) { _config.ItemEsp = b; ConfigManager.UpdateCache(_config); }
                b = _config.BombTimer; if (ImGui.Checkbox("\uE425 " + Language.Get("checkbox_bomb_timer"), ref b)) { _config.BombTimer = b; ConfigManager.UpdateCache(_config); }
                b = _config.SpectatorList; if (ImGui.Checkbox("\uE7FB " + Language.Get("checkbox_spectator_list"), ref b)) { _config.SpectatorList = b; ConfigManager.UpdateCache(_config); }
                ImGui.EndTabItem();
            }
            if (ImGui.BeginTabItem("\uE663 " + Language.Get("visuals_effects")))
            {
                bool b = _config.HitMarker; if (ImGui.Checkbox("\uE3B7 " + Language.Get("checkbox_hitmarker"), ref b)) { _config.HitMarker = b; ConfigManager.UpdateCache(_config); }
                b = _config.HitSound; if (ImGui.Checkbox("\uE050 " + Language.Get("checkbox_hit_sound"), ref b)) { _config.HitSound = b; ConfigManager.UpdateCache(_config); }
                if (_config.HitSound)
                {
                    float hsv = _config.HitSoundVolume; if (Slider("\uE050 " + Language.Get("slider_volume"), "##hsv", ref hsv, 0f, 1f, "%.2f")) _config.HitSoundVolume = hsv;
                    var snds = HitSound.GetAvailableSounds(); var idx = Array.IndexOf(snds, _config.HitSoundName); if (idx < 0) idx = 0; int sel = idx;
                    if (Combo("\uE050 " + Language.Get("combo_hit_sound"), "##hs", ref sel, snds)) { _config.HitSoundName = snds[sel]; HitSound.Play(); }
                    if (Button("\uE037 " + Language.Get("btn_test_sound"), C_Accent)) HitSound.Play();
                }
                if (_config.HitMarker)
                {
                    float hms = _config.HitMarkerSize; if (Slider("\uE8B5 " + Language.Get("slider_size"), "##hms", ref hms, 1f, 30f, "%.0f")) _config.HitMarkerSize = hms;
                    hms = _config.HitMarkerGap; if (Slider("\uE893 " + Language.Get("slider_gap"), "##hmg", ref hms, 0f, 10f, "%.0f")) _config.HitMarkerGap = hms;
                    hms = _config.HitMarkerDuration; if (Slider("\uE425 " + Language.Get("slider_duration_ms"), "##hmd", ref hms, 100f, 2000f, "%.0f")) _config.HitMarkerDuration = hms;
                    hms = _config.HitMarkerThickness; if (Slider("\uE8D0 " + Language.Get("slider_thickness"), "##hmt", ref hms, 0.5f, 5f, "%.1f")) _config.HitMarkerThickness = hms;
                    ColorPick("\uE3B7 " + Language.Get("color_pick"), "##hmc", _config.HitMarkerColor);
                }
                b = _config.DamageText; if (ImGui.Checkbox("\uE3A0 " + Language.Get("checkbox_damage_text"), ref b)) { _config.DamageText = b; ConfigManager.UpdateCache(_config); }
                if (_config.DamageText)
                {
                    float dts = _config.DamageTextSize; if (Slider("\uE8B5 " + Language.Get("slider_text_size"), "##dts", ref dts, 10f, 40f, "%.0f")) _config.DamageTextSize = dts;
                    dts = _config.DamageTextDuration; if (Slider("\uE425 " + Language.Get("slider_duration_ms"), "##dtd", ref dts, 500f, 3000f, "%.0f")) _config.DamageTextDuration = dts;
                    ColorPick("\uE3B7 " + Language.Get("color_text_color"), "##dtc", _config.DamageTextColor);
                }
                ImGui.EndTabItem();
            }
            if (ImGui.BeginTabItem("\uE3B7 " + Language.Get("visuals_colors")))
            {
                ColorPick("\uE7FD " + Language.Get("color_enemy_box"), "##ec", _config.EspBoxColor); ColorPick("\uE7FD " + Language.Get("color_team_box"), "##tc", _config.EspBoxColorTeam);
                ColorPick("\uE84D " + Language.Get("color_esp_text"), "##etc", _config.EspTextColor);
                bool b = _config.EspTextRainbow; if (ImGui.Checkbox("\uE8F4 " + Language.Get("checkbox_esp_rainbow"), ref b)) { _config.EspTextRainbow = b; ConfigManager.UpdateCache(_config); }
                ColorPick("\uE8F4 " + Language.Get("color_watermark"), "##wmc", _config.WatermarkTextColor);
                b = _config.WatermarkTextRainbow; if (ImGui.Checkbox("\uE8F4 " + Language.Get("checkbox_watermark_rainbow"), ref b)) { _config.WatermarkTextRainbow = b; ConfigManager.UpdateCache(_config); }
                ColorPick("\uE425 " + Language.Get("color_bomb_panel"), "##bp", _config.BombTimerColPanel);
                ColorPick("\uE425 " + Language.Get("color_bomb_text"), "##bt", _config.BombTimerColText);
                ColorPick("\uE425 " + Language.Get("color_bomb_marker"), "##bm", _config.BombTimerColMarker);
                b = _config.BombTimerRainbow; if (ImGui.Checkbox("\uE425 " + Language.Get("checkbox_bomb_rainbow"), ref b)) { _config.BombTimerRainbow = b; ConfigManager.UpdateCache(_config); }
                ImGui.EndTabItem();
            }
            ImGui.EndTabBar();
        }
    }

    void TabMisc()
    {
        if (ImGui.BeginTabBar("##misc_tabs"))
        {
            if (ImGui.BeginTabItem("\uE8D5 " + Language.Get("misc_movement")))
            {
                bool b = _config.BunnyHop; if (ImGui.Checkbox("\uE8D5 " + Language.Get("checkbox_bunnyhop") + "##bh", ref b)) { _config.BunnyHop = b; ConfigManager.UpdateCache(_config); }
                b = _config.VelocityGraph; if (ImGui.Checkbox("\uE8B5 " + Language.Get("checkbox_velocity_graph"), ref b)) { _config.VelocityGraph = b; ConfigManager.UpdateCache(_config); }
                ImGui.EndTabItem();
            }
            if (ImGui.BeginTabItem("\uE869 " + Language.Get("misc_tools")))
            {
                bool b = _config.TriggerBot; if (ImGui.Checkbox("\uE435 " + Language.Get("checkbox_triggerbot") + "##tr", ref b)) { _config.TriggerBot = b; ConfigManager.UpdateCache(_config); }
                b = _config.TeamCheck; if (ImGui.Checkbox("\uE87C " + Language.Get("checkbox_team_check"), ref b)) { _config.TeamCheck = b; ConfigManager.UpdateCache(_config); }
                b = _config.VoteTeller; if (ImGui.Checkbox("\uE8AF " + Language.Get("checkbox_vote_teller"), ref b)) { _config.VoteTeller = b; ConfigManager.UpdateCache(_config); }
                ImGui.EndTabItem();
            }
            if (ImGui.BeginTabItem("\uE435 " + Language.Get("misc_grenades")))
            {
                bool b = _config.GrenadeHelper; if (ImGui.Checkbox("\uE435 " + Language.Get("checkbox_grenade_helper"), ref b)) { _config.GrenadeHelper = b; ConfigManager.UpdateCache(_config); }
                if (_config.GrenadeHelper)
                {
                    bool aw = GrenadeHelper.AutoWeaponFilter;
                    ImGui.TextColored(aw ? C_Green : C_Text, "\uE873 " + Language.Get("grenade_detection") + (aw ? Language.Get("grenade_on") : Language.Get("grenade_off")));
                    var wl = new[] { Language.Get("grenade_auto") }.Concat(GrenadeHelper.GetWeaponLabels()).ToArray();
                    int si = _config.GrenadeHelperWeaponFilter + 1; if (si < 0) si = 0;
                    if (Combo("\uE869 " + Language.Get("combo_weapon_filter"), "##ghw", ref si, wl)) { _config.GrenadeHelperWeaponFilter = si - 1; if (si == 0) GrenadeHelper.ClearWeaponFilter(); else GrenadeHelper.SetWeapon(si - 1); }
                    string mi = string.IsNullOrEmpty(GrenadeHelper.CurrentMap) ? Language.Get("grenade_detecting") : GrenadeHelper.CurrentMap;
                    ImGui.TextColored(C_TextSub, "\uE80B " + Language.Get("grenade_detected_map") + mi);
                    if (!string.IsNullOrEmpty(GrenadeHelper.CurrentMap) && GrenadeHelper.GetAvailableMaps().Contains(GrenadeHelper.CurrentMap)) ImGui.TextColored(C_Green, "\uE86C " + Language.Get("grenade_data_loaded"));
                    else ImGui.TextColored(C_Yellow, "\uE002 " + Language.Get("grenade_no_data"));
                    var maps = GrenadeHelper.GetAvailableMaps();
                    if (maps.Length > 0) { int mIdx = GrenadeHelper.SelectedMapIndex; if (mIdx < 0) mIdx = 0; if (Combo("\uE8B8 " + Language.Get("combo_map_select"), "##ghm", ref mIdx, maps)) GrenadeHelper.SetManualMap(mIdx); }
                    ImGui.Spacing(); ImGui.TextWrapped(Language.Get("grenade_instructions"));
                }
                ImGui.EndTabItem();
            }
            if (ImGui.BeginTabItem("\uE5D2 " + Language.Get("misc_interface")))
            {
                bool b = _config.Watermark; if (ImGui.Checkbox("\uE8F4 " + Language.Get("checkbox_show_watermark"), ref b)) { _config.Watermark = b; ConfigManager.UpdateCache(_config); }
                b = _config.StreamProof; if (ImGui.Checkbox("\uE0B1 " + Language.Get("checkbox_stream_proof"), ref b)) { _config.StreamProof = b; ConfigManager.UpdateCache(_config); }
                b = _config.FreeCpu; if (ImGui.Checkbox("\uE8B8 " + Language.Get("checkbox_cpu_optimize"), ref b)) { _config.FreeCpu = b; ConfigManager.UpdateCache(_config); }
                ImGui.EndTabItem();
            }
            ImGui.EndTabBar();
        }
    }

    void TabSettings()
    {
        if (ImGui.BeginTabBar("##set_tabs"))
        {
            if (ImGui.BeginTabItem("\uE312 " + Language.Get("settings_keybinds")))
            {
                KeyBind("\uEA28 " + Language.Get("key_aim"), "AimBotKey", _config.AimBotKey, k => _config.AimBotKey = k);
                KeyBind("\uE435 " + Language.Get("key_trigger"), "TriggerBotKey", _config.TriggerBotKey, k => _config.TriggerBotKey = k);
                KeyBind("\uE8D5 " + Language.Get("key_bhop"), "BunnyHopKey", _config.BunnyHopKey, k => _config.BunnyHopKey = k);
                KeyBind("\uE5D2 " + Language.Get("key_menu"), "MenuToggleKey", _config.MenuToggleKey, k => _config.MenuToggleKey = k);
                ImGui.EndTabItem();
            }
            if (ImGui.BeginTabItem("\uE8BA " + Language.Get("settings_config")))
            {
                ImGui.SeparatorText(Language.Get("language_section"));
                int langIdx = Array.IndexOf(Language.Available, Language.Current);
                if (langIdx < 0) langIdx = 0;
                if (Combo("\uE8B8 " + Language.Get("language_label"), "##lang", ref langIdx, Language.Available))
                    Language.Current = Language.Available[langIdx];
                ImGui.Separator();
                if (Button("\uE161 " + Language.Get("config_save"), C_Accent)) ConfigManager.Save(_config);
                if (Button("\uE863 " + Language.Get("config_load"))) { ConfigManager.Reload(); _config = ConfigManager.Load(); }
                if (Button("\uE872 " + Language.Get("config_reset"), C_Red)) { _config = ConfigManager.Default(); ConfigManager.Save(_config); }
                ImGui.EndTabItem();
            }
            ImGui.EndTabBar();
        }
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
        string txt = waiting ? Language.Get("press_any_key") : ConfigManager.GetKeyName(currentKey).ToUpper();
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
        if (_featuresStaggerStart == 0f) _featuresStaggerStart = _animTime;
        float elapsed = _animTime - _featuresStaggerStart;
        const float staggerDelay = 0.04f;
        const float fadeDuration = 0.3f;
        float x = 8, y = 8; int cnt = 0;
        var tc = _config.WatermarkTextRainbow ? GetRainbowColor() : ToColor(new Vector4(_config.WatermarkTextColor[0], _config.WatermarkTextColor[1], _config.WatermarkTextColor[2], _config.WatermarkTextColor[3]));
        void A(string n, bool on, string? k = null)
        {
            if (!on) return;
            float itemAlpha = Math.Clamp((elapsed - cnt * staggerDelay) / fadeDuration, 0f, 1f);
            if (itemAlpha < 0.01f) { cnt++; return; }
            uint circleCol = ToColor(new Vector4(0.15f, 0.85f, 0.40f, 0.9f * itemAlpha));
            uint textCol = tc;
            if (itemAlpha < 1f)
            {
                float r = ((tc >> 0) & 0xFF) / 255f;
                float g = ((tc >> 8) & 0xFF) / 255f;
                float b = ((tc >> 16) & 0xFF) / 255f;
                textCol = ToColor(new Vector4(r, g, b, itemAlpha));
            }
            dl.AddCircleFilled(new Vector2(x + 3, y + cnt * 15 + 6), 3, circleCol);
            dl.AddText(new Vector2(x + 10, y + cnt * 15), textCol, k != null ? $"{n} [{k}]" : n);
            cnt++;
        }
        A("\uEA28 " + Language.Get("feature_aimbot"), _config.AimBot, ConfigManager.GetKeyName(_config.AimBotKey));
        A("\uE435 " + Language.Get("feature_triggerbot"), _config.TriggerBot, ConfigManager.GetKeyName(_config.TriggerBotKey));
        A("\uE8D5 " + Language.Get("feature_bunnyhop"), _config.BunnyHop, ConfigManager.GetKeyName(_config.BunnyHopKey));
        A("\uE3E7 " + Language.Get("feature_rcs"), _config.AimRcs); A("\uE7FD " + Language.Get("feature_box_esp"), _config.EspBox); A("\uE0D0 " + Language.Get("feature_skeleton_esp"), _config.SkeletonEsp);
        A("\uE84D " + Language.Get("feature_name_esp"), _config.EspName); A("\uE8E8 " + Language.Get("feature_weapon_esp"), _config.EspWeapon); A("\uE004 " + Language.Get("feature_health_bar"), _config.EspHealthBar);
        A("\uE31A " + Language.Get("feature_armor_bar"), _config.EspArmorBar); A("\uE80E " + Language.Get("feature_head_tracker"), _config.EspHeadDot); A("\uE0CD " + Language.Get("feature_snaplines"), _config.EspSnaplines);
        A("\uE893 " + Language.Get("feature_distance"), _config.EspDistance); A("\uE23F " + Language.Get("feature_flags"), _config.EspFlags); A("\uE8F0 " + Language.Get("feature_eye_traces"), _config.EspEyeTraces);
        A("\uE7FD " + Language.Get("feature_crosshair"), _config.EspAimCrosshair); A("\uE80B " + Language.Get("feature_radar"), _config.Radar); A("\uE2C7 " + Language.Get("feature_item_esp"), _config.ItemEsp);
        A("\uE663 " + Language.Get("feature_anti_flash"), _config.AntiFlash); A("\uE3E7 " + Language.Get("feature_glow_esp"), _config.EspGlow); A("\uE3B7 " + Language.Get("feature_hit_marker"), _config.HitMarker);
        A("\uE3A0 " + Language.Get("feature_damage_text"), _config.DamageText); A("\uE5E1 " + Language.Get("feature_offscreen"), _config.OffscreenEnemy); A("\uE8F4 " + Language.Get("feature_watermark"), _config.Watermark);
        A("\uE8B5 " + Language.Get("feature_velocity"), _config.VelocityGraph); A("\uE0B1 " + Language.Get("feature_streamproof"), _config.StreamProof); A("\uE87C " + Language.Get("feature_team_check"), _config.TeamCheck);
        A("\uE050 " + Language.Get("feature_hit_sound"), _config.HitSound); A("\uE430 " + Language.Get("feature_fov_circle"), _config.AimFovCircle); A("\uE8AF " + Language.Get("feature_vote_teller"), _config.VoteTeller);
        A("\uE435 " + Language.Get("feature_grenade_helper"), _config.GrenadeHelper);
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
