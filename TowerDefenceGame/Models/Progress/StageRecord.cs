using System.Text.Json.Serialization;
using TowerDefenceGame.Models.Data;

namespace TowerDefenceGame.Models.Progress;

/// <summary>1ステージ分のクリア記録。難易度レベルごとに持つ。</summary>
public sealed record StageRecord
{
    /// <summary>クリアしたレベルの記録。キーはレベル。</summary>
    public IReadOnlyDictionary<int, LevelRecord> Levels { get; init; } = new Dictionary<int, LevelRecord>();

    /// <summary>レベル制になる前のハードモードをクリアしていたか。すべてのレベルを解放済みとして扱う。</summary>
    public bool LegacyHardCleared { get; init; }

    /// <summary>記録に関係なく、すべてのレベルを遊べる扱いか（<see cref="ProgressStore.UnlockAllLevels"/>）。保存はしない。</summary>
    [JsonIgnore]
    public bool AllLevelsUnlocked { get; init; }

    /// <summary>そのレベルの記録。未クリアなら null。</summary>
    public LevelRecord? Get(int level) => Levels.GetValueOrDefault(level);

    /// <summary>クリアした中で最も高いレベル。1つもクリアしていなければ 0。</summary>
    public int HighestClearedLevel() => Levels.Count == 0 ? 0 : Levels.Keys.Max();

    /// <summary>Lv1 は最初から、それより上は1つ下のレベルをクリアすると遊べる。</summary>
    public bool IsUnlocked(int level) =>
        level <= DifficultyRules.NormalLevel || level <= HighestClearedLevel() + 1 || LegacyHardCleared || AllLevelsUnlocked;
}
