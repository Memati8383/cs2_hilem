using System.Numerics;
using System.Text;
using CS2Cheat.Data.Entity;
using CS2Cheat.Data.Game;
using CS2Cheat.Graphics;
using CS2Cheat.Utils;
using ImGuiNET;

namespace CS2Cheat.Features;

internal class BombTimer(GameProcess gameProcess) : ThreadedServiceBase
{
    #region Data Fields
    public static bool IsBombPlanted { get; private set; }
    public static Vector3 BombPosition { get; private set; }
    public static float TimeLeft { get; private set; }
    public static float DefuseLeft { get; private set; }
    public static float TimerLength { get; private set; } = 40f;
    public static float DefuseLength { get; private set; } = 10f;
    public static bool IsBeingDefused { get; private set; }
    public static bool HasDefuser { get; private set; }
    public static string BombSite { get; private set; } = string.Empty;
    public static bool IsExploded { get; private set; }
    public static bool IsDefused { get; private set; }
    #endregion

    private float _lastBeepTime;
    private static string SoundPath => Path.Combine(ResourceHelper.SoundDir, "beep.wav");

    private float _lastReadTime;

    private float ReadCurrentTime()
    {
        // 1. Try dereferenced GlobalVars (Standard pointer)
        var globalVarsPtr = gameProcess.ModuleClient!.Read<IntPtr>(Offsets.dwGlobalVars);
        if (globalVarsPtr != IntPtr.Zero && globalVarsPtr.ToInt64() > 0x1000)
        {
            float t2C = gameProcess.Process!.Read<float>(globalVarsPtr + 0x2C);
            float t30 = gameProcess.Process!.Read<float>(globalVarsPtr + 0x30);
            
            if (t2C > 0.1f && t2C != _lastReadTime) return t2C;
            if (t30 > 0.1f && t30 != _lastReadTime) return t30;
        }

        // 2. Try direct GlobalVars (Offset to structure itself)
        var directGlobalVars = gameProcess.ModuleClient!.ProcessModule!.BaseAddress + Offsets.dwGlobalVars;
        float direct2C = gameProcess.Process!.Read<float>(directGlobalVars + 0x2C);
        if (direct2C > 0.1f && direct2C != _lastReadTime) return direct2C;

        return 0;
    }

    protected override void FrameAction()
    {
        if (!gameProcess.IsValid || gameProcess.ModuleClient == null || gameProcess.Process == null) return;

        float currentTime = ReadCurrentTime();
        if (currentTime > 0) _lastReadTime = currentTime;
        else currentTime = _lastReadTime;

        // 2. Find Planted C4
        IntPtr bombPtr = FindPlantedC4();
        if (bombPtr == IntPtr.Zero)
        {
            bombPtr = gameProcess.ModuleClient.Read<IntPtr>(Offsets.dwPlantedC4);
            if (bombPtr != IntPtr.Zero && bombPtr.ToInt64() > 0x1000)
            {
                var resolved = gameProcess.Process.Read<IntPtr>(bombPtr);
                if (resolved.ToInt64() > 0x1000000) bombPtr = resolved;
            }
        }

        // 3. Process Bomb Data
        if (bombPtr != IntPtr.Zero && bombPtr.ToInt64() > 0x1000)
        {
            bool ticking = gameProcess.Process.Read<bool>(bombPtr + Offsets.m_bBombTicking);
            bool defused = gameProcess.Process.Read<bool>(bombPtr + Offsets.m_bBombDefused);

            if (ticking && !defused)
            {
                float blowTime = gameProcess.Process.Read<float>(bombPtr + Offsets.m_flC4Blow);
                
                IsBombPlanted = true;
                IsDefused = false;
                
                if (currentTime > 0)
                {
                    TimeLeft = blowTime - currentTime;
                }

                TimerLength = gameProcess.Process.Read<float>(bombPtr + Offsets.m_flTimerLength);
                if (TimerLength <= 5f || TimerLength > 60f) TimerLength = 40f;

                IsBeingDefused = gameProcess.Process.Read<bool>(bombPtr + Offsets.m_bBeingDefused);
                BombSite = gameProcess.Process.Read<int>(bombPtr + Offsets.m_nBombSite) == 1 ? Language.Get("bomb_site_b") : Language.Get("bomb_site_a");

                if (IsBeingDefused)
                {
                    float defuseCountDown = gameProcess.Process.Read<float>(bombPtr + Offsets.m_flDefuseCountDown);
                    DefuseLeft = Math.Max(defuseCountDown - currentTime, 0);
                    DefuseLength = gameProcess.Process.Read<float>(bombPtr + Offsets.m_flDefuseLength);
                    if (DefuseLength <= 0) DefuseLength = HasDefuser ? 5f : 10f;
                    
                    var defuserHandle = gameProcess.Process.Read<int>(bombPtr + Offsets.m_hBombDefuser);
                    var defuserPawn = ResolveHandle(gameProcess.Process, defuserHandle);
                    if (defuserPawn != IntPtr.Zero)
                        HasDefuser = gameProcess.Process.Read<bool>(defuserPawn + Offsets.m_bHasDefuser);
                }

                var gameSceneNode = gameProcess.Process.Read<IntPtr>(bombPtr + Offsets.m_pGameSceneNode);
                if (gameSceneNode != IntPtr.Zero)
                    BombPosition = gameProcess.Process.Read<Vector3>(gameSceneNode + Offsets.m_vecAbsOrigin);
                
                IsExploded = TimeLeft <= -0.5f;
            }
            else ResetState();
        }
        else
        {
            // Fallback: Check GameRules
            var gameRules = gameProcess.ModuleClient.Read<IntPtr>(Offsets.dwGameRules);
            if (gameRules != IntPtr.Zero)
            {
                var isPlanted = gameProcess.Process.Read<bool>(gameRules + Offsets.m_bBombPlanted);
                if (isPlanted)
                {
                    if (!IsBombPlanted)
                    {
                        IsBombPlanted = true;
                        TimeLeft = 40f;
                    }
                }
                else ResetState();
            }
            else ResetState();
        }

        HandleAudio(currentTime);
    }

