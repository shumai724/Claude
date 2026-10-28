using System.Numerics;
using TowerDefenceGame.Models.Core;
using TowerDefenceGame.Models.Data;
using TowerDefenceGame.Models.Map;

namespace TowerDefenceGame.Models.Entities;

/// <summary>
/// 範囲内の攻撃タワーの攻撃力と攻撃速度を上げる建物（司令塔）。
/// 複数の司令塔の範囲が重なっても効果は足し算せず、最も強いものが適用される。
/// <list type="bullet">
/// <item>強化量 Lv5「鼓舞」: 範囲内のタワーの射程も上がる</item>
/// <item>効果範囲 Lv5「索敵網」: 範囲内のタワーが同時にもう1体を攻撃できる</item>
/// </list>
/// </summary>
public sealed class SupportTower : Tower
{
    public const float InspireRangeBonus = 0.15f;

    private static readonly UpgradeStat[] Stats = [UpgradeStat.BuffPower, UpgradeStat.BuffRange];

    private const float BonusGrowthPerLevel = 0.06f;
    private const float RangeGrowthPerLevel = 0.1f;

    public SupportTower(TowerSpec spec, GridPos cell, Vector2 position)
        : base(spec, cell, position, Stats)
    {
    }

    public override float Range => Spec.Range * StatScale(UpgradeStat.BuffRange, RangeGrowthPerLevel);

    public float DamageBonus => Spec.DamageBuff + BonusGrowthPerLevel * (GetLevel(UpgradeStat.BuffPower) - 1);

    public float FireRateBonus => Spec.FireRateBuff + BonusGrowthPerLevel * (GetLevel(UpgradeStat.BuffPower) - 1);

    public float RangeBonus => IsMastered(UpgradeStat.BuffPower) ? InspireRangeBonus : 0f;

    public int ExtraTargets => IsMastered(UpgradeStat.BuffRange) ? 1 : 0;

    /// <summary>効果は <see cref="World.GetBuffAt"/> で攻撃タワー側から参照するので、自身は何もしない。</summary>
    public override void Update(float deltaTime, World world)
    {
    }

    public bool Covers(Vector2 position) => Vector2.DistanceSquared(Position, position) <= Range * Range;
}
