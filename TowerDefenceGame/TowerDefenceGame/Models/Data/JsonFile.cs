using System.Text.Json;
using System.Text.Json.Serialization;

namespace TowerDefenceGame.Models.Data;

/// <summary>データファイル（JSON）の読み書き共通処理。</summary>
internal static class JsonFile
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() },
    };

    public static T Read<T>(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        return JsonSerializer.Deserialize<T>(stream, Options)
            ?? throw new InvalidDataException($"{Path.GetFileName(filePath)} が空です。");
    }

    public static void Write<T>(string filePath, T value)
    {
        using var stream = File.Create(filePath);
        JsonSerializer.Serialize(stream, value, Options);
    }
}
