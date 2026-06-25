# MematiHack v1.0

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

### Geri Bildirim
- **Hitmarker** — Hasar alındığında ekran ortasında animasyonlu X işareti.
- **Damage Text** — Hasar noktasında kayan hasar sayıları veya "KILL" yazısı (sönümlenerek kaybolur).
- **HitSound** — Düşmana hasar verildiğinde MCI üzerinden yapılandırılabilir .wav sesi çalma.

## Kurulum
1. [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)'yı indirin ve kurun.
2. Bu repoyu klonlayın.
3. Visual Studio 2022 ile çözümü açın veya komut satırını kullanın:
   ```bash
   dotnet build
   ```

## Kullanım
1. Counter-Strike 2'yi başlatın.
2. `CS2Cheat.exe`'yi çalıştırın.
3. Menüyü açmak/kapatmak için **INSERT** tuşuna basın.

## Sorumluluk Reddi
Bu proje yalnızca eğitim amaçlıdır. Kullanımı tamamen kendi sorumluluğunuzdadır. Bu yazılımın kullanımından kaynaklanan ban veya diğer sorunlardan sorumlu değilim.
