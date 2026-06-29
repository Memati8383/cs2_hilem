# MematiHack v2.0

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)
![Platform](https://img.shields.io/badge/platform-Windows-0078D4?logo=windows)
![License](https://img.shields.io/badge/license-MIT-green)
![CS2](https://img.shields.io/badge/game-CS2-FF6B35?logo=steam)
![Release](https://img.shields.io/github/v/release/Memati8383/cs2_hilem?color=blue&logo=github)

MematiHack, modern ImGui overlay, gömülü menü ve gelişmiş özellikler sunan, Counter-Strike 2 için yüksek performanslı bir external CS2 projesidir.

## Özellikler

### Aimbot
- **AimBot** — Kemik hedefleme, geri tepme kontrolü (RCS), smoothing ve dinamik FOV ile düşmanlara otomatik nişan alma.

### TriggerBot
- **TriggerBot** — Trigger tuşuna basılıyken nişangah düşmanın üzerindeyken otomatik ateş etme.

### Hareket
- **BunnyHop** — Otomatik zıplama ve force-jump ile bunny hop.

### ESP
- **Box ESP** — Sağlık barı, zırh barı, silah bilgisi, isim, mesafe, flagler ve snapline ile 2D çerçeve.
- **Skeleton ESP** — Canlı düşmanlarda kemik bağlantılı iskelet gösterimi.
- **Item ESP** — Yerdeki silah isimlerini ve mesafelerini gösterme.
- **Glow ESP** — Görsel vurgulama için düşman belleğine glow rengi/tipi/stili yazma.
- **Offscreen ESP** — Ekranda görünmeyen düşmanlara yön gösteren oklar.
- **Aim Crosshair** — Sabit nişangah + mermi düşüş noktasını gösteren recoil dot.

### Görseller
- **Radar** — Yakındaki düşmanları ve bomba konumunu gösteren 2D dönen radar penceresi.
- **Anti-Flash** — Flash bombasının etkisini anında sıfırlama.
- **Bomb Timer** — C4 zamanlayıcı paneli, progress bar, defuse bar ve dünya işareti.
- **Vote Teller** — Devam eden oylamanın detaylarını (konu, takım, evet/potansiyel sayıları) gösterme.
- **Watermark** — Sağ üst köşede hile adı, FPS ve saat gösterimi.
- **Spectator List** — Sizi izleyen oyuncuları sağ üst panelde listeleme.
- **Velocity Graph** — Oyuncu hızının son 120 karelik gerçek zamanlı çizgi grafiği.
- **Grenade Helper** — JSON lineup'lardan yüklenen atış talimatları ve nişan alma açıları ile harita işaretçileri.

### Offsets (Güncelleme)
- **a2x/cs2-dumper** — Oyun güncellemelerinde offset'ler otomatik çekilir:
  - `offsets.json` → Base adresler (`dwEntityList`, `dwLocalPlayerPawn`, vb.)
  - `client_dll.json` → Class field offset'leri (`m_iHealth`, `m_iTeamNum`, vb.)
  - `buttons.json` → Tuş kodları (`dwForceJump`, vb.)
  - Kaynak: [github.com/a2x/cs2-dumper](https://github.com/a2x/cs2-dumper)

### Geri Bildirim
- **Hitmarker** — Hasar alındığında ekran ortasında animasyonlu X işareti.
- **Damage Text** — Hasar noktasında kayan hasar sayıları veya "KILL" yazısı (sönümlenerek kaybolur).
- **HitSound** — Düşmana hasar verildiğinde MCI üzerinden yapılandırılabilir .wav sesi çalma.

## İndir (Önerilen)
Hazır derlenmiş sürümleri [GitHub Releases](https://github.com/Memati8383/cs2_hilem/releases) sayfasından indirebilirsiniz.

| Sürüm | Açıklama | İndir |
|-------|----------|-------|
| **v2.0** | Dil sistemi, Material Icons, stagger animasyonları, tam çeviri | [İndir](https://github.com/Memati8383/cs2_hilem/releases/tag/v2) |
| **v1.0** | Klasör tabanlı, self-contained, .NET Runtime gerekmez | [İndir](https://github.com/Memati8383/cs2_hilem/releases/tag/v1.0) |

## Konfigürasyon

`config.json` dosyası `MematiHack.exe` ile aynı dizinde otomatik oluşturulur. Dosyayı not defteriyle açıp değiştirebilirsiniz — oyun içinde menüden yapılan değişiklikler de aynı dosyaya kaydedilir.

### ESP Görsel Ayarları

| Ayar | Varsayılan | Açıklama |
|------|-----------|----------|
| `EspBox` | `false` | Oyuncuların etrafında 2D kutu |
| `EspBoxCorner` | `false` | Tam kutu yerine sadece köşe çizgileri |
| `EspBoxColor` | `[1,0,0,1]` | Düşman kutusu rengi (R,G,B,Alpha) |
| `EspBoxColorTeam` | `[0,1,0,1]` | Takım arkadaşı kutusu rengi |
| `EspName` | `false` | Oyuncu ismini gösterme |
| `EspWeapon` | `false` | Eldeki silah ismi |
| `EspWeaponIcon` | `false` | Silah ikonu (UTF-8 karakter) |
| `EspFlags` | `false` | Flag göstergeleri (helm, defusal kit, scope) |
| `EspSnaplines` | `false` | Oyuncudan ekran altına çizgi |
| `EspDistance` | `false` | Oyuncuya olan mesafe (metre) |
| `EspHealthBar` | `false` | Can çubuğu |
| `EspArmorBar` | `false` | Zırh çubuğu |
| `EspHeadDot` | `false` | Kafada nokta işareti |
| `EspAmmo` | `false` | Mermi sayısı |
| `EspMoney` | `false` | Oyuncunun parası |
| `EspPing` | `false` | Ping değeri |
| `EspReloading` | `false` | Doldurma göstergesi |
| `EspDefusing` | `false` | Bomb imha göstergesi |
| `EspSpottedOnly` | `false` | Sadece takımın gördüğü düşmanları göster |
| `EspTextColor` | `[1,1,1,1]` | ESP yazı rengi |
| `EspTextRainbow` | `false` | Gökkuşağı yazı rengi |

### Skeleton & Glow

| Ayar | Varsayılan | Açıklama |
|------|-----------|----------|
| `SkeletonEsp` | `false` | Oyuncu iskeletini çiz |
| `EspGlow` | `false` | Glow efekti (düşman belleğine yazar) |
| `GlowColorEnemy` | `[1,0,0,0.5]` | Düşman glow rengi |
| `GlowColorTeam` | `[0,1,0,0.5]` | Takım glow rengi |
| `GlowHealthBased` | `false` | Can'a göre glow rengi |
| `GlowStyle` | `3` | Glow stili (0-6 arası) |

### Ek Görseller

| Ayar | Varsayılan | Açıklama |
|------|-----------|----------|
| `Radar` | `false` | 2D dönen radar |
| `RadarRange` | `0.05` | Radar yakınlaştırma oranı |
| `BombTimer` | `true` | Bomba zamanlayıcı paneli |
| `BombTimerColPanel` | `[0.08,0.08,0.1,0.8]` | Panel arka plan rengi |
| `BombTimerColText` | `[1,1,1,1]` | Panel yazı rengi |
| `BombTimerColMarker` | `[1,1,0,1]` | Bomba işareti rengi |
| `BombTimerRainbow` | `false` | Gökkuşağı bombası |
| `VoteTeller` | `false` | Oylama detaylarını göster |
| `Watermark` | `true` | Sağ üst köşede filigran |
| `WatermarkTextColor` | `[1,1,1,1]` | Filigran yazı rengi |
| `WatermarkTextRainbow` | `false` | Gökkuşağı filigran |
| `SpectatorList` | `false` | Sizi izleyenleri listele |
| `VelocityGraph` | `false` | Hız grafiği |
| `ItemEsp` | `false` | Yerdeki silahları göster |
| `OffscreenEnemy` | `false` | Ekran dışı düşman okları |
| `EspEyeTraces` | `false` | Düşmanın baktığı yöne çizgi |
| `GrenadeHelper` | `false` | Lineup atış yardımcısı |
| `GrenadeHelperWeaponFilter` | `-1` | -1=tümü, 0=smoke, 1=HE, 2=flash, 3=molotov |
| `AntiFlash` | `false` | Flash koruması |
| `EspAimCrosshair` | `false` | Nişangah + recoil dot |
| `StreamProof` | `false` | Yayıncı modu (pencereyi gizle) |

### Aimbot

| Ayar | Varsayılan | Açıklama |
|------|-----------|----------|
| `AimBot` | `false` | Aimbot aç/kapa |
| `AimBotKey` | `XButton2` | Aimbot tuşu (Mouse5) |
| `AimFov` | `15` | Nişan alma açısı (derece) |
| `AimFovCircle` | `false` | FOV çemberini göster |
| `AimDynamicFov` | `false` | Hedefe uzaklığa göre dinamik FOV |
| `AimSmoothing` | `3` | Smoothing değeri (yüksek = yavaş) |
| `AimBoneIndex` | `0` | 0=kafa, 1=boyun, 2=gövde, 3=pelvis |
| `AimRcs` | `false` | Geri tepme kontrolü |
| `AimRcsStrength` | `100` | RCS gücü (%) |

### TriggerBot

| Ayar | Varsayılan | Açıklama |
|------|-----------|----------|
| `TriggerBot` | `false` | TriggerBot aç/kapa |
| `TriggerBotKey` | `LMenu` | Trigger tuşu (LAlt) |

### Hareket

| Ayar | Varsayılan | Açıklama |
|------|-----------|----------|
| `BunnyHop` | `false` | Otomatik zıplama |
| `BunnyHopKey` | `Space` | Bunny hop tuşu |

### Geri Bildirim

| Ayar | Varsayılan | Açıklama |
|------|-----------|----------|
| `HitSound` | `true` | Vuruş sesi |
| `HitSoundVolume` | `0.5` | Ses seviyesi (0-1) |
| `HitSoundName` | `"beep.wav"` | Ses dosyası adı (assets/sounds/ içinden) |
| `HitMarker` | `true` | Vuruş işareti |
| `HitMarkerColor` | `[1,1,1,1]` | İşaret rengi |
| `HitMarkerSize` | `12` | İşaret boyutu |
| `HitMarkerGap` | `2` | İşaret boşluğu |
| `HitMarkerDuration` | `300` | Görünme süresi (ms) |
| `HitMarkerThickness` | `2` | Çizgi kalınlığı |
| `DamageText` | `true` | Hasar sayıları |
| `DamageTextColor` | `[1,0.2,0.2,1]` | Hasar yazı rengi |
| `DamageTextDuration` | `1000` | Görünme süresi (ms) |
| `DamageTextSize` | `16` | Yazı boyutu |

### Tuşlar

| Ayar | Varsayılan | Açıklama |
|------|-----------|----------|
| `MenuToggleKey` | `Insert` | Menü aç/kapa |
| `TeamCheck` | `true` | Takım kontrolü (kendi takımını vurma) |

### Performans

| Ayar | Varsayılan | Açıklama |
|------|-----------|----------|
| `FreeCpu` | `false` | CPU tasarrufu (uygulama arka plandayken uyku) |
| `VSync` | `false` | Dikey senkronizasyon |

## Kurulum (Kaynaktan Derleme)
1. [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)'yı indirin ve kurun.
2. Bu repoyu klonlayın.
3. Visual Studio 2022 ile çözümü açın veya komut satırını kullanın:
   ```bash
   dotnet build
   ```

## Sorumluluk Reddi
Bu proje yalnızca eğitim amaçlıdır. Kullanımı tamamen kendi sorumluluğunuzdadır. Bu yazılımın kullanımından kaynaklanan ban veya diğer sorunlardan sorumlu değilim.
