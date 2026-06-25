using System.Collections.Concurrent;
using System.Numerics;
using CS2Cheat.Data.Game;
using CS2Cheat.Graphics;
using CS2Cheat.Utils;

namespace CS2Cheat.Data.Entity;

public class Entity : EntityBase
{
    private readonly ConcurrentDictionary<string, Vector3> _bonePositions;
    private bool _dormant = true;

    public Entity(int index)
    {
        Id = index;
        _bonePositions = new ConcurrentDictionary<string, Vector3>(Offsets.Bones.ToDictionary(
            bone => bone.Key,
            _ => Vector3.Zero
        ));
    }

    public bool IsSpotted { get; private set; }
    public int ObserverTarget { get; private set; }
    public Vector3 ViewAngles { get; private set; }
    public Vector3 EyeDirection { get; private set; }
    protected internal string Name { get; private set; } = string.Empty;
    public int IsInScope { get; private set; }
    protected internal float FlashAlpha { get; private set; }
    public IReadOnlyDictionary<string, Vector3> BonePos => _bonePositions;
    public int Id { get; }

    public override bool IsAlive()
    {
        return base.IsAlive() && !_dormant;
    }

    protected override IntPtr ReadControllerBase(GameProcess gameProcess)
    {
        if (gameProcess?.Process == null) return IntPtr.Zero;

        var entityListBase = gameProcess.Process.Read<IntPtr>(EntityList + 16);
        return entityListBase != IntPtr.Zero
            ? gameProcess.Process.Read<IntPtr>(entityListBase + (Id + 1) * 112)
            : IntPtr.Zero;
    }

    protected override IntPtr ReadAddressBase(GameProcess gameProcess)
    {
        if (gameProcess?.Process == null) return IntPtr.Zero;

        var playerPawn = gameProcess.Process.Read<int>(ControllerBase + Offsets.m_hPawn);
        if (playerPawn == -1) return IntPtr.Zero;
        var pawnIndex = (playerPawn & 0x7FFF) >> 9;
        var listEntry = gameProcess.Process.Read<IntPtr>(EntityList + 8 * pawnIndex + 16);

        return listEntry != IntPtr.Zero
            ? gameProcess.Process.Read<IntPtr>(listEntry + 112 * (playerPawn & 0x1FF))
            : IntPtr.Zero;
    }

    public override bool Update(GameProcess gameProcess)
    {
        if (!base.Update(gameProcess)) return false;

        if (AddressBase != IntPtr.Zero)
        {
            _dormant = gameProcess.Process != null && gameProcess.Process.Read<bool>(AddressBase + Offsets.m_bDormant);
            IsSpotted = gameProcess.Process?.Read<bool>(AddressBase + Offsets.m_entitySpottedState + Offsets.m_bSpotted) ?? false;
            if (gameProcess.Process != null)
            {
                var observerServices = gameProcess.Process.Read<IntPtr>(AddressBase + Offsets.m_pObserverServices);
                ObserverTarget = observerServices != IntPtr.Zero
                    ? gameProcess.Process.Read<int>(observerServices + Offsets.m_hObserverTarget)
                    : 0;
            }
            else { ObserverTarget = 0; }
            if (gameProcess.Process != null)
            {
                var eyeAngles = gameProcess.Process.Read<Vector3>(AddressBase + Offsets.m_angEyeAngles);
                ViewAngles = eyeAngles;
                EyeDirection = GraphicsMath.GetVectorFromEulerAngles(
                    eyeAngles.X.DegreeToRadian(), eyeAngles.Y.DegreeToRadian());
            }
            IsInScope = gameProcess.Process?.Read<int>(AddressBase + Offsets.m_bIsScoped) ?? 0;
            FlashAlpha = gameProcess.Process?.Read<float>(AddressBase + Offsets.m_flFlashDuration) ?? 0f;
        }

        Name = gameProcess.Process != null
            ? gameProcess.Process.ReadString(ControllerBase + Offsets.m_iszPlayerName)
            : string.Empty;

        return !IsAlive() || UpdateBonePositions(gameProcess);
    }

    private bool UpdateBonePositions(GameProcess gameProcess)
    {
        try
        {
            if (gameProcess?.Process == null) return false;

            var gameSceneNode = gameProcess.Process.Read<IntPtr>(AddressBase + Offsets.m_pGameSceneNode);
            var boneArray = gameProcess.Process.Read<IntPtr>(gameSceneNode + Offsets.m_modelState + Offsets.BoneArrayOffset);

            foreach (var (boneName, boneIndex) in Offsets.Bones)
            {
                var bonePos = gameProcess.Process.Read<Vector3>(boneArray + boneIndex * 32);
                _bonePositions.AddOrUpdate(boneName, bonePos, (_, _) => bonePos);
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

}
