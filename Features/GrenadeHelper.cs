using System.Diagnostics;
using System.Numerics;
using System.Text.Json;
using CS2Cheat.Core;
using CS2Cheat.Data.Game;
using CS2Cheat.Graphics;
using CS2Cheat.Utils;
using ImGuiNET;

namespace CS2Cheat.Features;

public class GrenadeEntry
{
    public string? Description { get; set; }
    public List<string>? Name { get; set; }
    public List<float> Position { get; set; } = new();
    public List<float> Viewangles { get; set; } = new();
    public string Weapon { get; set; } = "";
    public bool Duck { get; set; }
    public bool Jump { get; set; }
    public GrenadeNested? Grenade { get; set; }
}

public class GrenadeNested
{
    public bool Jump { get; set; }
    public bool Duck { get; set; }
}

public static class GrenadeHelper
{
    private static Dictionary<string, List<GrenadeEntry>> _allGrenades = new();
    private static bool _dataLoaded;
    private static string _currentMap = "";
    private static string _selectedWeapon = "";
    private static int _selectedWeaponIndex = -1;
    private static bool _autoWeaponFilter = true;

    private static readonly string[] WeaponNames = { "weapon_smokegrenade", "weapon_hegrenade", "weapon_flashbang", "weapon_molotov" };
    private static readonly string[] WeaponLabels = { "Smoke", "HE", "Flash", "Molotov" };

    public static string CurrentMap => _currentMap;

    private static IntPtr _matchmakingBase;
    private static IntPtr _engineBase;
    private static int _mapReadFailCount;
    private static string _manualMapOverride = "";
    private static string[] _availableMapNames = Array.Empty<string>();

    private const float ProximityThreshold = 300f;
    private static GrenadeEntry? _nearestEntry;
    private static float _nearestDist;
    private static float _proximityTimer;

    public static string[] GetAvailableMaps() => _availableMapNames;

    public static int SelectedMapIndex
    {
        get
        {
            var map = !string.IsNullOrEmpty(_manualMapOverride) ? _manualMapOverride : _currentMap;
            return Array.IndexOf(_availableMapNames, map);
        }
    }

    public static void SetManualMap(int index)
    {
        if (index >= 0 && index < _availableMapNames.Length)
        {
            _manualMapOverride = _availableMapNames[index];
            _currentMap = _manualMapOverride;
        }
    }

    private static short DetectHeldWeapon(GameProcess gp, IntPtr pawnAddr)
    {
        if (gp.Process == null || pawnAddr == IntPtr.Zero) return 0;
        try
        {
            var svc = gp.Process.Read<IntPtr>(pawnAddr + Offsets.m_pWeaponServices);
            if (svc == IntPtr.Zero) return 0;

            var handle = gp.Process.Read<int>(svc + Offsets.m_hActiveWeapon);
            if (handle <= 0) return 0;

            var entityList = gp.ModuleClient!.Read<IntPtr>(Offsets.dwEntityList);
            if (entityList == IntPtr.Zero) return 0;

            var chunk = gp.Process.Read<IntPtr>(entityList + 0x8 * ((handle & 0x7FFF) >> 9) + 16);
            if (chunk == IntPtr.Zero) return 0;

            var weaponAddr = gp.Process.Read<IntPtr>(chunk + 112 * (handle & 0x1FF));
            if (weaponAddr == IntPtr.Zero) return 0;

            return gp.Process.Read<short>(weaponAddr + Offsets.m_AttributeManager + Offsets.m_Item + Offsets.m_iItemDefinitionIndex);
        }
        catch { return 0; }
    }

    private static string MapWeaponIndexToGrenade(short idx)
    {
        return idx switch
        {
            40 => "weapon_flashbang",
            41 => "weapon_hegrenade",
            42 => "weapon_smokegrenade",
            43 => "weapon_molotov",
            45 => "weapon_molotov",
            _ => ""
        };
    }

