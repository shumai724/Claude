using TowerDefenceGame.Models.Map;
using TowerDefenceGame.Models.Waves;

namespace TowerDefenceGame.Models.Data;

/// <summary>ステージファイルから読み込んだ1ステージ分のデータ。</summary>
public sealed record StageData
{
    public const int MinDifficulty = 1;
    public const int MaxDifficulty = 5;

    public required string Name { get; init; }

    public string Description { get; init; } = "";

    /// <summary>難易度（1〜5）。ステージ選択画面の表示に使う。</summary>
    public int Difficulty { get; init; } = MinDifficulty;

    public required GameMap Map { get; init; }

    public required IReadOnlyList<Wave> Waves { get; init; }

    public required int InitialGold { get; init; }

    public required int InitialLives { get; init; }

    /// <summary>このデータに適用されている難易度レベル。</summary>
    public int Level { get; init; } = DifficultyRules.NormalLevel;

    /// <summary>
    /// このステージだけの補正の上書き。キーのレベル以降、書いた項目は共通の値の代わりにこちらを使う。
    /// 無ければ共通の補正をそのまま使う。
    /// </summary>
    public IReadOnlyDictionary<int, DifficultyOverrides>? LevelOverrides { get; init; }

    /// <summary>共通の各レベルの補正に、このステージの上書きを重ねる。</summary>
    public IReadOnlyList<DifficultyRules> ResolveLevels(IReadOnlyList<DifficultyRules> commonLevels)
    {
        if (LevelOverrides is not { Count: > 0 } overrides)
        {
            return commonLevels;
        }

        int maxLevel = commonLevels.Count;
        if (overrides.Keys.Any(level => level <= DifficultyRules.NormalLevel || level > maxLevel))
        {
            throw new InvalidDataException($"上書きできるのは Lv{DifficultyRules.NormalLevel + 1}〜Lv{maxLevel} です。");
        }

        var ordered = overrides.OrderBy(kv => kv.Key).ToList();
        return commonLevels
            .Select(rules => ordered.TakeWhile(kv => kv.Key <= rules.Level).Aggregate(rules, (r, kv) => kv.Value.ApplyTo(r)))
            .ToList();
    }
}
