using TowerDefenceGame.Models.Entities;

namespace TowerDefenceGame.Models.Data;

/// <summary>
/// 難易度レベルによる補正。ステージやステータスの定義そのものは変えず、読み込んだ値にこの補正を掛けて遊ぶ。
/// Lv1 はすべて等倍（<see cref="Normal"/>）で、レベルが上がるごとに補正が少しずつ厳しくなる。
/// </summary>
public sealed record DifficultyRules
{
    /// <summary>補正の無い基本のレベル。最初から遊べる。</summary>
    public const int NormalLevel = 1;

    private const int CostRoundingUnit = 5;

    public static DifficultyRules Normal { get; } = new();

    public int Level { get; init; } = NormalLevel;

    public float EnemyHpMultiplier { get; init; } = 1f;

    public float EnemySpeedMultiplier { get; init; } = 1f;

    /// <summary>敵の防御力に加える値。</summary>
    public int EnemyArmorBonus { get; init; }

    /// <summary>敵を倒したときの所持金の倍率。</summary>
    public float RewardMultiplier { get; init; } = 1f;

    public float WaveBonusMultiplier { get; init; } = 1f;

    public float TowerCostMultiplier { get; init; } = 1f;

    public float UpgradeCostMultiplier { get; init; } = 1f;

    public float LivesMultiplier { get; init; } = 1f;

    /// <summary>道の近くの草地のうち、建設できない荒れ地にする割合（0〜0.9）。</summary>
    public float BlockedTileRatio { get; init; }

    /// <summary>
    /// 難易度設定ファイルから、Lv1 から順に各レベルの補正を作る。
    /// ファイルには Lv2 以降について「1つ前のレベルから変える項目」だけが書かれている。
    /// </summary>
    public static IReadOnlyList<DifficultyRules> LoadLevels(string filePath)
    {
        var file = JsonFile.Read<DifficultyFile>(filePath);
        var levels = new List<DifficultyRules> { Normal };
        foreach (var step in file.Levels ?? [])
        {
            int level = levels.Count + 1;
            try
            {
                levels.Add(step.ApplyTo(levels[^1]) with { Level = level });
            }
            catch (InvalidDataException ex)
            {
                throw new InvalidDataException($"Lv{level}: {ex.Message}");
            }
        }

        return levels;
    }

    public EnemySpec Apply(EnemySpec spec) => spec with
    {
        MaxHp = Math.Max(1, (int)MathF.Round(spec.MaxHp * EnemyHpMultiplier)),
        Speed = spec.Speed * EnemySpeedMultiplier,
        Armor = Math.Max(0, spec.Armor + EnemyArmorBonus),
        Reward = ScaleKeepingNonZero(spec.Reward, RewardMultiplier),
    };

    public TowerSpec Apply(TowerSpec spec) => spec with
    {
        Cost = RoundUpCost(spec.Cost * TowerCostMultiplier),
        UpgradeCost = RoundUpCost(spec.UpgradeCost * UpgradeCostMultiplier),
    };

    public IReadOnlyDictionary<TowerType, TowerSpec> Apply(IReadOnlyDictionary<TowerType, TowerSpec> specs) =>
        specs.ToDictionary(kv => kv.Key, kv => Apply(kv.Value));

    public IReadOnlyDictionary<EnemyType, EnemySpec> Apply(IReadOnlyDictionary<EnemyType, EnemySpec> specs) =>
        specs.ToDictionary(kv => kv.Key, kv => Apply(kv.Value));

    public int ApplyLives(int lives) => Math.Max(1, (int)MathF.Round(lives * LivesMultiplier));

    public int ApplyWaveBonus(int bonus) => (int)MathF.Round(bonus * WaveBonusMultiplier);

    public void Validate()
    {
        bool valid = EnemyHpMultiplier > 0 && EnemySpeedMultiplier > 0 && RewardMultiplier >= 0
            && WaveBonusMultiplier >= 0 && TowerCostMultiplier > 0 && UpgradeCostMultiplier > 0
            && LivesMultiplier > 0 && BlockedTileRatio is >= 0 and <= 0.9f;
        if (!valid)
        {
            throw new InvalidDataException("難易度の補正値に不正な値があります。");
        }
    }

    /// <summary>報酬が 0 にならないようにする（もともと 0 なら 0 のまま）。</summary>
    private static int ScaleKeepingNonZero(int value, float multiplier) =>
        value == 0 ? 0 : Math.Max(1, (int)MathF.Round(value * multiplier));

    /// <summary>費用は見やすいよう 5G 単位に切り上げる。</summary>
    private static int RoundUpCost(float cost) =>
        (int)MathF.Ceiling(cost / CostRoundingUnit - 0.0001f) * CostRoundingUnit;

    private sealed class DifficultyFile
    {
        /// <summary>Lv2 から順に、前のレベルから変える項目。</summary>
        public List<DifficultyOverrides>? Levels { get; init; }
    }
}
