using System.Numerics;
using TowerDefenceGame.Models.Data;
using TowerDefenceGame.Models.Map;

namespace TowerDefenceGame.Models.Entities;

/// <summary>
/// マップに設置できる建物の基底クラス。建設・強化・売却に関わる共通部分を持つ。
/// 攻撃する建物は <see cref="AttackTower"/>、攻撃しない建物（金鉱・司令塔）はこのクラスを直接継承する。
/// </summary>
/// <remarks>
/// 強化は項目（<see cref="UpgradeStat"/>）ごとに行う。各項目の上限は Lv5 だが、
/// Lv5 にできるのは1つの建物につき1項目だけで、それ以外の項目は Lv4 で止まる。
/// </remarks>
public abstract class Tower : Entity
{
    public const int MaxLevel = 5;

    /// <summary>
    /// 現在のレベルごとの強化費用の倍率（基本強化費に掛ける）。
    /// 1回の強化で1項目しか上がらないので1段階は安く、Lv4→5 は追加効果が付く分だけ高い。
    /// </summary>
    private static readonly float[] CostFactors = [0.5f, 1f, 1.5f, 3f];

    private const int CostRoundingUnit = 5;
    private const float SellRate = 0.7f;

    private readonly Dictionary<UpgradeStat, int> _levels;
    private int _invested;

    protected Tower(TowerSpec spec, GridPos cell, Vector2 position, IReadOnlyList<UpgradeStat> upgradeStats)
        : base(position)
    {
        Spec = spec;
        Cell = cell;
        UpgradeStats = upgradeStats;
        _levels = upgradeStats.ToDictionary(stat => stat, _ => 1);
        _invested = spec.Cost;
    }

    public TowerSpec Spec { get; }

    public GridPos Cell { get; }

    /// <summary>この建物が持つ強化項目（表示順）。</summary>
    public IReadOnlyList<UpgradeStat> UpgradeStats { get; }

    /// <summary>攻撃や効果が届く範囲。範囲を持たない建物は 0。</summary>
    public virtual float Range => 0f;

    /// <summary>Lv5 に達している項目。まだ無ければ null。</summary>
    public UpgradeStat? MasteredStat => _levels.Where(kv => kv.Value >= MaxLevel).Select(kv => (UpgradeStat?)kv.Key).FirstOrDefault();

    /// <summary>いちばん高い項目のレベル。見た目の表示に使う。</summary>
    public int HighestLevel => _levels.Values.Max();

    public int SellPrice => (int)(_invested * SellRate);

    public bool HasStat(UpgradeStat stat) => _levels.ContainsKey(stat);

    public int GetLevel(UpgradeStat stat) => _levels.GetValueOrDefault(stat);

    public bool IsMastered(UpgradeStat stat) => GetLevel(stat) >= MaxLevel;

    /// <summary>その項目が到達できる上限。他の項目が Lv5 なら Lv4 まで。</summary>
    public int LevelLimit(UpgradeStat stat) =>
        MasteredStat is { } mastered && mastered != stat ? MaxLevel - 1 : MaxLevel;

    /// <summary>他の項目が Lv5 のため、これ以上強化できない状態か。</summary>
    public bool IsBlockedByMastery(UpgradeStat stat) =>
        HasStat(stat) && GetLevel(stat) >= LevelLimit(stat) && LevelLimit(stat) < MaxLevel;

    public bool CanUpgrade(UpgradeStat stat) => HasStat(stat) && GetLevel(stat) < LevelLimit(stat);

    /// <summary>その項目を次のレベルに上げる費用（5G 単位に切り上げ）。Lv4→5 は追加効果の分だけ高い。</summary>
    public int GetUpgradeCost(UpgradeStat stat)
    {
        int level = Math.Clamp(GetLevel(stat), 1, MaxLevel - 1);
        float cost = Spec.UpgradeCost * CostFactors[level - 1];
        return (int)MathF.Ceiling(cost / CostRoundingUnit - 0.0001f) * CostRoundingUnit;
    }

    public void Upgrade(UpgradeStat stat)
    {
        if (!CanUpgrade(stat))
        {
            throw new InvalidOperationException($"{Spec.Name} の {stat} はこれ以上強化できません。");
        }

        _invested += GetUpgradeCost(stat);
        _levels[stat]++;
    }

    /// <summary>項目のレベルに応じた倍率（Lv1 で 1.0）。</summary>
    protected float StatScale(UpgradeStat stat, float growthPerLevel) => 1f + growthPerLevel * (GetLevel(stat) - 1);

    /// <summary>項目のレベル分だけ factor を掛け合わせた値（Lv1 で 1.0）。間隔を縮める強化に使う。</summary>
    protected float StatPower(UpgradeStat stat, float factorPerLevel) => MathF.Pow(factorPerLevel, GetLevel(stat) - 1);
}
