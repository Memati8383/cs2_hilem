using System.Numerics;
using CS2Cheat.Core.Data;
using CS2Cheat.Data.Entity;
using CS2Cheat.Data.Game;
using CS2Cheat.Graphics;
using CS2Cheat.Utils;
using ImGuiNET;

namespace CS2Cheat.Features;

public static class EspBox
{
    private static readonly Dictionary<string, string> GunIcons = new()
    {
        ["knife_ct"] = "]", ["knife_t"] = "[", ["deagle"] = "A", ["elite"] = "B",
        ["fiveseven"] = "C", ["glock"] = "D", ["revolver"] = "J", ["hkp2000"] = "E",
        ["p250"] = "F", ["usp_silencer"] = "G", ["tec9"] = "H", ["cz75a"] = "I",
        ["mac10"] = "K", ["ump45"] = "L", ["bizon"] = "M", ["mp7"] = "N",
        ["mp9"] = "R", ["p90"] = "O", ["galilar"] = "Q", ["famas"] = "R",
        ["m4a1_silencer"] = "T", ["m4a1"] = "S", ["aug"] = "U", ["sg556"] = "V",
        ["ak47"] = "W", ["g3sg1"] = "X", ["scar20"] = "Y", ["awp"] = "Z",
        ["ssg08"] = "a", ["xm1014"] = "b", ["sawedoff"] = "c", ["mag7"] = "d",
        ["nova"] = "e", ["negev"] = "f", ["m249"] = "g", ["taser"] = "h",
        ["flashbang"] = "i", ["hegrenade"] = "j", ["smokegrenade"] = "k",
        ["molotov"] = "l", ["decoy"] = "m", ["incgrenade"] = "n", ["c4"] = "o"
    };

    private static ConfigManager Config => ConfigManager.Load();

    public static void Draw(ImDrawListPtr drawList, GameData gameData)
    {
        var player = gameData.Player;
        if (player == null || gameData.Entities == null) return;

        foreach (var entity in gameData.Entities)
        {
            if (!entity.IsAlive() || entity.AddressBase == player.AddressBase) continue;
            if (Config.TeamCheck && entity.Team == player.Team) continue;
            if (Config.EspSpottedOnly && !entity.IsSpotted) continue;

            var boundingBox = GetEntityBoundingBox(player, entity);
            if (boundingBox == null) continue;

            var isEnemy = entity.Team != player.Team;
            var colorArr = isEnemy ? Config.EspBoxColor : Config.EspBoxColorTeam;
            var colorBox = OverlayRenderer.ToColor(new Vector4(colorArr[0], colorArr[1], colorArr[2], colorArr[3]));

            DrawEntityInfo(drawList, player, entity, colorBox, boundingBox.Value, isEnemy);
        }
    }