    public static void Draw(ImDrawListPtr dl, GameData gameData, GameProcess gameProcess)
    {
        var config = ConfigManager.Load();
        if (!config.GrenadeHelper) return;

        if (!_dataLoaded)
            LoadGrenadeData();

        UpdateMapName(gameProcess);

        if (!_dataLoaded || !_allGrenades.ContainsKey(_currentMap))
            return;

        var allEntries = _allGrenades[_currentMap];
        var io = ImGui.GetIO();

        string effectiveWeapon = _selectedWeapon;

        // Auto-detect held grenade via direct memory read
        if (_autoWeaponFilter && gameData.Player != null && gameData.Player.AddressBase != IntPtr.Zero)
        {
            short idx = DetectHeldWeapon(gameProcess, gameData.Player.AddressBase);
            string mapped = MapWeaponIndexToGrenade(idx);
            if (!string.IsNullOrEmpty(mapped))
                effectiveWeapon = mapped;
        }

        var visible = string.IsNullOrEmpty(effectiveWeapon)
            ? allEntries
            : allEntries.Where(e => e.Weapon == effectiveWeapon).ToList();

        if (visible.Count == 0)
            return;

        // Sort by distance to player
        var playerPos = gameData.Player != null ? gameData.Player.Origin : Vector3.Zero;
        var sorted = visible
            .Select(e => new { Entry = e, WorldPos = e.Position.Count >= 3 ? new Vector3(e.Position[0], e.Position[1], e.Position[2]) : Vector3.Zero })
            .Select(e => new { e.Entry, e.WorldPos, Dist = Vector3.Distance(playerPos, e.WorldPos) })
            .OrderBy(e => e.Dist)
            .ToList();

        // Draw markers with cluster grouping
        _nearestEntry = null;
        _nearestDist = float.MaxValue;
        const float maxDrawDist = 3000f;
        const float clusterThreshold = 80f;

        var clusters = new List<(Vector3 WorldPos, float Dist, List<GrenadeEntry> Entries)>();
        foreach (var item in sorted)
        {
            if (item.Dist > maxDrawDist || item.Entry.Position.Count < 3) continue;
            bool added = false;
            for (int i = 0; i < clusters.Count; i++)
            {
                if (Vector3.Distance(clusters[i].WorldPos, item.WorldPos) < clusterThreshold)
                {
                    clusters[i].Entries.Add(item.Entry);
                    added = true;
                    break;
                }
            }
            if (!added)
                clusters.Add((item.WorldPos, item.Dist, new List<GrenadeEntry> { item.Entry }));
        }

        foreach (var cluster in clusters)
        {
            var screenPos = gameData.Player?.MatrixViewProjectionViewport.Transform(cluster.WorldPos) ?? new Vector3(-1, -1, 2);
            if (screenPos.Z >= 1) continue;

            var sp = new Vector2(screenPos.X, screenPos.Y);
            bool isNear = cluster.Dist < ProximityThreshold;
            float alpha = Math.Clamp(1f - (cluster.Dist - ProximityThreshold * 0.5f) / (ProximityThreshold * 0.5f), 0.4f, 1f);

            if (cluster.Entries.Count == 1)
            {
                DrawSingleMarker(dl, cluster.Entries[0], sp, isNear, alpha, cluster.Dist);
                if (isNear && cluster.Dist < _nearestDist)
                {
                    _nearestDist = cluster.Dist;
                    _nearestEntry = cluster.Entries[0];
                }
            }
            else
            {
                DrawClusterMarker(dl, cluster, sp, isNear, alpha);
            }
        }

        // Draw instruction panel
        if (_nearestEntry != null)
        {
            float dt = io.DeltaTime > 0f ? io.DeltaTime : 0.016f;
            _proximityTimer = Math.Min(_proximityTimer + dt * 4f, 1f);
            DrawInstructionPanel(dl, _nearestEntry, _nearestDist, io, gameData, gameProcess);
        }
        else
        {
            float dt = io.DeltaTime > 0f ? io.DeltaTime : 0.016f;
            _proximityTimer = Math.Max(_proximityTimer - dt * 4f, 0f);
        }
    }

