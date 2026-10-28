namespace TowerDefenceGame.Models.Data;

/// <summary>
/// 難易度の補正の一部だけを書き換える設定。書いた項目だけが置き換わり、残りは元の値のまま。
/// difficulty.json の各レベルの段階と、ステージファイルの "levels" の上書きに使う。
/// </summary>
public sealed record DifficultyOverrides
{
    public float? EnemyHpMultiplier { get; init; }

    public float? EnemySpeedMultiplier { get; init; }

    public int? EnemyArmorBonus { get; init; }

    public float? RewardMultiplier { get; init; }

    public float? WaveBonusMultiplier { get; init; }

    public float? TowerCostMultiplier { get; init; }

    public float? UpgradeCostMultiplier { get; init; }

    public float? LivesMultiplier { get; init; }

    public float? BlockedTileRatio { get; init; }

    public DifficultyRules ApplyTo(DifficultyRules rules)
    {
        var merged = rules with
        {
            EnemyHpMultiplier = EnemyHpMultiplier ?? rules.EnemyHpMultiplier,
            EnemySpeedMultiplier = EnemySpeedMultiplier ?? rules.EnemySpeedMultiplier,
            EnemyArmorBonus = EnemyArmorBonus ?? rules.EnemyArmorBonus,
            RewardMultiplier = RewardMultiplier ?? rules.RewardMultiplier,
            WaveBonusMultiplier = WaveBonusMultiplier ?? rules.WaveBonusMultiplier,
            TowerCostMultiplier = TowerCostMultiplier ?? rules.TowerCostMultiplier,
            UpgradeCostMultiplier = UpgradeCostMultiplier ?? rules.UpgradeCostMultiplier,
            LivesMultiplier = LivesMultiplier ?? rules.LivesMultiplier,
            BlockedTileRatio = BlockedTileRatio ?? rules.BlockedTileRatio,
        };
        merged.Validate();
        return merged;
    }
}