    private static void DrawEntityInfo(ImDrawListPtr drawList, Player player, Entity entity, uint color,
        (Vector2, Vector2) boundingBox, bool isEnemy)
    {
        var textColor = Config.EspTextRainbow 
            ? OverlayRenderer.GetRainbowColor() 
            : OverlayRenderer.ToColor(new Vector4(Config.EspTextColor[0], Config.EspTextColor[1], Config.EspTextColor[2], Config.EspTextColor[3]));

        var (topLeft, bottomRight) = boundingBox;
        if (topLeft.X > bottomRight.X || topLeft.Y > bottomRight.Y) return;

        var healthPercentage = Math.Clamp(entity.Health / 100f, 0f, 1f);
        var boxWidth = bottomRight.X - topLeft.X;
        var boxHeight = bottomRight.Y - topLeft.Y;

        // Box ESP
        if (Config.EspBox)
        {
            if (Config.EspBoxCorner)
                DrawCornerBox(drawList, topLeft, bottomRight, color, 1.5f, 8f);
            else
                drawList.AddRect(topLeft, bottomRight, color, 0, ImDrawFlags.None, 1.5f);
        }

        // Health Bar & Number
        if (Config.EspHealthBar)
        {
            var healthBarLeft = topLeft.X - 10f;
            var healthBarTopLeft = new Vector2(healthBarLeft, topLeft.Y);
            var healthBarBottomRight = new Vector2(healthBarLeft + 6f, bottomRight.Y);
            DrawHealthBar(drawList, healthBarTopLeft, healthBarBottomRight, healthPercentage);

            var healthText = entity.Health.ToString();
            var textWidth = ImGui.CalcTextSize(healthText).X;
            var textHeight = ImGui.CalcTextSize(healthText).Y;
            var healthX = healthBarLeft - textWidth - 4f;
            var healthY = topLeft.Y + boxHeight * (1 - healthPercentage) - textHeight * 0.5f;
            var hpColor = entity.Health > 50 ? OverlayRenderer.ToColor(100, 255, 100) :
                          entity.Health > 25 ? OverlayRenderer.ToColor(255, 255, 100) :
                          OverlayRenderer.ToColor(255, 80, 80);
            DrawOutlinedText(drawList, new Vector2(healthX, healthY), hpColor, healthText);
        }

        // Armor Bar
        if (Config.EspArmorBar)
        {
            var armorPercent = Math.Clamp(entity.Armor / 100f, 0f, 1f);
            var armorBarLeft = topLeft.X - 10f;
            var armorBarTopLeft = new Vector2(armorBarLeft, topLeft.Y - 8f);
            var armorBarBottomRight = new Vector2(armorBarLeft + 6f, topLeft.Y - 2f);
            DrawArmorBar(drawList, armorBarTopLeft, armorBarBottomRight, armorPercent, entity.HasHelmet);
        }

        // Head Tracker
        if (Config.EspHeadDot)
        {
            if (entity.BonePos != null && entity.BonePos.TryGetValue("head", out var headPos3D))
            {
                var headPos2D = player.MatrixViewProjectionViewport.Transform(headPos3D);
                if (headPos2D.Z < 1 && headPos2D.X >= 0 && headPos2D.Y >= 0)
                {
                    drawList.AddCircleFilled(new Vector2(headPos2D.X, headPos2D.Y), 4f, OverlayRenderer.ToColor(255, 0, 0, 255));
                    drawList.AddCircle(new Vector2(headPos2D.X, headPos2D.Y), 4f, OverlayRenderer.Colors.White, 0, 1f);
                }
            }
        }

        // Weapon Name & Icon
        var weaponName = FormatWeaponName(entity);
        var weaponIcon = GetWeaponIcon(entity.CurrentWeaponName);

        if (Config.EspWeapon && !string.IsNullOrEmpty(weaponName))
        {
            var weaponX = (topLeft.X + bottomRight.X) / 2 - weaponName.Length * 3.5f;
            var weaponY = bottomRight.Y + 4;
            DrawOutlinedText(drawList, new Vector2(weaponX, weaponY), textColor, weaponName);
        }

        if (Config.EspWeaponIcon && !string.IsNullOrEmpty(weaponIcon))
        {
            var iconY = bottomRight.Y + 4;
            if (Config.EspWeapon) iconY += 16;
            var iconX = (topLeft.X + bottomRight.X) / 2;
            drawList.AddText(new Vector2(iconX, iconY), textColor, weaponIcon);
        }

        // Name & Distance
        if (Config.EspName || Config.EspDistance)
        {
            var name = "";
            if (Config.EspName)
            {
                name = entity.Name ?? Language.Get("flag_unknown");
                if (entity.Name != null && entity.Name.Contains("bot", StringComparison.OrdinalIgnoreCase))
                    name += " (" + Language.Get("flag_bot") + ")";
            }
            if (Config.EspDistance)
            {
                float dist = TacticalManager.GetDistanceInMeters(player.Origin, entity.Origin);
                name += string.IsNullOrEmpty(name) ? $"[{TacticalManager.FormatDistance(dist)}]" : $" [{TacticalManager.FormatDistance(dist)}]";
            }
            var nameX = (topLeft.X + bottomRight.X) / 2 - name.Length * 3;
            var nameY = topLeft.Y - 15f;
            DrawOutlinedText(drawList, new Vector2(nameX, nameY), textColor, name);
        }

        // Flags (right side)
        if (Config.EspFlags)
        {
            var flagX = bottomRight.X + 5f;
            var flagY = topLeft.Y;
            var spacing = 14;

            // Ammo
            if (Config.EspAmmo && entity.Ammo >= 0)
            {
                DrawOutlinedText(drawList, new Vector2(flagX, flagY), textColor, $"{Language.Get("flag_ammo")}: {entity.Ammo}");
                flagY += spacing;
            }

            // Money
            if (Config.EspMoney)
            {
                DrawOutlinedText(drawList, new Vector2(flagX, flagY), OverlayRenderer.ToColor(100, 255, 100), $"${entity.Money}");
                flagY += spacing;
            }

            // Ping
            if (Config.EspPing)
            {
                var pingColor = entity.Ping < 50 ? OverlayRenderer.Colors.Green :
                                entity.Ping < 100 ? OverlayRenderer.ToColor(255, 255, 0) :
                                OverlayRenderer.ToColor(255, 50, 50);
                DrawOutlinedText(drawList, new Vector2(flagX, flagY), pingColor, $"{entity.Ping}ms");
                flagY += spacing;
            }

            // Scoped
            if (entity.IsInScope == 1 || entity.IsScoped)
            {
                DrawOutlinedText(drawList, new Vector2(flagX, flagY), OverlayRenderer.ToColor(0, 191, 255), Language.Get("flag_scoped"));
                flagY += spacing;
            }

            // Flashed
            if (entity.FlashAlpha > 0)
            {
                DrawOutlinedText(drawList, new Vector2(flagX, flagY), OverlayRenderer.ToColor(255, 255, 0), Language.Get("flag_flashed"));
                flagY += spacing;
            }

            // Reloading
            if (Config.EspReloading && entity.IsReloading)
            {
                DrawOutlinedText(drawList, new Vector2(flagX, flagY), OverlayRenderer.ToColor(255, 165, 0), Language.Get("flag_reloading"));
                flagY += spacing;
            }

            // Defusing
            if (Config.EspDefusing && entity.IsDefusing)
            {
                DrawOutlinedText(drawList, new Vector2(flagX, flagY), OverlayRenderer.ToColor(0, 255, 255), Language.Get("flag_defusing"));
                flagY += spacing;
            }

            // Has Defuser
            if (entity.HasDefuser)
            {
                DrawOutlinedText(drawList, new Vector2(flagX, flagY), OverlayRenderer.ToColor(100, 200, 255), Language.Get("flag_kit"));
                flagY += spacing;
            }

            // Heavy armor
            if (entity.HasHelmet && entity.Armor > 0)
            {
                DrawOutlinedText(drawList, new Vector2(flagX, flagY), OverlayRenderer.ToColor(200, 200, 200), Language.Get("flag_helmet"));
            }
        }

        // Snaplines (Tracers)
        if (Config.EspSnaplines)
        {
            var io = ImGui.GetIO();
            var screenBottom = new Vector2(io.DisplaySize.X / 2, io.DisplaySize.Y);
            var targetPos = new Vector2((topLeft.X + bottomRight.X) / 2, bottomRight.Y);
            drawList.AddLine(screenBottom, targetPos, color, 1.5f);
        }
    }

