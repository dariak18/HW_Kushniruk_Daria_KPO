using System.Text.Encodings.Web;
using System.Text.Json;

namespace Pos.Infrastructure.Persistence;

/// <summary>Параметры хранилища: каталог, где лежат JSON-файлы</summary>
public sealed record StorageOptions(string DataDirectory);

internal static class JsonFile
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static List<T> Load<T>(string path)
    {
        if (!File.Exists(path))
        {
            return new List<T>();
        }

        using var stream = File.OpenRead(path);
        return JsonSerializer.Deserialize<List<T>>(stream, Options) ?? new List<T>();
    }

    public static void Save<T>(string path, IEnumerable<T> items)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        File.WriteAllText(path, JsonSerializer.Serialize(items, Options));
    }
}
