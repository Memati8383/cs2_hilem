using System.IO;
using System.Reflection;

namespace CS2Cheat.Utils;

public static class ResourceHelper
{
    private static readonly string ExtractDir = Path.Combine(Path.GetTempPath(), "MematiHack");
    private static readonly object Lock = new();
    private static bool _extracted;

    public static string GetExtractedPath(string relativePath)
    {
        EnsureExtracted();
        return Path.Combine(ExtractDir, relativePath);
    }

    public static string[] ListSoundFiles()
    {
        EnsureExtracted();
        var soundsDir = Path.Combine(ExtractDir, "sounds");
        return Directory.Exists(soundsDir)
            ? Directory.GetFiles(soundsDir, "*.wav").Select(Path.GetFileName).Where(f => f != null).Cast<string>().OrderBy(f => f).ToArray()
            : [];
    }

    public static string ReadText(string relativePath)
    {
        var asm = Assembly.GetExecutingAssembly();
        var name = asm.GetManifestResourceNames().FirstOrDefault(n => n.EndsWith("." + relativePath.Replace('/', '.').Replace('\\', '.')));
        if (name == null) return "";
        using var stream = asm.GetManifestResourceStream(name);
        if (stream == null) return "";
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private static void EnsureExtracted()
    {
        if (_extracted) return;
        lock (Lock)
        {
            if (_extracted) return;
            ExtractAll();
            _extracted = true;
        }
    }

    private static void ExtractAll()
    {
        var asm = Assembly.GetExecutingAssembly();
        foreach (var name in asm.GetManifestResourceNames())
        {
            if (!name.StartsWith("CS2Cheat.assets.")) continue;
            var relPath = name["CS2Cheat.assets.".Length..].Replace('.', Path.DirectorySeparatorChar);
            var path = Path.Combine(ExtractDir, relPath);
            var dir = Path.GetDirectoryName(path)!;
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            if (File.Exists(path)) continue;
            using var stream = asm.GetManifestResourceStream(name);
            if (stream == null) continue;
            using var fs = new FileStream(path, FileMode.Create, FileAccess.Write);
            stream.CopyTo(fs);
        }
    }
}