    private static void DrawCornerBox(ImDrawListPtr drawList, Vector2 topLeft, Vector2 bottomRight, uint color, float thickness, float cornerLen)
    {
        var w = bottomRight.X - topLeft.X;
        var h = bottomRight.Y - topLeft.Y;
        var cl = Math.Min(cornerLen, Math.Min(w, h) * 0.3f);

        // Top-left
        drawList.AddLine(topLeft, new Vector2(topLeft.X + cl, topLeft.Y), color, thickness);
        drawList.AddLine(topLeft, new Vector2(topLeft.X, topLeft.Y + cl), color, thickness);
        // Top-right
        drawList.AddLine(new Vector2(bottomRight.X, topLeft.Y), new Vector2(bottomRight.X - cl, topLeft.Y), color, thickness);
        drawList.AddLine(new Vector2(bottomRight.X, topLeft.Y), new Vector2(bottomRight.X, topLeft.Y + cl), color, thickness);
        // Bottom-left
        drawList.AddLine(new Vector2(topLeft.X, bottomRight.Y), new Vector2(topLeft.X + cl, bottomRight.Y), color, thickness);
        drawList.AddLine(new Vector2(topLeft.X, bottomRight.Y), new Vector2(topLeft.X, bottomRight.Y - cl), color, thickness);
        // Bottom-right
        drawList.AddLine(bottomRight, new Vector2(bottomRight.X - cl, bottomRight.Y), color, thickness);
        drawList.AddLine(bottomRight, new Vector2(bottomRight.X, bottomRight.Y - cl), color, thickness);
    }