    private IntPtr FindPlantedC4()
    {
        var entityList = gameProcess.ModuleClient!.Read<IntPtr>(Offsets.dwEntityList);
        if (entityList == IntPtr.Zero) return IntPtr.Zero;

        for (int i = 64; i < 1024; i++) 
        {
            var listEntry = gameProcess.Process!.Read<IntPtr>(entityList + 8 * (i >> 9) + 16);
            if (listEntry == IntPtr.Zero) continue;
            
            var identityAddr = listEntry + 112 * (i & 0x1FF); // Step is 112 in this project
            var entityPtr = gameProcess.Process!.Read<IntPtr>(identityAddr);
            if (entityPtr == IntPtr.Zero) continue;

            var designerNamePtr = gameProcess.Process!.Read<IntPtr>(identityAddr + 0x20);
            if (designerNamePtr == IntPtr.Zero) continue;
            
            var name = gameProcess.Process!.ReadString(designerNamePtr, 32);
            if (name.Contains("planted_c4", StringComparison.OrdinalIgnoreCase)) 
                return entityPtr;
        }
        return IntPtr.Zero;
    }

    private IntPtr ResolveHandle(System.Diagnostics.Process process, int handle)
    {
        if (handle == -1) return IntPtr.Zero;
        var entityList = gameProcess.ModuleClient!.Read<IntPtr>(Offsets.dwEntityList);
        var listEntry = process.Read<IntPtr>(entityList + 8 * ((handle & 0x7FFF) >> 9) + 16);
        return listEntry != IntPtr.Zero ? process.Read<IntPtr>(listEntry + 112 * (handle & 0x1FF)) : IntPtr.Zero;
    }

    private void HandleAudio(float currentTime)
    {
        if (IsBombPlanted && TimeLeft < 5f && TimeLeft > 0.1f && !IsDefused && currentTime - _lastBeepTime > 0.8f)
        {
            if (File.Exists(SoundPath)) new System.Media.SoundPlayer(SoundPath).Play();
            _lastBeepTime = currentTime;
        }
    }

    private void ResetState()
    {
        IsBombPlanted = false;
        TimeLeft = 0;
        DefuseLeft = 0;
        IsBeingDefused = false;
        IsExploded = false;
        IsDefused = false;
        BombPosition = Vector3.Zero;
    }

