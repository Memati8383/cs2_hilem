using System.IO;
using System.Reflection;

namespace CS2Cheat.Utils;

public static class ResourceHelper
{
    private static readonly string BaseDir = Path.GetDirectoryName(Environment.ProcessPath)!;

    public static string AssetsDir => Path.Combine(BaseDir, "assets");

    public static string SoundDir => Path.Combine(AssetsDir, "sounds");

    public static string[] ListSoundFiles()
    {
        var dir = SoundDir;
        return Directory.Exists(dir)
            ? Directory.GetFiles(dir, "*.wav").Select(Path.GetFileName).Where(f => f != null).Cast<string>().OrderBy(f => f).ToArray()
            : [];
    }

    public static string ReadText(string relativePath)
    {
        var path = Path.Combine(BaseDir, relativePath);
        return File.Exists(path) ? File.ReadAllText(path) : "";
    }
}