    private static void DrawHealthBar(ImDrawListPtr drawList, Vector2 topLeft, Vector2 bottomRight,
        float healthPercentage)
    {
        var totalHeight = bottomRight.Y - topLeft.Y;
        var filledHeight = totalHeight * healthPercentage;
        var filledTop = new Vector2(topLeft.X, Math.Max(bottomRight.Y - filledHeight, topLeft.Y));

        drawList.AddRectFilled(topLeft, bottomRight, OverlayRenderer.Colors.Black);
        var healthColor = GetHealthColor(healthPercentage);
        drawList.AddRectFilled(filledTop, bottomRight, healthColor);
        drawList.AddRect(topLeft, bottomRight, OverlayRenderer.Colors.DarkGray);
    }

    private static void DrawArmorBar(ImDrawListPtr drawList, Vector2 topLeft, Vector2 bottomRight,
        float armorPercent, bool hasHelmet)
    {
        drawList.AddRectFilled(topLeft, bottomRight, OverlayRenderer.Colors.Black);
        var filledWidth = (bottomRight.X - topLeft.X) * armorPercent;
        var fillEnd = new Vector2(topLeft.X + filledWidth, bottomRight.Y);
        var armorColor = hasHelmet ? OverlayRenderer.ToColor(0, 150, 255) : OverlayRenderer.ToColor(200, 200, 50);
        drawList.AddRectFilled(topLeft, fillEnd, armorColor);
        drawList.AddRect(topLeft, bottomRight, OverlayRenderer.Colors.DarkGray);
    }

    private static uint GetHealthColor(float percentage)
    {
        var r = (byte)(percentage < 0.5f ? 255 : (int)(255 * (1 - percentage) * 2));
        var g = (byte)(percentage > 0.5f ? 255 : (int)(255 * percentage * 2));
        return OverlayRenderer.ToColor(r, g, 0);
    }

    private static string GetWeaponIcon(string weapon)
    {
        return GunIcons.TryGetValue(weapon?.ToLower() ?? string.Empty, out var icon) ? icon : string.Empty;
    }

    private static string FormatWeaponName(Entity entity)
    {
        var weapon = entity.CurrentWeaponName;
        if (string.IsNullOrWhiteSpace(weapon)) return string.Empty;
        return weapon.Replace("Silencer", "-S", StringComparison.OrdinalIgnoreCase).ToUpperInvariant();
    }

    private static void DrawOutlinedText(ImDrawListPtr drawList, Vector2 position, uint color, string text)
    {
        drawList.AddText(position + new Vector2(1, 1), OverlayRenderer.Colors.Black, text);
        drawList.AddText(position, color, text);
    }

    private static (Vector2, Vector2)? GetEntityBoundingBox(Player player, Entity entity)
    {
        const float padding = 5.0f;
        var minPos = new Vector2(float.MaxValue, float.MaxValue);
        var maxPos = new Vector2(float.MinValue, float.MinValue);

        var matrix = player.MatrixViewProjectionViewport;
        if (entity.BonePos == null || entity.BonePos.Count == 0) return null;

        var anyValid = false;
        foreach (var bone in entity.BonePos.Values)
        {
            var transformed = matrix.Transform(bone);
            if (transformed.Z >= 1) continue;

            anyValid = true;
            minPos.X = Math.Min(minPos.X, transformed.X);
            minPos.Y = Math.Min(minPos.Y, transformed.Y);
            maxPos.X = Math.Max(maxPos.X, transformed.X);
            maxPos.Y = Math.Max(maxPos.Y, transformed.Y);
        }

        if (!anyValid) return null;

        var paddingVector = new Vector2(padding);
        return (minPos - paddingVector, maxPos + paddingVector);
    }
}