    private static void DrawSingleMarker(ImDrawListPtr dl, GrenadeEntry entry, Vector2 sp, bool isNear, float alpha, float dist)
    {
        var color = GetWeaponColor(entry.Weapon);
        float radius = isNear ? 10f : 6f;

        dl.AddCircleFilled(sp, radius, ApplyAlpha(color, (byte)(180 * alpha)), 12);
        dl.AddCircle(sp, radius, ApplyAlpha(color, (byte)(255 * alpha)), 12, isNear ? 2.5f : 1.5f);

        string label = GetWeaponLabel(entry.Weapon);
        string letter = label.Length > 0 ? label[..1] : "G";
        dl.AddText(null, isNear ? 11f : 9f, sp - new Vector2(3, 5), ApplyAlpha(0xFFFFFFFF, (byte)(255 * alpha)), letter);

        if (entry.Viewangles.Count >= 2 && isNear)
        {
            float yawRad = entry.Viewangles[1] * (float)(Math.PI / 180f);
            var dir = new Vector2((float)Math.Cos(yawRad), -(float)Math.Sin(yawRad));
            float arrowLen = 30f;
            var arrowEnd = sp + dir * arrowLen;
            dl.AddLine(sp, arrowEnd, ApplyAlpha(color, (byte)(180 * alpha)), 2f);
            var perp = new Vector2(-dir.Y, dir.X);
            dl.AddTriangleFilled(arrowEnd, arrowEnd - (dir * 8f) + (perp * 5f), arrowEnd - (dir * 8f) - (perp * 5f),
                ApplyAlpha(color, (byte)(200 * alpha)));
        }

        if (isNear && entry.Name is { Count: >= 1 } && !string.IsNullOrEmpty(entry.Name[0]))
        {
            string nm = entry.Name[0];
            float maxW = 120f;
            var ts = ImGui.CalcTextSize(nm);
            if (ts.X > maxW) { int cut = (int)(maxW / (ts.X / nm.Length)) - 2; if (cut < 1) cut = 1; nm = nm[..cut] + ".."; }
            dl.AddText(null, 10f, sp + new Vector2(radius + 4, -8), ApplyAlpha(0xFFFFFFFF, (byte)(220 * alpha)), nm);
        }
    }

    private static void DrawClusterMarker(ImDrawListPtr dl, (Vector3 WorldPos, float Dist, List<GrenadeEntry> Entries) cluster, Vector2 sp, bool isNear, float alpha)
    {
        int count = cluster.Entries.Count;
        var weapons = cluster.Entries.Select(e => e.Weapon).Distinct().ToList();
        var primaryColor = GetWeaponColor(weapons[0]);
        float radius = isNear ? 16f : 9f;

        dl.AddCircleFilled(sp, radius, ApplyAlpha(primaryColor, (byte)(160 * alpha)), 16);
        dl.AddCircle(sp, radius, ApplyAlpha(0xFFFFFFFF, (byte)(220 * alpha)), 16, isNear ? 2.5f : 1.5f);

        string countText = count.ToString();
        var countSize = ImGui.CalcTextSize(countText);
        dl.AddText(null, isNear ? 14f : 11f, sp - countSize * 0.5f, ApplyAlpha(0xFFFFFFFF, (byte)(255 * alpha)), countText);

        if (isNear && weapons.Count > 1)
        {
            float iconX = sp.X - (weapons.Count * 8f) / 2f;
            for (int wi = 0; wi < weapons.Count; wi++)
            {
                var wc = GetWeaponColor(weapons[wi]);
                dl.AddCircleFilled(new Vector2(iconX + wi * 8f, sp.Y + radius + 6), 3f,
                    ApplyAlpha(wc, (byte)(220 * alpha)), 8);
            }
        }

        if (isNear)
        {
            float spreadRadius = radius + 28f;
            for (int i = 0; i < cluster.Entries.Count; i++)
            {
                var entry = cluster.Entries[i];
                float angle = (float)(2 * Math.PI * i / cluster.Entries.Count) - (float)Math.PI / 2f;
                var offset = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * spreadRadius;
                var childSp = sp + offset;
                var childColor = GetWeaponColor(entry.Weapon);

                dl.AddCircleFilled(childSp, 7f, ApplyAlpha(childColor, (byte)(200 * alpha)), 10);
                dl.AddCircle(childSp, 7f, ApplyAlpha(0xFFFFFFFF, (byte)(180 * alpha)), 10, 1f);

                string cl = GetWeaponLabel(entry.Weapon);
                string childLetter = cl.Length > 0 ? cl[..1] : "G";
                dl.AddText(null, 9f, childSp - new Vector2(2, 4), ApplyAlpha(0xFFFFFFFF, (byte)(255 * alpha)), childLetter);

                if (entry.Viewangles.Count >= 2)
                {
                    float yawRad = entry.Viewangles[1] * (float)(Math.PI / 180f);
                    var dir = new Vector2((float)Math.Cos(yawRad), -(float)Math.Sin(yawRad));
                    dl.AddLine(childSp, childSp + dir * 18f, ApplyAlpha(childColor, (byte)(150 * alpha)), 1.5f);
                }

                if (entry.Name is { Count: >= 1 } && !string.IsNullOrEmpty(entry.Name[0]))
                {
                    string nm = entry.Name[0];
                    var ts = ImGui.CalcTextSize(nm);
                    float maxW = 100f;
                    if (ts.X > maxW) { int cut = (int)(maxW / (ts.X / nm.Length)) - 2; if (cut < 1) cut = 1; nm = nm[..cut] + ".."; }
                    dl.AddText(null, 8f, childSp + new Vector2(9, -3), ApplyAlpha(0xFFFFFFFF, (byte)(180 * alpha)), nm);
                }

                if (isNear && cluster.Dist < _nearestDist)
                {
                    _nearestDist = cluster.Dist;
                    _nearestEntry = entry;
                }
            }
        }
    }

