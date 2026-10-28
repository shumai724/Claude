using System.Text.Json;

namespace TowerDefenceGame.Models.Data;

/// <summary>ステージフォルダ内のステージファイルをまとめて読み込んだ一覧。</summary>
public sealed class StageCatalog
{
    private StageCatalog(IReadOnlyList<StageSummary> stages, IReadOnlyList<string> errors)
    {
        Stages = stages;
        Errors = errors;
    }

    /// <summary>ファイル名順に並べたステージ。</summary>
    public IReadOnlyList<StageSummary> Stages { get; }

    /// <summary>読み込めなかったファイルとその理由。壊れたファイルがあっても他のステージは遊べるようにする。</summary>
    public IReadOnlyList<string> Errors { get; }

    public static StageCatalog Load(string directory)
    {
        if (!Directory.Exists(directory))
        {
            return new StageCatalog([], [$"ステージフォルダが見つかりません: {directory}"]);
        }

        var stages = new List<StageSummary>();
        var errors = new List<string>();
        foreach (var file in Directory.GetFiles(directory, "*.json").OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                stages.Add(new StageSummary(Path.GetFileNameWithoutExtension(file), file, StageLoader.Load(file)));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException
                                           or InvalidDataException or ArgumentException)
            {
                errors.Add($"{Path.GetFileName(file)}: {ex.Message}");
            }
        }

        return new StageCatalog(stages, errors);
    }
}
