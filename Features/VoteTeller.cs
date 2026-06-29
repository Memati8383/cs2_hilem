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
        [0] = Language.Get("vote_none"),
        [1] = Language.Get("vote_kick"),
        [2] = Language.Get("vote_swap"),
        [3] = Language.Get("vote_timeout"),
        [5] = Language.Get("vote_draw"),
        [6] = Language.Get("vote_rematch"),
        [7] = Language.Get("vote_surrender"),
        [8] = Language.Get("vote_timeout"),
        [9] = Language.Get("vote_kick"),
        [10] = Language.Get("vote_side"),
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
        var issueName = VoteIssues.TryGetValue(_activeIssue, out var name) ? name : $"{Language.Get("vote_unknown")} ({_activeIssue})";
        var teamName = _votingTeam == 2 ? Language.Get("vote_terrorists") : _votingTeam == 3 ? Language.Get("vote_ct") : Language.Get("vote_everyone");
        
        var textColor = config.WatermarkTextRainbow 
            ? OverlayRenderer.GetRainbowColor() 
            : OverlayRenderer.ToColor(new Vector4(config.WatermarkTextColor[0], config.WatermarkTextColor[1], config.WatermarkTextColor[2], config.WatermarkTextColor[3]));

        var text = string.Format(Language.Get("vote_format"), issueName, teamName, _yesVotes, _potentialVotes);

        var font = ImGui.GetFont();
        var fontSize = ImGui.GetFontSize() * 1.4f;
        var position = new Vector2(10, 350);

        drawList.AddText(font, fontSize, position + new Vector2(1, 1), OverlayRenderer.Colors.Black, text);
        drawList.AddText(font, fontSize, position, textColor, text);
    }
}