    private static void DrawInstructionPanel(ImDrawListPtr dl, GrenadeEntry entry, float dist, ImGuiIOPtr io, GameData gameData, GameProcess gameProcess)
    {
        float alpha  = _proximityTimer;
        byte  aBg    = (byte)(230 * alpha);
        byte  aText  = (byte)(255 * alpha);
        byte  aSub   = (byte)(190 * alpha);
        byte  aAccent= (byte)(255 * alpha);
        byte  aDim   = (byte)(130 * alpha);

        var font = ImGui.GetFont();

        float panelW = 370f;
        float tipH   = GetInstructionHeight(entry);
        float px     = io.DisplaySize.X * 0.5f - panelW * 0.5f;
        float py     = io.DisplaySize.Y - tipH - 34f;

        // ── Background ──────────────────────────────────────────────────────
        dl.AddRectFilled(new Vector2(px, py), new Vector2(px + panelW, py + tipH),
            OverlayRenderer.ToColor(8, 8, 14, aBg), 10f);

        // Subtle glass highlight strip at top
        dl.AddRectFilled(new Vector2(px + 1, py + 1), new Vector2(px + panelW - 1, py + 4),
            OverlayRenderer.ToColor(255, 255, 255, (byte)(18 * alpha)), 9f);

        // Weapon-colored left accent bar
        uint wCol = GetWeaponColor(entry.Weapon);
        dl.AddRectFilled(new Vector2(px, py + 10), new Vector2(px + 3, py + tipH - 10),
            ApplyAlpha(wCol, (byte)(200 * alpha)), 2f);

        // Border
        dl.AddRect(new Vector2(px, py), new Vector2(px + panelW, py + tipH),
            ApplyAlpha(wCol, (byte)(55 * alpha)), 10f, ImDrawFlags.None, 1f);

        // ── Header row ──────────────────────────────────────────────────────
        string wLabel = GetWeaponLabel(entry.Weapon);
        dl.AddText(font, 14f, new Vector2(px + 14, py + 9), ApplyAlpha(wCol, aText), wLabel.ToUpper());

        // Distance badge (right-aligned)
        float distM  = dist * 0.01905f;   // CS2: 1 unit ≈ 1.905 cm
        string distStr = $"{(int)distM}m";
        uint distCol = distM < 3f
            ? OverlayRenderer.ToColor(80, 230, 120, aText)
            : OverlayRenderer.ToColor(255, 200, 80, aText);
        float distW  = ImGui.CalcTextSize(distStr).X;
        dl.AddRectFilled(new Vector2(px + panelW - distW - 18, py + 7),
                         new Vector2(px + panelW - 8, py + 23),
            OverlayRenderer.ToColor(30, 30, 40, (byte)(180 * alpha)), 4f);
        dl.AddText(font, 11f, new Vector2(px + panelW - distW - 13, py + 9), distCol, distStr);

        // Thin separator under header
        float sepY = py + 28f;
        dl.AddLine(new Vector2(px + 10, sepY), new Vector2(px + panelW - 10, sepY),
            OverlayRenderer.ToColor(255, 255, 255, (byte)(18 * alpha)), 1f);

        float iy = py + 34f;

        // ── Location name ───────────────────────────────────────────────────
        if (entry.Name is { Count: >= 1 } && !string.IsNullOrEmpty(entry.Name[0]))
        {
            string loc = entry.Name[0];
            if (entry.Name.Count >= 2 && !string.IsNullOrEmpty(entry.Name[1]))
                loc += $"  \xbb  {entry.Name[1]}";
            dl.AddText(font, 13f, new Vector2(px + 14, iy),
                OverlayRenderer.ToColor(235, 230, 230, aText), loc);
            iy += 20f;
        }
        else
        {
            iy += 4f;
        }

        // ── Description ─────────────────────────────────────────────────────
        if (entry.Description is { Length: > 0 })
        {
            dl.AddText(font, 11f, new Vector2(px + 14, iy),
                OverlayRenderer.ToColor(165, 160, 175, aSub), entry.Description);
            iy += 18f;
        }

        iy += 4f;

        // Separator before aim / position data
        dl.AddLine(new Vector2(px + 10, iy), new Vector2(px + panelW - 10, iy),
            OverlayRenderer.ToColor(255, 255, 255, (byte)(14 * alpha)), 1f);
        iy += 6f;

        // ── Aim angles ──────────────────────────────────────────────────────
        if (entry.Viewangles.Count >= 2)
        {
            dl.AddText(font, 10f, new Vector2(px + 14, iy + 1),
                OverlayRenderer.ToColor(120, 120, 140, aDim), "AIM");
            string aimVals = $"{entry.Viewangles[0]:0.0}°   {entry.Viewangles[1]:0.0}°";
            dl.AddText(font, 12f, new Vector2(px + 44, iy),
                OverlayRenderer.ToColor(100, 210, 255, aText), aimVals);
        }
        iy += 18f;

        // ── Position ────────────────────────────────────────────────────────
        if (entry.Position.Count >= 3)
        {
            dl.AddText(font, 10f, new Vector2(px + 14, iy + 1),
                OverlayRenderer.ToColor(120, 120, 140, aDim), "POS");
            string posVals = $"{entry.Position[0]:0}   {entry.Position[1]:0}   {entry.Position[2]:0}";
            dl.AddText(font, 11f, new Vector2(px + 44, iy),
                OverlayRenderer.ToColor(160, 160, 180, aSub), posVals);
            iy += 17f;
        }

        iy += 4f;

        // ── Duck / Jump badges ──────────────────────────────────────────────
        bool needsDuck = entry.Duck || (entry.Grenade?.Duck ?? false);
        bool needsJump = entry.Jump || (entry.Grenade?.Jump ?? false);
        if (needsDuck || needsJump)
        {
            float badgeX = px + 14f;
            if (needsDuck)
            {
                dl.AddRectFilled(new Vector2(badgeX, iy), new Vector2(badgeX + 64, iy + 20),
                    OverlayRenderer.ToColor(30, 70, 120, (byte)(210 * alpha)), 5f);
                dl.AddRect(new Vector2(badgeX, iy), new Vector2(badgeX + 64, iy + 20),
                    OverlayRenderer.ToColor(60, 140, 220, (byte)(90 * alpha)), 5f, ImDrawFlags.None, 1f);
                dl.AddText(font, 11f, new Vector2(badgeX + 7, iy + 4),
                    OverlayRenderer.ToColor(100, 200, 255, aText), "CROUCH");
                badgeX += 70f;
            }
            if (needsJump)
            {
                dl.AddRectFilled(new Vector2(badgeX, iy), new Vector2(badgeX + 56, iy + 20),
                    OverlayRenderer.ToColor(30, 100, 40, (byte)(210 * alpha)), 5f);
                dl.AddRect(new Vector2(badgeX, iy), new Vector2(badgeX + 56, iy + 20),
                    OverlayRenderer.ToColor(60, 200, 80, (byte)(90 * alpha)), 5f, ImDrawFlags.None, 1f);
                dl.AddText(font, 11f, new Vector2(badgeX + 7, iy + 4),
                    OverlayRenderer.ToColor(120, 255, 120, aText), "JUMP");
            }
            iy += 26f;
        }

        iy += 2f;

        // ── Throw instruction ────────────────────────────────────────────────
        string instruction = BuildThrowInstruction(entry, dist);
        dl.AddText(font, 12f, new Vector2(px + 14, iy),
            OverlayRenderer.ToColor(190, 255, 190, aText), instruction);

        // ── World overlay: line + target rings ───────────────────────────────
        if (gameData.Player != null && entry.Position.Count >= 3)
        {
            var targetPos    = new Vector3(entry.Position[0], entry.Position[1], entry.Position[2]);
            var screenTarget = gameData.Player.MatrixViewProjectionViewport.Transform(targetPos);
            var screenPlayer = gameData.Player.MatrixViewProjectionViewport.Transform(gameData.Player.Origin);

            if (screenTarget.Z < 1 && screenPlayer.Z < 1)
            {
                var stPt = new Vector2(screenTarget.X, screenTarget.Y);
                var spPt = new Vector2(screenPlayer.X, screenPlayer.Y);
                var mid  = (stPt + spPt) * 0.5f;

                // Two-segment line (slightly faded mid-point)
                dl.AddLine(spPt, mid, ApplyAlpha(wCol, (byte)(55 * alpha)), 1.5f);
                dl.AddLine(mid, stPt, ApplyAlpha(wCol, (byte)(80 * alpha)), 1.5f);

                // Concentric target rings
                dl.AddCircleFilled(stPt, 14f, OverlayRenderer.ToColor(8, 8, 14, (byte)(180 * alpha)));
                dl.AddCircle(stPt, 14f, ApplyAlpha(wCol, aAccent), 0, 2f);
                dl.AddCircle(stPt, 8f,  ApplyAlpha(wCol, (byte)(150 * alpha)), 0, 1f);
                dl.AddCircleFilled(stPt, 3f, ApplyAlpha(wCol, aAccent));
            }
        }

        // ── Hint footer ──────────────────────────────────────────────────────
        string hint = "LMB  outside menu to apply viewangles";
        float hintW = ImGui.CalcTextSize(hint).X;
        float hx    = px + panelW * 0.5f - hintW * 0.5f;
        dl.AddText(font, 10f, new Vector2(hx, py + tipH - 16f),
            OverlayRenderer.ToColor(180, 180, 200, (byte)(100 * alpha)), hint);
    }

