using TowerDefenceGame.Models.Map;
using TowerDefenceGame.Models.Waves;

namespace TowerDefenceGame.Models.Data;

/// <summary>
/// ステージファイル（JSON）を読み込む。
/// layout の文字: '.' 草地 / '#' 道 / 'X' 障害物 / 'W' 荒れ地 / 'S' スタート / 'G' ゴール
/// </summary>
public static class StageLoader
{
    /// <param name="rules">難易度の補正。省略時はノーマル。</param>
    public static StageData Load(string filePath, DifficultyRules? rules = null)
    {
        rules ??= DifficultyRules.Normal;
        var dto = JsonFile.Read<StageDto>(filePath);
        if (dto.TileSize <= 0 || dto.InitialGold < 0 || dto.InitialLives <= 0)
        {
            throw new InvalidDataException("tileSize / initialGold / initialLives の値が不正です。");
        }

        if (dto.Difficulty is < StageData.MinDifficulty or > StageData.MaxDifficulty)
        {
            throw new InvalidDataException($"difficulty は {StageData.MinDifficulty}〜{StageData.MaxDifficulty} で指定してください。");
        }

        var layout = ParseLayout(dto.Layout);
        var start = FindSingle(layout, TileType.Spawn);
        var goal = FindSingle(layout, TileType.Goal);
        var routeCells = RouteFinder.FindRoute(layout, start, goal);

        // 荒れ地にするのは草地だけなので、経路は変わらない
        if (rules.BlockedTileRatio > 0f)
        {
            TileBlocker.Block(layout, rules.BlockedTileRatio, TileBlocker.StableSeed(Path.GetFileNameWithoutExtension(filePath)));
        }

        var waves = BuildWaves(dto.Waves)
            .Select(w => w with { Bonus = rules.ApplyWaveBonus(w.Bonus) })
            .ToList();

        return new StageData
        {
            Name = dto.Name,
            Description = dto.Description,
            Difficulty = dto.Difficulty,
            Map = new GameMap(layout, dto.TileSize, routeCells),
            Waves = waves,
            InitialGold = dto.InitialGold,
            InitialLives = rules.ApplyLives(dto.InitialLives),
            Level = rules.Level,
            LevelOverrides = dto.Levels,
        };
    }

    private static TileType[,] ParseLayout(IReadOnlyList<string> lines)
    {
        if (lines.Count == 0 || lines[0].Length == 0)
        {
            throw new InvalidDataException("layout が空です。");
        }

        int columns = lines[0].Length;
        var layout = new TileType[columns, lines.Count];
        for (int row = 0; row < lines.Count; row++)
        {
            if (lines[row].Length != columns)
            {
                throw new InvalidDataException($"layout の {row + 1} 行目の長さが他の行と異なります。");
            }

            for (int col = 0; col < columns; col++)
            {
                layout[col, row] = lines[row][col] switch
                {
                    '.' => TileType.Grass,
                    '#' => TileType.Road,
                    'X' => TileType.Obstacle,
                    'W' => TileType.Wasteland,
                    'S' => TileType.Spawn,
                    'G' => TileType.Goal,
                    var c => throw new InvalidDataException($"layout に未知の文字 '{c}' があります（{row + 1} 行目）。"),
                };
            }
        }

        return layout;
    }

    private static GridPos FindSingle(TileType[,] layout, TileType type)
    {
        GridPos? found = null;
        for (int col = 0; col < layout.GetLength(0); col++)
        {
            for (int row = 0; row < layout.GetLength(1); row++)
            {
                if (layout[col, row] != type)
                {
                    continue;
                }

                if (found is not null)
                {
                    throw new InvalidDataException($"{type} のマスは1つだけにしてください。");
                }

                found = new GridPos(col, row);
            }
        }

        return found ?? throw new InvalidDataException($"{type} のマスがありません。");
    }

    private static List<Wave> BuildWaves(IReadOnlyList<WaveDto> dtos)
    {
        if (dtos.Count == 0)
        {
            throw new InvalidDataException("waves が空です。");
        }

        var waves = new List<Wave>();
        for (int i = 0; i < dtos.Count; i++)
        {
            var dto = dtos[i];
            if (dto.Entries.Count == 0 || dto.HpMultiplier <= 0 || dto.Bonus < 0
                || dto.Entries.Exists(e => e.Count <= 0 || e.Interval < 0 || e.StartDelay < 0))
            {
                throw new InvalidDataException($"ウェーブ {i + 1} の定義に不正な値があります。");
            }

            waves.Add(new Wave
            {
                Number = i + 1,
                Entries = dto.Entries,
                HpMultiplier = dto.HpMultiplier,
                Bonus = dto.Bonus,
            });
        }

        return waves;
    }

    private sealed class StageDto
    {
        public string Name { get; init; } = "";

        public string Description { get; init; } = "";

        public int Difficulty { get; init; } = StageData.MinDifficulty;

        public float TileSize { get; init; } = 40f;

        public required int InitialGold { get; init; }

        public required int InitialLives { get; init; }

        public required List<string> Layout { get; init; }

        public required List<WaveDto> Waves { get; init; }

        /// <summary>キーはレベル。</summary>
        public Dictionary<int, DifficultyOverrides>? Levels { get; init; }
    }

    private sealed class WaveDto
    {
        public required List<SpawnEntry> Entries { get; init; }

        public float HpMultiplier { get; init; } = 1f;

        public int Bonus { get; init; }
    }
}
