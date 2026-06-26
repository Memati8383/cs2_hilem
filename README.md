# MematiHack v1.0

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
| **v2.6** | Tek exe + assets/ klasörü, self-contained, çalışması garanti | [İndir](https://github.com/Memati8383/cs2_hilem/releases/tag/v2.6) |
| **v2.5** | Tek exe, deneme (çalışmadı) | [İndir](https://github.com/Memati8383/cs2_hilem/releases/tag/v2.5) |
| **v2.4** | Klasör, self-contained (çalışıyor) | [İndir](https://github.com/Memati8383/cs2_hilem/releases/tag/v2.4) |
| **v2.2** | Self-contained, runtime dahil | [İndir](https://github.com/Memati8383/cs2_hilem/releases/tag/v2.2) |
| **v2.1** | Tek exe, .NET 8.0 Runtime gerekli | [İndir](https://github.com/Memati8383/cs2_hilem/releases/tag/v2.1) |
| **v2.0** | Tek exe (PDB dahil) | [İndir](https://github.com/Memati8383/cs2_hilem/releases/tag/v2.0) |
| **v1.1** | Tek exe + assets/ klasörü | [İndir](https://github.com/Memati8383/cs2_hilem/releases/tag/v1.1) |
| **v1.0** | Çoklu dosya + assets/ klasörü | [İndir](https://github.com/Memati8383/cs2_hilem/releases/tag/v1.0) |

## Kurulum (Kaynaktan Derleme)
1. [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)'yı indirin ve kurun.
2. Bu repoyu klonlayın.
3. Visual Studio 2022 ile çözümü açın veya komut satırını kullanın:
   ```bash
   dotnet build
   ```

## Sorumluluk Reddi
Bu proje yalnızca eğitim amaçlıdır. Kullanımı tamamen kendi sorumluluğunuzdadır. Bu yazılımın kullanımından kaynaklanan ban veya diğer sorunlardan sorumlu değilim.