    private static string BuildThrowInstruction(GrenadeEntry entry, float dist)
    {
        bool duck = entry.Duck || (entry.Grenade?.Duck ?? false);
        bool jump = entry.Jump || (entry.Grenade?.Jump ?? false);

        string action;
        if (duck && jump) action = "Crouch-jump";
        else if (duck) action = "Crouch";
        else if (jump) action = "Jump";
        else action = "Stand";

        string throwType;
        var w = entry.Weapon;
        if (w.Contains("smoke")) throwType = "throw smoke";
        else if (w.Contains("he")) throwType = "throw HE";
        else if (w.Contains("flash")) throwType = "throw flash";
        else if (w.Contains("molotov") || w.Contains("inc")) throwType = "throw molotov";
        else throwType = "throw";

        string loc = entry.Name is { Count: >= 2 } && !string.IsNullOrEmpty(entry.Name[1])
            ? $"  \xbb  {entry.Name[1]}"
            : "";

        return $"{action} and {throwType}{loc}";
    }

    private static float GetInstructionHeight(GrenadeEntry entry)
    {
        float h = 50f;
        if (entry.Name is { Count: >= 1 } && !string.IsNullOrEmpty(entry.Name[0])) h += 20;
        else h += 6;
        if (entry.Description is { Length: > 0 }) h += 18;
        h += 20;
        h += 17;
        bool duck = entry.Duck || (entry.Grenade?.Duck ?? false);
        bool jump = entry.Jump || (entry.Grenade?.Jump ?? false);
        if (duck || jump) h += 26;
        h += 20;
        h += 18; // hint
        return h;
    }

