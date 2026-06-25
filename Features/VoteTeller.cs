using System.Numerics;
using CS2Cheat.Data.Game;
using CS2Cheat.Graphics;
using CS2Cheat.Utils;
using ImGuiNET;

namespace CS2Cheat.Features;

internal class VoteTeller : ThreadedServiceBase
{
    private const int EntityListEntryOffset = 16;
    private const int EntityListStride = 112;
    private const int VoteControllerStartIndex = 64;
    private const int VoteControllerMaxIndex = 8192;

    private readonly GameProcess _gameProcess;

    private static bool _isVoting;
    private static int _votingTeam;
    private static int _yesVotes;
    private static int _activeIssue;
    private static int _potentialVotes;

    private static readonly Dictionary<int, string> VoteIssues = new()
    {
        [0] = "Yok",
        [1] = "Oyuncuyu At",
        [2] = "Takım Değiştir",
        [3] = "Mola Ver",
        [5] = "Maçı Berabere Bitir",
        [6] = "Yeniden Oyna",
        [7] = "Surrender",
        [8] = "Timeout",
        [9] = "Kick",
        [10] = "Side (Takım Seçimi)",
    };

    public VoteTeller(GameProcess gameProcess)
    {
        _gameProcess = gameProcess;
    }

    protected override void FrameAction()
    {
        if (!_gameProcess.IsValid || _gameProcess.ModuleClient == null || _gameProcess.Process == null)
        {
            ResetVote();
            return;
        }

        var entityList = _gameProcess.ModuleClient.Read<IntPtr>(Offsets.dwEntityList);
        if (entityList == IntPtr.Zero)
        {
            ResetVote();
            return;
        }

        var voteController = FindVoteController(entityList);
        if (voteController == IntPtr.Zero)
        {
            ResetVote();
            return;
        }

        _activeIssue = _gameProcess.Process.Read<int>(voteController + Offsets.m_iActiveIssueIndex);
        _votingTeam = _gameProcess.Process.Read<int>(voteController + Offsets.m_iOnlyTeamToVote);
        _potentialVotes = _gameProcess.Process.Read<int>(voteController + Offsets.m_nPotentialVotes);
        _yesVotes = _gameProcess.Process.Read<int>(voteController + Offsets.m_nVoteOptionCount);

        _isVoting = _activeIssue > 0;
    }

    private IntPtr FindVoteController(IntPtr entityList)
    {
        if (_gameProcess.Process == null) return IntPtr.Zero;

        for (var i = VoteControllerStartIndex; i < VoteControllerMaxIndex; i++)
        {
            var listEntry = _gameProcess.Process.Read<IntPtr>(entityList + 8 * (i >> 9) + EntityListEntryOffset);
            if (listEntry == IntPtr.Zero) continue;

            var entity = _gameProcess.Process.Read<IntPtr>(listEntry + EntityListStride * (i & 0x1FF));
            if (entity == IntPtr.Zero) continue;

            var entityIdentity = _gameProcess.Process.Read<IntPtr>(entity + 0x10);
            if (entityIdentity == IntPtr.Zero) continue;

            var designerNamePtr = _gameProcess.Process.Read<IntPtr>(entityIdentity + 0x20);
            if (designerNamePtr == IntPtr.Zero) continue;

            var designerName = _gameProcess.Process.ReadString(designerNamePtr, 64);
            if (designerName == "vote_controller") return entity;
        }

        return IntPtr.Zero;
    }

    private static void ResetVote()
    {
        _isVoting = false;
        _votingTeam = 0;
        _yesVotes = 0;
        _activeIssue = 0;
    }

    public static void Draw(ImDrawListPtr drawList)
    {
        if (!_isVoting) return;

        var config = ConfigManager.Load();
        var issueName = VoteIssues.TryGetValue(_activeIssue, out var name) ? name : $"Bilinmiyor ({_activeIssue})";
        var teamName = _votingTeam == 2 ? "TERÖRİSTLER" : _votingTeam == 3 ? "ANTİ-TERÖRİSTLER" : "HERKES";
        
        var textColor = config.WatermarkTextRainbow 
            ? OverlayRenderer.GetRainbowColor() 
            : OverlayRenderer.ToColor(new Vector4(config.WatermarkTextColor[0], config.WatermarkTextColor[1], config.WatermarkTextColor[2], config.WatermarkTextColor[3]));

        var text = $"Oylama: {issueName}\nTakım: {teamName}\nSeçenek: {_yesVotes} | Oy verebilecek: {_potentialVotes}";

        var font = ImGui.GetFont();
        var fontSize = ImGui.GetFontSize() * 1.4f;
        var position = new Vector2(10, 350);

        drawList.AddText(font, fontSize, position + new Vector2(1, 1), OverlayRenderer.Colors.Black, text);
        drawList.AddText(font, fontSize, position, textColor, text);
    }
}