    public static void Draw(ImDrawListPtr drawList, GameData gameData)
    {
        if (!IsBombPlanted) return;
        if (IsDefused || IsExploded) 
        {
             // Keep drawing for a second after explosion/defuse for visual feedback
             if (TimeLeft < -2.0f) return;
        }

        var player = gameData.Player;
        if (player == null) return;
        
        var config = ConfigManager.Load();

        // UI Config
        var io = ImGui.GetIO();
        var panelWidth = 400f;
        var panelHeight = 60f;
        var pos = new Vector2((io.DisplaySize.X - panelWidth) * 0.5f, 50f);

        // Background
        uint panelCol = OverlayRenderer.ToColor(new Vector4(config.BombTimerColPanel[0], config.BombTimerColPanel[1], config.BombTimerColPanel[2], config.BombTimerColPanel[3]));
        uint textCol = config.BombTimerRainbow ? OverlayRenderer.GetRainbowColor() : OverlayRenderer.ToColor(new Vector4(config.BombTimerColText[0], config.BombTimerColText[1], config.BombTimerColText[2], config.BombTimerColText[3]));
        uint markerCol = config.BombTimerRainbow ? OverlayRenderer.GetRainbowColor() : OverlayRenderer.ToColor(new Vector4(config.BombTimerColMarker[0], config.BombTimerColMarker[1], config.BombTimerColMarker[2], config.BombTimerColMarker[3]));

        drawList.AddRectFilled(pos, pos + new Vector2(panelWidth, panelHeight), panelCol, 10f);
        drawList.AddRect(pos, pos + new Vector2(panelWidth, panelHeight), textCol, 10f, ImDrawFlags.None, 2f);

        // Progress Bar
        float visualTime = Math.Clamp(TimeLeft, 0f, TimerLength);
        float progress = visualTime / TimerLength;
        uint barColor = config.BombTimerRainbow ? OverlayRenderer.GetRainbowColor() : OverlayRenderer.ToColor((byte)(255 * (1 - progress)), (byte)(255 * progress), 0);
        drawList.AddRectFilled(pos + new Vector2(10, 35), pos + new Vector2(10 + (panelWidth - 20) * progress, 50), barColor, 5f);

        // Text
        string text = $"{Language.Get("bomb_bomba")} [{BombSite}] - {visualTime:0.0}s";
        drawList.AddText(pos + new Vector2(panelWidth * 0.5f - ImGui.CalcTextSize(text).X * 0.5f, 10), textCol, text);

        // Tactical Info & ESP Marker
        if (BombPosition != Vector3.Zero)
        {
            float distMeters = TacticalManager.GetDistanceInMeters(player.Origin, BombPosition);
            
            // ESP Marker
            Vector3 screenPos = player.MatrixViewProjectionViewport.Transform(BombPosition);
            if (screenPos.Z < 1)
            {
                var bombPos2D = new Vector2(screenPos.X, screenPos.Y);
                drawList.AddCircleFilled(bombPos2D, 15f, OverlayRenderer.ToColor(10, 10, 10, 180));
                drawList.AddCircle(bombPos2D, 15f, markerCol, 0, 2f);
                
                string marker = Language.Get("bomb_c4");
                string distText = $"{(int)distMeters}m";
                
                Vector2 markerSize = ImGui.CalcTextSize(marker);
                Vector2 distSize = ImGui.CalcTextSize(distText);
                
                drawList.AddText(bombPos2D - new Vector2(markerSize.X * 0.5f, markerSize.Y), config.BombTimerRainbow ? OverlayRenderer.GetRainbowColor() : textCol, marker);
                drawList.AddText(bombPos2D + new Vector2(-distSize.X * 0.5f, 2f), markerCol, distText);
            }
        }

        // Defusal Bar
        if (IsBeingDefused)
        {
            var dBarPos = pos + new Vector2(0, panelHeight + 40f);
            float dProg = Math.Clamp(DefuseLeft / DefuseLength, 0f, 1f);
            bool canDefuse = DefuseLeft < TimeLeft;

            drawList.AddRectFilled(dBarPos, dBarPos + new Vector2(panelWidth, 30f), OverlayRenderer.ToColor(15, 15, 15, 220), 8f);
            uint dCol = canDefuse ? OverlayRenderer.Colors.Green : OverlayRenderer.Colors.Red;
            drawList.AddRectFilled(dBarPos + new Vector2(4, 4), dBarPos + new Vector2(4 + (panelWidth - 8) * (1.0f - dProg), 26f), dCol, 5f);
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