    public static bool ApplyViewangles(GameProcess gameProcess, out Vector3 targetAngles)
    {
        targetAngles = Vector3.Zero;
        if (_nearestEntry == null || _nearestEntry.Viewangles.Count < 2) return false;
        targetAngles = new Vector3(_nearestEntry.Viewangles[0], _nearestEntry.Viewangles[1], 0);
        return true;
    }

    public static void SetWeapon(int index)
    {
        _autoWeaponFilter = false;
        if (index >= 0 && index < WeaponNames.Length)
        {
            _selectedWeaponIndex = index;
            _selectedWeapon = WeaponNames[index];
        }
    }

    public static void ClearWeaponFilter()
    {
        _autoWeaponFilter = true;
        _selectedWeapon = "";
        _selectedWeaponIndex = -1;
    }

    public static string[] GetWeaponLabels() => WeaponLabels;
    public static int SelectedWeaponIndex => _selectedWeaponIndex;
    public static bool AutoWeaponFilter => _autoWeaponFilter;

    private static uint GetWeaponColor(string weapon)
    {
        if (weapon.Contains("smoke")) return OverlayRenderer.ToColor(204, 204, 204);
        if (weapon.Contains("he")) return OverlayRenderer.ToColor(255, 68, 68);
        if (weapon.Contains("flash")) return OverlayRenderer.ToColor(255, 221, 68);
        if (weapon.Contains("molotov") || weapon.Contains("inc")) return OverlayRenderer.ToColor(255, 136, 0);
        return OverlayRenderer.ToColor(255, 255, 255);
    }

