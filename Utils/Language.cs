using System.Collections.Generic;

namespace CS2Cheat.Utils;

public static class Language
{
    public static string Current { get; set; } = "Türkçe";
    public static string[] Available => _langs.Keys.ToArray();

    public static string Get(string key)
    {
        if (_langs.TryGetValue(Current, out var dict) && dict.TryGetValue(key, out var val))
            return val;
        return key;
    }

    static readonly Dictionary<string, Dictionary<string, string>> _langs = new()
    {
        ["Türkçe"] = new()
        {
            // Tab labels
            ["tab_dashboard"] = "PANO",
            ["tab_aimbot"] = "SİLAH",
            ["tab_visuals"] = "GÖRSELLER",
            ["tab_misc"] = "DİĞER",
            ["tab_settings"] = "AYARLAR",

            // Sidebar
            ["sidebar_title"] = "MematiHack",
            ["sidebar_subtitle"] = "External v2.0",
            ["sidebar_connected"] = "Bağlı",

            // Content header
            ["header_subtitle"] = "MematiHack External",

            // Dashboard
            ["dashboard_title"] = "MematiHack External",
            ["dashboard_subtitle"] = "Counter-Strike 2  |  v2.0",
            ["card_status"] = "DURUM",
            ["card_fps"] = "FPS",
            ["card_features"] = "ÖZELLİKLER",
            ["card_active"] = "Aktif",
            ["features_header"] = "AKTİF ÖZELLİKLER",
            ["quick_actions"] = "HIZLI İŞLEMLER",

            // Social
            ["social_instagram"] = "Instagram",
            ["social_github"] = "GitHub",
            ["social_source"] = "Kaynak Kod",

            // Aimbot tabs
            ["aimbot_help"] = "Yardım",
            ["aimbot_settings"] = "Ayarlar",
            ["aimbot_recoil"] = "Rekoil",

            // Aimbot controls
            ["checkbox_aimbot"] = "Aimbot Ustası",
            ["checkbox_fov_circle"] = "FOV Çemberi Çiz",
            ["checkbox_dynamic_fov"] = "Dinamik FOV",
            ["checkbox_rcs"] = "RCS'yi Etkinleştir",
            ["slider_fov"] = "Görüş Alanı",
            ["slider_smoothing"] = "Yumuşatma Hızı",
            ["slider_rcs_strength"] = "RCS Gücü",
            ["combo_bone"] = "Hedef Kemiği",

            // Visuals tabs
            ["visuals_player"] = "Oyuncu ESP",
            ["visuals_world"] = "Dünya",
            ["visuals_effects"] = "Efektler",
            ["visuals_colors"] = "Renkler",

            // Visuals - Player ESP
            ["checkbox_box_esp"] = "Kutu ESP",
            ["checkbox_name_esp"] = "İsim ESP",
            ["checkbox_health_bar"] = "Can Barı",
            ["checkbox_armor_bar"] = "Zırh Barı",
            ["checkbox_distance"] = "Mesafe",
            ["checkbox_weapon"] = "Silah",
            ["checkbox_money"] = "Para",
            ["checkbox_ammo"] = "Mermi",
            ["checkbox_skeleton"] = "İskelet",
            ["checkbox_snaplines"] = "Çizgiler",
            ["checkbox_visible_only"] = "Sadece Görünen",

            // Visuals - World
            ["checkbox_crosshair"] = "Nişangah",
            ["checkbox_flash_protection"] = "Flash Koruması",
            ["checkbox_glow_esp"] = "Parlama ESP",
            ["checkbox_health_based"] = "Can Bazlı",
            ["checkbox_offscreen_arrows"] = "FOV Dışı Oklar",
            ["checkbox_radar"] = "2D Radar",
            ["checkbox_item_esp"] = "Eşya ESP",
            ["checkbox_bomb_timer"] = "Bomba Zamanlayıcı",
            ["checkbox_spectator_list"] = "İzleyici Listesi",
            ["slider_radar_zoom"] = "Radar Yakınlaştırma",
            ["combo_glow_style"] = "Parlama Stili",
            ["glow_default"] = "Varsayılan",
            ["glow_pulse"] = "Nabız",
            ["glow_outline"] = "Dış Hat",
            ["glow_solid"] = "Düz",
            ["color_enemy_glow"] = "Düşman Parlaması",
            ["color_team_glow"] = "Takım Parlaması",

            // Visuals - Effects
            ["checkbox_hitmarker"] = "Hitmarker'ı Etkinleştir",
            ["checkbox_hit_sound"] = "Hit Sesini Etkinleştir",
            ["checkbox_damage_text"] = "Hasar Yazısı",
            ["slider_volume"] = "Ses Seviyesi",
            ["combo_hit_sound"] = "Hit Sesi",
            ["btn_test_sound"] = "SESİ TEST ET",
            ["slider_size"] = "Boyut",
            ["slider_gap"] = "Aralık",
            ["slider_duration_ms"] = "Süre (ms)",
            ["slider_thickness"] = "Kalınlık",
            ["color_pick"] = "Renk",
            ["slider_text_size"] = "Yazı Boyutu",
            ["color_text_color"] = "Yazı Rengi",

            // Visuals - Colors
            ["color_enemy_box"] = "Düşman Kutusu",
            ["color_team_box"] = "Takım Kutusu",
            ["color_esp_text"] = "ESP Yazısı",
            ["checkbox_esp_rainbow"] = "ESP Gökkuşağı",
            ["color_watermark"] = "Filigran",
            ["checkbox_watermark_rainbow"] = "Filigran Gökkuşağı",
            ["color_bomb_panel"] = "Bomba Paneli",
            ["color_bomb_text"] = "Bomba Yazısı",
            ["color_bomb_marker"] = "Bomba İşareti",
            ["checkbox_bomb_rainbow"] = "Bomba Gökkuşağı",

            // Misc tabs
            ["misc_movement"] = "Hareket",
            ["misc_tools"] = "Araçlar",
            ["misc_grenades"] = "El Bombaları",
            ["misc_interface"] = "Arayüz",

            // Misc - Movement
            ["checkbox_bunnyhop"] = "Otomatik Zıplama",
            ["checkbox_velocity_graph"] = "Hız Grafiği",

            // Misc - Tools
            ["checkbox_triggerbot"] = "Tetik Botu",
            ["checkbox_team_check"] = "Takım Kontrolü",
            ["checkbox_vote_teller"] = "Oyu Göster",

            // Misc - Grenades
            ["checkbox_grenade_helper"] = "Bomba Yardımcısını Etkinleştir",
            ["grenade_detection"] = "Otomatik silah algılama: ",
            ["grenade_on"] = "AÇIK",
            ["grenade_off"] = "KAPALI",
            ["grenade_auto"] = "Otomatik (eldeki bomba)",
            ["grenade_detecting"] = "Algılanıyor...",
            ["grenade_detected_map"] = "Algılanan Harita: ",
            ["grenade_data_loaded"] = "Veri yüklendi",
            ["grenade_no_data"] = "Bu harita için veri yok",
            ["combo_weapon_filter"] = "Silah Filtresi",
            ["combo_map_select"] = "Harita Seç",
            ["grenade_instructions"] = "Atış noktaları renkli noktalar olarak gösterilir. Birinin yakınına yürüyerek fırlatma talimatlarını görün. LMB açıları uygular.",

            // Misc - Interface
            ["checkbox_show_watermark"] = "Filigranı Göster",
            ["checkbox_stream_proof"] = "Yayın Koruması (OBS)",
            ["checkbox_cpu_optimize"] = "CPU Optimizasyonu",

            // Settings tabs
            ["settings_keybinds"] = "Tuş Atamaları",
            ["settings_config"] = "Yapılandırma",

            // Settings - Keybinds
            ["key_aim"] = "Aim Tuşu",
            ["key_trigger"] = "Trigger Tuşu",
            ["key_bhop"] = "Bhop Tuşu",
            ["key_menu"] = "Menü Aç/Kapa",

            // Settings - Config
            ["config_save"] = "KAYDET",
            ["config_load"] = "YÜKLE",
            ["config_reset"] = "SIFIRLA",

            // KeyBind helper
            ["press_any_key"] = "BİR TUŞA BAS",

            // DrawActiveFeatures (overlay names)
            ["feature_aimbot"] = "Aimbot",
            ["feature_triggerbot"] = "Triggerbot",
            ["feature_bunnyhop"] = "BunnyHop",
            ["feature_rcs"] = "RCS",
            ["feature_box_esp"] = "Box ESP",
            ["feature_skeleton_esp"] = "Skeleton ESP",
            ["feature_name_esp"] = "Name ESP",
            ["feature_weapon_esp"] = "Weapon ESP",
            ["feature_health_bar"] = "Health Bar",
            ["feature_armor_bar"] = "Armor Bar",
            ["feature_head_tracker"] = "Head Tracker",
            ["feature_snaplines"] = "Snaplines",
            ["feature_distance"] = "Distance",
            ["feature_flags"] = "Flags",
            ["feature_eye_traces"] = "Eye Traces",
            ["feature_crosshair"] = "Crosshair",
            ["feature_radar"] = "Radar",
            ["feature_item_esp"] = "Item ESP",
            ["feature_anti_flash"] = "Anti-Flash",
            ["feature_glow_esp"] = "Glow ESP",
            ["feature_hit_marker"] = "Hit Marker",
            ["feature_damage_text"] = "Damage Text",
            ["feature_offscreen"] = "Offscreen",
            ["feature_watermark"] = "Watermark",
            ["feature_velocity"] = "Velocity",
            ["feature_streamproof"] = "Streamproof",
            ["feature_team_check"] = "Team Check",
            ["feature_hit_sound"] = "Hit Sound",
            ["feature_fov_circle"] = "FOV Circle",
            ["feature_vote_teller"] = "Vote Teller",
            ["feature_grenade_helper"] = "Grenade Helper",

            // ESP flag text
            ["flag_unknown"] = "Bilinmiyor",
            ["flag_scoped"] = "Nişancı",  // "Scoped"
            ["flag_flashed"] = "Flaşlı",  // "Flashed"
            ["flag_reloading"] = "Şarjör Değiştiriyor",
            ["flag_defusing"] = "Bomba İmha",
            ["flag_kit"] = "Kit",
            ["flag_helmet"] = "Kask",
            ["flag_ammo"] = "Mermi",

            // Bomb timer
            ["bomb_bomba"] = "BOMBA",
            ["bomb_site_a"] = "A",
            ["bomb_site_b"] = "B",
            ["bomb_c4"] = "C4",

            // Damage text
            ["damage_kill"] = "ÖLDÜR",

            // Spectator list
            ["spectator_unknown"] = "Bilinmiyor",
            ["spectator_header"] = "İzleyiciler",

            // Watermark
            ["watermark_fps"] = "FPS",

            // Bone names
            ["bone_head"] = "Kafa",
            ["bone_neck"] = "Boyun",
            ["bone_chest"] = "Gövde",
            ["bone_pelvis"] = "Pelvis",

            // Grenade helper
            ["grenade_smoke"] = "Smoke",
            ["grenade_he"] = "HE",
            ["grenade_flash"] = "Flash",
            ["grenade_molotov"] = "Molotof",
            ["grenade_aim"] = "NİŞAN AL",
            ["grenade_pos"] = "KONUM",
            ["grenade_crouch"] = "EĞİL",
            ["grenade_jump"] = "ZIPLA",
            ["grenade_stand"] = "Ayakta",
            ["grenade_throw"] = "at",
            ["grenade_crouch_jump"] = "Eğil-zıpla",
            ["grenade_action_throw_smoke"] = "sis at",
            ["grenade_action_throw_he"] = "HE at",
            ["grenade_action_throw_flash"] = "flash at",
            ["grenade_action_throw_molotov"] = "molotof at",

            // Velocity graph
            ["velocity_label"] = "Hz",
            ["velocity_max"] = "Maks",

            // Distance unit
            ["distance_meters"] = "m",

            // Feature names for dashboard/overlay
            ["feature_head_dot"] = "Head Dot",
            ["feature_ping"] = "Ping",
            ["feature_reloading"] = "Reloading",
            ["feature_defusing"] = "Defusing",
            ["feature_weapon_icon"] = "Weapon Icon",

            // Radar window
            ["window_radar"] = "Radar",

            // Grenade helper hint
            ["grenade_hint_apply"] = "Menü dışında LMB açıları uygular",

            // Vote teller
            ["vote_none"] = "Yok",
            ["vote_kick"] = "Oyuncuyu At",
            ["vote_swap"] = "Takım Değiştir",
            ["vote_timeout"] = "Mola Ver",
            ["vote_draw"] = "Maçı Berabere Bitir",
            ["vote_rematch"] = "Yeniden Oyna",
            ["vote_surrender"] = "Teslim Ol",
            ["vote_side"] = "Taraf Seçimi",
            ["vote_unknown"] = "Bilinmiyor",
            ["vote_terrorists"] = "TERÖRİSTLER",
            ["vote_ct"] = "ANTİ-TERÖRİSTLER",
            ["vote_everyone"] = "HERKES",
            ["vote_format"] = "Oylama: {0}\nTakım: {1}\nSeçenek: {2} | Oy verebilecek: {3}",
            ["vote_yes"] = "Evet",
            ["vote_potential"] = "Oy verebilecek",

            // Language selector
            ["language_label"] = "Dil",
            ["language_section"] = "DİL AYARLARI",
        },
        ["English"] = new()
        {
            ["tab_dashboard"] = "DASHBOARD",
            ["tab_aimbot"] = "COMBAT",
            ["tab_visuals"] = "VISUALS",
            ["tab_misc"] = "MISC",
            ["tab_settings"] = "SETTINGS",

            ["sidebar_title"] = "MematiHack",
            ["sidebar_subtitle"] = "External v2.0",
            ["sidebar_connected"] = "Connected",

            ["header_subtitle"] = "MematiHack External",

            ["dashboard_title"] = "MematiHack External",
            ["dashboard_subtitle"] = "Counter-Strike 2  |  v2.0",
            ["card_status"] = "STATUS",
            ["card_fps"] = "FPS",
            ["card_features"] = "FEATURES",
            ["card_active"] = "Active",
            ["features_header"] = "ACTIVE FEATURES",
            ["quick_actions"] = "QUICK ACTIONS",

            ["social_instagram"] = "Instagram",
            ["social_github"] = "GitHub",
            ["social_source"] = "Source Code",

            ["aimbot_help"] = "Help",
            ["aimbot_settings"] = "Settings",
            ["aimbot_recoil"] = "Recoil",

            ["checkbox_aimbot"] = "Aimbot Master",
            ["checkbox_fov_circle"] = "Draw FOV Circle",
            ["checkbox_dynamic_fov"] = "Dynamic FOV",
            ["checkbox_rcs"] = "Enable RCS",
            ["slider_fov"] = "Field of View",
            ["slider_smoothing"] = "Smoothing Speed",
            ["slider_rcs_strength"] = "RCS Strength",
            ["combo_bone"] = "Target Bone",

            ["visuals_player"] = "Player ESP",
            ["visuals_world"] = "World",
            ["visuals_effects"] = "Effects",
            ["visuals_colors"] = "Colors",

            ["checkbox_box_esp"] = "Box ESP",
            ["checkbox_name_esp"] = "Name ESP",
            ["checkbox_health_bar"] = "Health Bar",
            ["checkbox_armor_bar"] = "Armor Bar",
            ["checkbox_distance"] = "Distance",
            ["checkbox_weapon"] = "Weapon",
            ["checkbox_money"] = "Money",
            ["checkbox_ammo"] = "Ammo",
            ["checkbox_skeleton"] = "Skeleton",
            ["checkbox_snaplines"] = "Snaplines",
            ["checkbox_visible_only"] = "Visible Only",

            ["checkbox_crosshair"] = "Crosshair",
            ["checkbox_flash_protection"] = "Flash Protection",
            ["checkbox_glow_esp"] = "Glow ESP",
            ["checkbox_health_based"] = "Health Based",
            ["checkbox_offscreen_arrows"] = "Offscreen Arrows",
            ["checkbox_radar"] = "2D Radar",
            ["checkbox_item_esp"] = "Item ESP",
            ["checkbox_bomb_timer"] = "Bomb Timer",
            ["checkbox_spectator_list"] = "Spectator List",
            ["slider_radar_zoom"] = "Radar Zoom",
            ["combo_glow_style"] = "Glow Style",
            ["glow_default"] = "Default",
            ["glow_pulse"] = "Pulse",
            ["glow_outline"] = "Outline",
            ["glow_solid"] = "Solid",
            ["color_enemy_glow"] = "Enemy Glow",
            ["color_team_glow"] = "Team Glow",

            ["checkbox_hitmarker"] = "Enable Hitmarker",
            ["checkbox_hit_sound"] = "Enable Hit Sound",
            ["checkbox_damage_text"] = "Damage Text",
            ["slider_volume"] = "Volume",
            ["combo_hit_sound"] = "Hit Sound",
            ["btn_test_sound"] = "TEST SOUND",
            ["slider_size"] = "Size",
            ["slider_gap"] = "Gap",
            ["slider_duration_ms"] = "Duration (ms)",
            ["slider_thickness"] = "Thickness",
            ["color_pick"] = "Color",
            ["slider_text_size"] = "Text Size",
            ["color_text_color"] = "Text Color",

            ["color_enemy_box"] = "Enemy Box",
            ["color_team_box"] = "Team Box",
            ["color_esp_text"] = "ESP Text",
            ["checkbox_esp_rainbow"] = "ESP Rainbow",
            ["color_watermark"] = "Watermark",
            ["checkbox_watermark_rainbow"] = "Watermark Rainbow",
            ["color_bomb_panel"] = "Bomb Panel",
            ["color_bomb_text"] = "Bomb Text",
            ["color_bomb_marker"] = "Bomb Marker",
            ["checkbox_bomb_rainbow"] = "Bomb Rainbow",

            ["misc_movement"] = "Movement",
            ["misc_tools"] = "Tools",
            ["misc_grenades"] = "Grenades",
            ["misc_interface"] = "Interface",

            ["checkbox_bunnyhop"] = "Auto BunnyHop",
            ["checkbox_velocity_graph"] = "Velocity Graph",

            ["checkbox_triggerbot"] = "Trigger Bot",
            ["checkbox_team_check"] = "Team Check",
            ["checkbox_vote_teller"] = "Show Vote",

            ["checkbox_grenade_helper"] = "Enable Grenade Helper",
            ["grenade_detection"] = "Auto weapon detection: ",
            ["grenade_on"] = "ON",
            ["grenade_off"] = "OFF",
            ["grenade_auto"] = "Auto (current grenade)",
            ["grenade_detecting"] = "Detecting...",
            ["grenade_detected_map"] = "Detected Map: ",
            ["grenade_data_loaded"] = "Data loaded",
            ["grenade_no_data"] = "No data for this map",
            ["combo_weapon_filter"] = "Weapon Filter",
            ["combo_map_select"] = "Map Select",
            ["grenade_instructions"] = "Grenade points are shown as colored dots. Walk near one to see throw instructions. LMB applies angles.",

            ["checkbox_show_watermark"] = "Show Watermark",
            ["checkbox_stream_proof"] = "Stream Proof (OBS)",
            ["checkbox_cpu_optimize"] = "CPU Optimization",

            ["settings_keybinds"] = "Key Bindings",
            ["settings_config"] = "Configuration",

            ["key_aim"] = "Aim Key",
            ["key_trigger"] = "Trigger Key",
            ["key_bhop"] = "BunnyHop Key",
            ["key_menu"] = "Menu Toggle",

            ["config_save"] = "SAVE",
            ["config_load"] = "LOAD",
            ["config_reset"] = "RESET",

            ["press_any_key"] = "PRESS ANY KEY",

            ["feature_aimbot"] = "Aimbot",
            ["feature_triggerbot"] = "Triggerbot",
            ["feature_bunnyhop"] = "BunnyHop",
            ["feature_rcs"] = "RCS",
            ["feature_box_esp"] = "Box ESP",
            ["feature_skeleton_esp"] = "Skeleton ESP",
            ["feature_name_esp"] = "Name ESP",
            ["feature_weapon_esp"] = "Weapon ESP",
            ["feature_health_bar"] = "Health Bar",
            ["feature_armor_bar"] = "Armor Bar",
            ["feature_head_tracker"] = "Head Tracker",
            ["feature_snaplines"] = "Snaplines",
            ["feature_distance"] = "Distance",
            ["feature_flags"] = "Flags",
            ["feature_eye_traces"] = "Eye Traces",
            ["feature_crosshair"] = "Crosshair",
            ["feature_radar"] = "Radar",
            ["feature_item_esp"] = "Item ESP",
            ["feature_anti_flash"] = "Anti-Flash",
            ["feature_glow_esp"] = "Glow ESP",
            ["feature_hit_marker"] = "Hit Marker",
            ["feature_damage_text"] = "Damage Text",
            ["feature_offscreen"] = "Offscreen",
            ["feature_watermark"] = "Watermark",
            ["feature_velocity"] = "Velocity",
            ["feature_streamproof"] = "Streamproof",
            ["feature_team_check"] = "Team Check",
            ["feature_hit_sound"] = "Hit Sound",
            ["feature_fov_circle"] = "FOV Circle",
            ["feature_vote_teller"] = "Vote Teller",
            ["feature_grenade_helper"] = "Grenade Helper",

            ["flag_unknown"] = "Unknown",
            ["flag_scoped"] = "Scoped",
            ["flag_flashed"] = "Flashed",
            ["flag_reloading"] = "Reloading",
            ["flag_defusing"] = "Defusing",
            ["flag_kit"] = "Kit",
            ["flag_helmet"] = "Helmet",
            ["flag_ammo"] = "Ammo",

            ["bomb_bomba"] = "BOMB",
            ["bomb_site_a"] = "A",
            ["bomb_site_b"] = "B",
            ["bomb_c4"] = "C4",

            ["damage_kill"] = "KILL",

            ["spectator_unknown"] = "Unknown",
            ["spectator_header"] = "Spectators",

            ["watermark_fps"] = "FPS",

            ["bone_head"] = "Head",
            ["bone_neck"] = "Neck",
            ["bone_chest"] = "Chest",
            ["bone_pelvis"] = "Pelvis",

            ["grenade_smoke"] = "Smoke",
            ["grenade_he"] = "HE",
            ["grenade_flash"] = "Flash",
            ["grenade_molotov"] = "Molotov",
            ["grenade_aim"] = "AIM",
            ["grenade_pos"] = "POS",
            ["grenade_crouch"] = "CROUCH",
            ["grenade_jump"] = "JUMP",
            ["grenade_stand"] = "Stand",
            ["grenade_throw"] = "throw",
            ["grenade_crouch_jump"] = "Crouch-jump",
            ["grenade_action_throw_smoke"] = "throw smoke",
            ["grenade_action_throw_he"] = "throw HE",
            ["grenade_action_throw_flash"] = "throw flash",
            ["grenade_action_throw_molotov"] = "throw molotov",

            ["velocity_label"] = "Vel",
            ["velocity_max"] = "Max",

            ["distance_meters"] = "m",

            ["feature_head_dot"] = "Head Dot",
            ["feature_ping"] = "Ping",
            ["feature_reloading"] = "Reloading",
            ["feature_defusing"] = "Defusing",
            ["feature_weapon_icon"] = "Weapon Icon",

            ["window_radar"] = "Radar",

            ["grenade_hint_apply"] = "LMB outside menu to apply viewangles",

            ["vote_none"] = "None",
            ["vote_kick"] = "Kick Player",
            ["vote_swap"] = "Swap Team",
            ["vote_timeout"] = "Timeout",
            ["vote_draw"] = "Draw",
            ["vote_rematch"] = "Rematch",
            ["vote_surrender"] = "Surrender",
            ["vote_side"] = "Side Selection",
            ["vote_unknown"] = "Unknown",
            ["vote_terrorists"] = "TERRORISTS",
            ["vote_ct"] = "COUNTER-TERRORISTS",
            ["vote_everyone"] = "EVERYONE",
            ["vote_format"] = "Vote: {0}\nTeam: {1}\nYes: {2} | Can vote: {3}",
            ["vote_yes"] = "Yes",
            ["vote_potential"] = "Can vote",

            ["language_label"] = "Language",
            ["language_section"] = "LANGUAGE SETTINGS",
        },
    };
}
