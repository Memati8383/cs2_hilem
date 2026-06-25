using System.Dynamic;
using System.Net.Http;
using CS2Cheat.DTO.ClientDllDTO;
using CS2Cheat.Utils.DTO;
using Newtonsoft.Json;

namespace CS2Cheat.Utils;

public abstract class Offsets
{
    #region offsets

    public const float WeaponRecoilScale = 2f;
    public const int BoneArrayOffset = 128;
    public static int dwLocalPlayerPawn;
    public static int m_vOldOrigin;
    public static int m_vecViewOffset;
    public static int m_AimPunchAngle;
    public static int m_AimPunchCache;
    public static int m_pAimPunchServices;
    public static int m_vecCsViewPunchAngle;
    public static int m_iActiveIssueIndex;
    public static int m_iOnlyTeamToVote;
    public static int m_nVoteOptionCount;
    public static int m_nPotentialVotes;
    public static int m_modelState;
    public static int m_pGameSceneNode;
    public static int m_fFlags;
    public static int m_iIDEntIndex;
    public static int m_lifeState;
    public static int m_iHealth;
    public static int m_iTeamNum;
    public static int dwEntityList;
    public static int m_bDormant;
    public static int m_iShotsFired;
    public static int m_hPawn;
    public static int dwLocalPlayerController;
    public static int dwViewMatrix;
    public static int dwViewAngles;
    public static int m_entitySpottedState;
    public static int m_bSpotted;
    public static int m_vecAbsOrigin;
    public static int m_hBombDefuser;
    public static int m_Item;
    public static int m_pClippingWeapon;
    public static int m_AttributeManager;
    public static int m_iItemDefinitionIndex;
    public static int m_bIsScoped;
    public static int m_flFlashDuration;
    public static int m_flFlashMaxAlpha;
    public static int m_iszPlayerName;
    public static int dwPlantedC4;
    public static int dwGlobalVars;
    public static int m_nBombSite;
    public static int m_bBombDefused;
    public static int m_bBombTicking;
    public static int m_vecAbsVelocity;
    public static int m_flDefuseCountDown;
    public static int m_flC4Blow;
    public static int m_bBeingDefused;
    public static int m_matchStats;
    public static int m_iKills;
    public static int dwForceJump;
    public static int dwGameRules;
    public static int m_Glow;
    public static int m_bGlowing;
    public static int m_glowColorOverride;
    public static int m_iGlowType;
    public static int m_ArmorValue;
    public static int m_bIsDefusing;
    public static int m_bHasDefuser;
    public static int m_bHasHelmet;
    public static int m_iPing;
    public static int m_pInGameMoneyServices;
    public static int m_iAccount;
    public static int m_iClip1;
    public static int m_iClip2;
    public static int m_bInReload;
    public static int m_pWeaponServices;
    public static int m_hActiveWeapon;
    public static int m_pActionTrackingServices;
    public static int m_unTotalRoundDamageDealt;
    public static int m_iTotalHitsDealt;
    public static int m_pBulletServices;
    public static int m_totalHitsOnServer;
    public static int m_iAmmo;
    public static int m_hObserverTarget;
    public static int m_angEyeAngles;
    public static int m_pObserverServices;
    public static int m_bBombPlanted;
    public static int m_flTimerLength;
    public static int m_flDefuseLength;

    // Matchmaking (for map name)
    public static int dwGameTypes;
    public static int dwGameTypes_mapName;
    public static int dwNetworkGameClient;

    public static readonly Dictionary<string, int> Bones = new()
    {
        { "head", 7 },
        { "neck_0", 6 },
        { "spine_1", 8 },
        { "spine_2", 3 },
        { "pelvis", 1 },
        { "arm_upper_L", 9 },
        { "arm_lower_L", 10 },
        { "hand_L", 11 },
        { "arm_upper_R", 13 },
        { "arm_lower_R", 14 },
        { "hand_R", 15 },
        { "leg_upper_L", 17 },
        { "leg_lower_L", 18 },
        { "ankle_L", 19 },
        { "leg_upper_R", 20 },
        { "leg_lower_R", 21 },
        { "ankle_R", 22 }
    };