    private static string GetWeaponLabel(string weapon)
    {
        return weapon switch
        {
            "weapon_smokegrenade" => "Smoke",
            "weapon_hegrenade" => "HE",
            "weapon_flashbang" => "Flash",
            "weapon_molotov" or "weapon_incgrenade" => "Molotov",
            _ => weapon
        };
    }

    private static uint ApplyAlpha(uint color, byte alpha)
    {
        // ImGui uses ABGR: alpha occupies bits 24-31
        return ((uint)alpha << 24) | (color & 0x00FFFFFF);
    }

    // ─── Map Name Detection ───

    private static void UpdateMapName(GameProcess gameProcess)
    {
        if (gameProcess.Process == null || gameProcess.ModuleClient == null) return;

        if (!string.IsNullOrEmpty(_manualMapOverride)) return;

        if (_matchmakingBase == IntPtr.Zero || _engineBase == IntPtr.Zero)
        {
            try
            {
                foreach (ProcessModule m in gameProcess.Process.Modules)
                {
                    if (m.ModuleName.Equals("matchmaking.dll", StringComparison.OrdinalIgnoreCase))
                        _matchmakingBase = m.BaseAddress;
                    else if (m.ModuleName.Equals("engine2.dll", StringComparison.OrdinalIgnoreCase))
                        _engineBase = m.BaseAddress;
                }
            }
            catch { }
        }

        if (_matchmakingBase != IntPtr.Zero && TryMatchmaking(gameProcess)) return;
        if (_engineBase != IntPtr.Zero && TryEngine2(gameProcess)) return;
        if (TryGameRules(gameProcess)) return;
        if (_mapReadFailCount == 0 && TryCmdLine(gameProcess)) return;
        if (_mapReadFailCount > 3 && TryMemoryScan(gameProcess)) return;

        _mapReadFailCount++;
    }

