using System.Text.Json;
using TowerDefenceGame.Models.Data;

namespace TowerDefenceGame.Models.Progress;

/// <summary>ステージごとのクリア記録をファイルに保存・読み込みする。</summary>
public sealed class ProgressStore
{
    private readonly string _filePath;
    private readonly Dictionary<string, StageRecord> _records;

    public ProgressStore(string filePath)
    {
        _filePath = filePath;
        _records = LoadRecords(filePath);
    }

    public static string DefaultPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TowerDefenceGame", "progress.json");

    /// <summary>
    /// 記録に関係なく、すべてのレベルを遊べる扱いにする。記録（ファイル）そのものは変えない。
    /// ゲーム本体では使わない（テスト用に、起動する側のプロジェクトから切り替える）。
    /// </summary>
    public bool UnlockAllLevels { get; set; }

    /// <summary>そのステージの記録。<see cref="UnlockAllLevels"/> のときは、未クリアでも全レベル解放済みの記録を返す。</summary>
    public StageRecord? Get(string stageId)
    {
        var record = _records.GetValueOrDefault(stageId);
        return UnlockAllLevels ? (record ?? new StageRecord()) with { AllLevelsUnlocked = true } : record;
    }

    /// <summary>そのステージのそのレベルを遊べるか。</summary>
    public bool IsUnlocked(string stageId, int level) => (Get(stageId) ?? new StageRecord()).IsUnlocked(level);

    /// <summary>クリアを記録する。そのレベルのベストスコアを更新したら true を返す。</summary>
    public bool RecordClear(string stageId, int level, int score, int lives)
    {
        // 保存する記録には「全レベル解放」の扱いを混ぜないよう、元の記録から作る
        var previous = _records.GetValueOrDefault(stageId) ?? new StageRecord();
        var best = previous.Get(level);
        bool isNewBest = best is null || score > best.BestScore;
        var levels = new Dictionary<int, LevelRecord>(previous.Levels)
        {
            [level] = new LevelRecord
            {
                BestScore = Math.Max(best?.BestScore ?? 0, score),
                BestLives = Math.Max(best?.BestLives ?? 0, lives),
            },
        };
        _records[stageId] = previous with { Levels = levels };
        Save();
        return isNewBest;
    }

    private static Dictionary<string, StageRecord> LoadRecords(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return [];
        }

        try
        {
            return JsonFile.Read<Dictionary<string, StoredRecord>>(filePath)
                .ToDictionary(kv => kv.Key, kv => kv.Value.ToRecord());
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or InvalidDataException)
        {
            // 記録が壊れていても遊べるように、空の記録から始める
            return [];
        }
    }

    private void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
            JsonFile.Write(_filePath, _records);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // 保存できなくてもプレイは続けられるので、記録だけ諦める
        }
    }

    /// <summary>記録ファイルの1ステージ分。レベル制になる前（ノーマル／ハード）の項目も読めるようにしておく。</summary>
    private sealed class StoredRecord
    {
        public Dictionary<int, LevelRecord>? Levels { get; init; }

        public bool LegacyHardCleared { get; init; }

        // 以前の形式のノーマルの記録。Lv1 の記録として引き継ぐ
        public bool Cleared { get; init; }

        public int BestScore { get; init; }

        public int BestLives { get; init; }

        // 以前の形式のハード。補正が変わったので記録は引き継がず、全レベル解放だけ引き継ぐ
        public bool HardCleared { get; init; }

        public StageRecord ToRecord()
        {
            var levels = Levels ?? [];
            if (Cleared && !levels.ContainsKey(DifficultyRules.NormalLevel))
            {
                levels[DifficultyRules.NormalLevel] = new LevelRecord { BestScore = BestScore, BestLives = BestLives };
            }

            return new StageRecord { Levels = levels, LegacyHardCleared = LegacyHardCleared || HardCleared };
        }
    }
}