    public static async Task UpdateOffsets()
    {
        try
        {
            var offsetsJson = await FetchJson("https://raw.githubusercontent.com/a2x/cs2-dumper/main/output/offsets.json");
            var clientJson = await FetchJson("https://raw.githubusercontent.com/a2x/cs2-dumper/main/output/client_dll.json");
            var buttonsJson = await FetchJson("https://raw.githubusercontent.com/a2x/cs2-dumper/main/output/buttons.json");

            var sourceDataDw = JsonConvert.DeserializeObject<OffsetsDTO>(offsetsJson)!;
            var sourceDataClient = JsonConvert.DeserializeObject<ClientDllDTO>(clientJson)!;

            dynamic rawClient = JsonConvert.DeserializeObject(clientJson)!;
            dynamic buttonsData = JsonConvert.DeserializeObject(buttonsJson)!;

            dynamic destData = new ExpandoObject();

#pragma warning disable CS8602
            destData.dwBuildNumber = sourceDataDw.engine2dll.dwBuildNumber;
            destData.dwLocalPlayerController = sourceDataDw.clientdll.dwLocalPlayerController;
            destData.dwEntityList = sourceDataDw.clientdll.dwEntityList;
            destData.dwViewMatrix = sourceDataDw.clientdll.dwViewMatrix;
            destData.dwPlantedC4 = sourceDataDw.clientdll.dwPlantedC4;
            destData.dwLocalPlayerPawn = sourceDataDw.clientdll.dwLocalPlayerPawn;
            destData.dwViewAngles = sourceDataDw.clientdll.dwViewAngles;
            destData.dwGlobalVars = sourceDataDw.clientdll.dwGlobalVars;
            destData.dwGameRules = sourceDataDw.clientdll.dwGameRules;
            destData.dwForceJump = (int)buttonsData["client.dll"]["jump"];

            destData.dwGameTypes = sourceDataDw.matchmakingdll.dwGameTypes;
            destData.dwGameTypes_mapName = sourceDataDw.matchmakingdll.dwGameTypes_mapName;
            destData.dwNetworkGameClient = sourceDataDw.engine2dll.dwNetworkGameClient;

            destData.m_bBombPlanted = sourceDataClient.clientdll.classes.C_CSGameRules.fields.m_bBombPlanted;
            destData.m_fFlags = sourceDataClient.clientdll.classes.C_BaseEntity.fields.m_fFlags;
            destData.m_vOldOrigin = sourceDataClient.clientdll.classes.C_BasePlayerPawn.fields.m_vOldOrigin;
            destData.m_vecViewOffset =
                sourceDataClient.clientdll.classes.C_BaseModelEntity.fields.m_vecViewOffset;
            destData.m_aimPunchAngle = sourceDataClient.clientdll.classes.C_CSPlayerPawn.fields.m_aimPunchAngle;
            destData.m_aimPunchCache = sourceDataClient.clientdll.classes.C_CSPlayerPawn.fields.m_aimPunchCache;
            destData.m_pAimPunchServices = sourceDataClient.clientdll.classes.C_CSPlayerPawn.fields.m_pAimPunchServices;
            destData.m_vecCsViewPunchAngle = sourceDataClient.clientdll.classes.CPlayer_CameraServices.fields.m_vecCsViewPunchAngle;
            destData.m_modelState = sourceDataClient.clientdll.classes.CSkeletonInstance.fields.m_modelState;
            destData.m_pGameSceneNode = sourceDataClient.clientdll.classes.C_BaseEntity.fields.m_pGameSceneNode;
            destData.m_iIDEntIndex = sourceDataClient.clientdll.classes.C_CSPlayerPawn.fields.m_iIDEntIndex;
            destData.m_lifeState = sourceDataClient.clientdll.classes.C_BaseEntity.fields.m_lifeState;
            destData.m_iHealth = sourceDataClient.clientdll.classes.C_BaseEntity.fields.m_iHealth;
            destData.m_iTeamNum = sourceDataClient.clientdll.classes.C_BaseEntity.fields.m_iTeamNum;
            destData.m_iActiveIssueIndex = sourceDataClient.clientdll.classes.C_VoteController.fields.m_iActiveIssueIndex;
            destData.m_iOnlyTeamToVote = sourceDataClient.clientdll.classes.C_VoteController.fields.m_iOnlyTeamToVote;
            destData.m_nVoteOptionCount = sourceDataClient.clientdll.classes.C_VoteController.fields.m_nVoteOptionCount;
            destData.m_nPotentialVotes = sourceDataClient.clientdll.classes.C_VoteController.fields.m_nPotentialVotes;
            destData.m_bDormant = sourceDataClient.clientdll.classes.CGameSceneNode.fields.m_bDormant;
            destData.m_iShotsFired = sourceDataClient.clientdll.classes.C_CSPlayerPawn.fields.m_iShotsFired;
            destData.m_hPawn = sourceDataClient.clientdll.classes.CBasePlayerController.fields.m_hPawn;
            destData.m_entitySpottedState =
                sourceDataClient.clientdll.classes.C_CSPlayerPawn.fields.m_entitySpottedState;
            destData.m_Item = sourceDataClient.clientdll.classes.C_AttributeContainer.fields.m_Item;
            destData.m_pClippingWeapon =
                sourceDataClient.clientdll.classes.C_CSPlayerPawnBase.fields.m_pClippingWeapon;
            destData.m_AttributeManager =
                sourceDataClient.clientdll.classes.C_EconEntity.fields.m_AttributeManager;
            destData.m_iItemDefinitionIndex =
                sourceDataClient.clientdll.classes.C_EconItemView.fields.m_iItemDefinitionIndex;
            destData.m_bSpotted = sourceDataClient.clientdll.classes.EntitySpottedState_t.fields.m_bSpotted;
            destData.m_vecAbsOrigin = sourceDataClient.clientdll.classes.CGameSceneNode.fields.m_vecAbsOrigin;
            destData.m_hBombDefuser = sourceDataClient.clientdll.classes.C_PlantedC4.fields.m_hBombDefuser;
            destData.m_bIsScoped = sourceDataClient.clientdll.classes.C_CSPlayerPawnBase.fields.m_bIsScoped;
            destData.m_flFlashDuration =
                sourceDataClient.clientdll.classes.C_CSPlayerPawnBase.fields.m_flFlashDuration;
            destData.m_iszPlayerName =
                sourceDataClient.clientdll.classes.CBasePlayerController.fields.m_iszPlayerName;
            destData.m_flFlashMaxAlpha =
                sourceDataClient.clientdll.classes.C_CSPlayerPawnBase.fields.m_flFlashMaxAlpha;
            destData.m_nBombSite = sourceDataClient.clientdll.classes.C_PlantedC4.fields.m_nBombSite;
            destData.m_bBombDefused = sourceDataClient.clientdll.classes.C_PlantedC4.fields.m_bBombDefused;
            destData.m_bBombTicking = sourceDataClient.clientdll.classes.C_PlantedC4.fields.m_bBombTicking;
            destData.m_vecAbsVelocity =
                sourceDataClient.clientdll.classes.C_BaseEntity.fields.m_vecAbsVelocity;
            destData.m_flDefuseCountDown =
                sourceDataClient.clientdll.classes.C_PlantedC4.fields.m_flDefuseCountDown;
            destData.m_flC4Blow = sourceDataClient.clientdll.classes.C_PlantedC4.fields.m_flC4Blow;
            destData.m_flTimerLength = sourceDataClient.clientdll.classes.C_PlantedC4.fields.m_flTimerLength;
            destData.m_flDefuseLength = sourceDataClient.clientdll.classes.C_PlantedC4.fields.m_flDefuseLength;
            destData.m_bBeingDefused = sourceDataClient.clientdll.classes.C_PlantedC4.fields.m_bBeingDefused;
            destData.m_matchStats = sourceDataClient.clientdll.classes.CCSPlayerController.fields.m_matchStats;
            destData.m_iKills = sourceDataClient.clientdll.classes.CSMatchStats_t.fields.m_iKills;
            destData.m_Glow = sourceDataClient.clientdll.classes.C_BaseModelEntity.fields.m_Glow;
            destData.m_bGlowing = sourceDataClient.clientdll.classes.CGlowProperty.fields.m_bGlowing;
            destData.m_glowColorOverride = sourceDataClient.clientdll.classes.CGlowProperty.fields.m_glowColorOverride;
            destData.m_iGlowType = sourceDataClient.clientdll.classes.CGlowProperty.fields.m_iGlowType;

            destData.m_ArmorValue = sourceDataClient.clientdll.classes.C_CSPlayerPawnBase.fields.m_ArmorValue;
            destData.m_bIsDefusing = sourceDataClient.clientdll.classes.C_CSPlayerPawnBase.fields.m_bIsDefusing;
            destData.m_bHasDefuser = sourceDataClient.clientdll.classes.C_CSPlayerPawnBase.fields.m_bHasDefuser;
            destData.m_bHasHelmet = sourceDataClient.clientdll.classes.C_CSPlayerPawnBase.fields.m_bHasHelmet;
            destData.m_pWeaponServices = sourceDataClient.clientdll.classes.C_CSPlayerPawnBase.fields.m_pWeaponServices;
            destData.m_pBulletServices = sourceDataClient.clientdll.classes.C_CSPlayerPawn.fields.m_pBulletServices;
            destData.m_pObserverServices = sourceDataClient.clientdll.classes.C_CSPlayerPawnBase.fields.m_pObserverServices;
            destData.m_iPing = sourceDataClient.clientdll.classes.CCSPlayerController.fields.m_iPing;
            destData.m_pInGameMoneyServices = sourceDataClient.clientdll.classes.CCSPlayerController.fields.m_pInGameMoneyServices;
            destData.m_pActionTrackingServices = sourceDataClient.clientdll.classes.CCSPlayerController.fields.m_pActionTrackingServices;
            destData.m_unTotalRoundDamageDealt =
                (int)rawClient["client.dll"]["classes"]["CCSPlayerController_ActionTrackingServices"]["fields"]["m_flTotalRoundDamageDealt"];
            destData.m_iAccount = sourceDataClient.clientdll.classes.CCSPlayerController_InGameMoneyServices.fields.m_iAccount;
            destData.m_iClip1 = sourceDataClient.clientdll.classes.C_BasePlayerWeapon.fields.m_iClip1;
            destData.m_iClip2 = sourceDataClient.clientdll.classes.C_BasePlayerWeapon.fields.m_iClip2;
            destData.m_bInReload = sourceDataClient.clientdll.classes.C_CSWeaponBase.fields.m_bInReload;
            destData.m_iAmmo = sourceDataClient.clientdll.classes.CPlayer_WeaponServices.fields!.m_iAmmo;
            destData.m_hActiveWeapon = sourceDataClient.clientdll.classes.CPlayer_WeaponServices.fields!.m_hActiveWeapon;
            destData.m_totalHitsOnServer = sourceDataClient.clientdll.classes.CCSPlayer_BulletServices.fields!.m_totalHitsOnServer;
            destData.m_hObserverTarget = sourceDataClient.clientdll.classes.CPlayer_ObserverServices.fields!.m_hObserverTarget;
            destData.m_angEyeAngles = sourceDataClient.clientdll.classes.C_CSPlayerPawnBase.fields.m_angEyeAngles;
#pragma warning restore CS8602

            UpdateStaticFields(destData);
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"An error occurred: {ex.Message}");
            throw;
        }
    }

    private static readonly HttpClient HttpClientInstance = new();

    private static async Task<string> FetchJson(string url)
    {
        return await HttpClientInstance.GetStringAsync(url);
    }

    private static void UpdateStaticFields(dynamic data)
    {
        dwLocalPlayerPawn = data.dwLocalPlayerPawn;
        m_vOldOrigin = data.m_vOldOrigin;
        m_vecViewOffset = data.m_vecViewOffset;
        m_AimPunchAngle = data.m_aimPunchAngle;
        m_AimPunchCache = data.m_aimPunchCache;
        m_pAimPunchServices = data.m_pAimPunchServices;
        m_vecCsViewPunchAngle = data.m_vecCsViewPunchAngle;
        m_modelState = data.m_modelState;
        m_pGameSceneNode = data.m_pGameSceneNode;
        m_iIDEntIndex = data.m_iIDEntIndex;
        m_lifeState = data.m_lifeState;
        m_iHealth = data.m_iHealth;
        m_iTeamNum = data.m_iTeamNum;
        m_iActiveIssueIndex = data.m_iActiveIssueIndex;
        m_iOnlyTeamToVote = data.m_iOnlyTeamToVote;
        m_nVoteOptionCount = data.m_nVoteOptionCount;
        m_nPotentialVotes = data.m_nPotentialVotes;
        m_bDormant = data.m_bDormant;
        m_iShotsFired = data.m_iShotsFired;
        m_hPawn = data.m_hPawn;
        m_fFlags = data.m_fFlags;
        dwLocalPlayerController = data.dwLocalPlayerController;
        dwViewMatrix = data.dwViewMatrix;
        dwViewAngles = data.dwViewAngles;
        dwEntityList = data.dwEntityList;
        m_entitySpottedState = data.m_entitySpottedState;
        m_bSpotted = data.m_bSpotted;
        m_vecAbsOrigin = data.m_vecAbsOrigin;
        m_hBombDefuser = data.m_hBombDefuser;
        m_Item = data.m_Item;
        m_pClippingWeapon = data.m_pClippingWeapon;
        m_AttributeManager = data.m_AttributeManager;
        m_iItemDefinitionIndex = data.m_iItemDefinitionIndex;
        m_bIsScoped = data.m_bIsScoped;
        m_flFlashDuration = data.m_flFlashDuration;
        m_flFlashMaxAlpha = data.m_flFlashMaxAlpha;
        m_iszPlayerName = data.m_iszPlayerName;
        dwPlantedC4 = data.dwPlantedC4;
        dwGlobalVars = data.dwGlobalVars;
        m_nBombSite = data.m_nBombSite;
        m_bBombDefused = data.m_bBombDefused;
        m_bBombTicking = data.m_bBombTicking;
        m_vecAbsVelocity = data.m_vecAbsVelocity;
        m_flDefuseCountDown = data.m_flDefuseCountDown;
        m_flC4Blow = data.m_flC4Blow;
        m_flTimerLength = data.m_flTimerLength;
        m_flDefuseLength = data.m_flDefuseLength;
        m_bBeingDefused = data.m_bBeingDefused;
        m_matchStats = data.m_matchStats;
        m_iKills = data.m_iKills;
        dwForceJump = data.dwForceJump;
        dwGameRules = data.dwGameRules;
        m_Glow = data.m_Glow;
        m_bGlowing = data.m_bGlowing;
        m_glowColorOverride = data.m_glowColorOverride;
        m_iGlowType = data.m_iGlowType;
        m_ArmorValue = data.m_ArmorValue;
        m_bIsDefusing = data.m_bIsDefusing;
        m_bHasDefuser = data.m_bHasDefuser;
        m_bHasHelmet = data.m_bHasHelmet;
        m_iPing = data.m_iPing;
        m_pInGameMoneyServices = data.m_pInGameMoneyServices;
        m_pActionTrackingServices = data.m_pActionTrackingServices;
        m_unTotalRoundDamageDealt = data.m_unTotalRoundDamageDealt;
        m_iAccount = data.m_iAccount;
        m_iClip1 = data.m_iClip1;
        m_iClip2 = data.m_iClip2;
        m_bInReload = data.m_bInReload;
        m_pWeaponServices = data.m_pWeaponServices;
        m_hActiveWeapon = data.m_hActiveWeapon;
        m_pBulletServices = data.m_pBulletServices;
        m_totalHitsOnServer = data.m_totalHitsOnServer;
        m_pObserverServices = data.m_pObserverServices;
        m_iAmmo = data.m_iAmmo;
        m_hObserverTarget = data.m_hObserverTarget;
        m_angEyeAngles = data.m_angEyeAngles;
        dwGameTypes = data.dwGameTypes;
        dwGameTypes_mapName = data.dwGameTypes_mapName;
        dwNetworkGameClient = data.dwNetworkGameClient;
        m_bBombPlanted = data.m_bBombPlanted;
        m_flTimerLength = data.m_flTimerLength;
        m_flDefuseLength = data.m_flDefuseLength;
    }

    #endregion

}