    private static bool TryMatchmaking(GameProcess gp)
    {
        try
        {
            var ptr = gp.Process!.Read<IntPtr>(_matchmakingBase + Offsets.dwGameTypes);
            if (ptr == IntPtr.Zero) return false;
            var n = gp.Process!.ReadString(ptr + Offsets.dwGameTypes_mapName, 64);
            if (IsValidMap(n)) { _currentMap = NormalizeMap(n); return true; }
            var p2 = gp.Process!.Read<IntPtr>(ptr + Offsets.dwGameTypes_mapName);
            if (p2 != IntPtr.Zero && p2.ToInt64() > 0x10000)
            {
                n = gp.Process!.ReadString(p2, 64);
                if (IsValidMap(n)) { _currentMap = NormalizeMap(n); return true; }
            }
        }
        catch { }
        return false;
    }

    private static bool TryEngine2(GameProcess gp)
    {
        try
        {
            var ngc = gp.Process!.Read<IntPtr>(_engineBase + Offsets.dwNetworkGameClient);
            if (ngc == IntPtr.Zero) return false;
            foreach (int off in new[] { 0x138, 0x248, 0x128, 0x238, 0x148, 0x258, 0x120 })
            {
                var n = gp.Process!.ReadString(ngc + off, 64);
                if (IsValidMap(n)) { _currentMap = NormalizeMap(n); return true; }
            }
        }
        catch { }
        return false;
    }

    private static bool TryGameRules(GameProcess gp)
    {
        try
        {
            var gr = gp.ModuleClient!.Read<IntPtr>(Offsets.dwGameRules);
            if (gr == IntPtr.Zero) return false;
            foreach (int off in new[] { 0x430, 0x520, 0x530, 0x438, 0x528, 0x440, 0x4F8, 0x500, 0x508 })
            {
                var n = gp.Process!.ReadString(gr + off, 64);
                if (IsValidMap(n)) { _currentMap = NormalizeMap(n); return true; }
            }
        }
        catch { }
        return false;
    }

    private static bool TryCmdLine(GameProcess gp)
    {
        try
        {
            var args = gp.Process!.StartInfo?.Arguments;
            if (!string.IsNullOrEmpty(args))
            {
                var m = System.Text.RegularExpressions.Regex.Match(args, @"\+map\s+(\S+)");
                if (m.Success && IsValidMap(m.Groups[1].Value))
                {
                    _currentMap = NormalizeMap(m.Groups[1].Value);
                    return true;
                }
            }
        }
        catch { }
        return false;
    }

    private static bool TryMemoryScan(GameProcess gp)
    {
        try
        {
            var gr = gp.ModuleClient!.Read<IntPtr>(Offsets.dwGameRules);
            if (gr == IntPtr.Zero) return false;
            var buf = new byte[4096];
            var h = System.Runtime.InteropServices.GCHandle.Alloc(buf, System.Runtime.InteropServices.GCHandleType.Pinned);
            try
            {
                if (Kernel32.ReadProcessMemory(gp.Process!.Handle, gr, h.AddrOfPinnedObject(), buf.Length, out _))
                {
                    var txt = System.Text.Encoding.UTF8.GetString(buf);
                    foreach (var km in _availableMapNames)
                        if (txt.Contains(km, StringComparison.OrdinalIgnoreCase))
                        { _currentMap = km; return true; }
                }
            }
            finally { h.Free(); }
        }
        catch { }
        return false;
    }

    private static bool IsValidMap(string? n) =>
        !string.IsNullOrEmpty(n) && n.Contains('_') && (n.StartsWith("de_") || n.StartsWith("cs_"));

    private static string NormalizeMap(string n)
    {
        n = n.Trim().ToLowerInvariant();
        if (n.Contains('/')) n = n.Split('/')[^1];
        if (n.EndsWith(".bsp", StringComparison.OrdinalIgnoreCase)) n = n[..^4];
        return n;
    }

    private static bool _loadAttempted;

    private static void LoadGrenadeData()
    {
        if (_loadAttempted) return; // Prevent infinite retry on failure
        _loadAttempted = true;
        try
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", "grenades.json");
            if (!File.Exists(path)) { _dataLoaded = false; return; }
            var json = File.ReadAllText(path);
            var raw = JsonSerializer.Deserialize<Dictionary<string, List<GrenadeEntry>>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (raw != null)
            {
                _allGrenades = raw;
                _availableMapNames = raw.Keys.OrderBy(k => k).ToArray();
                _dataLoaded = true;
            }
        }
        catch { _dataLoaded = false; }
    }
}