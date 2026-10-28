using System.Numerics;
using TowerDefenceGame.Models.Data;
using TowerDefenceGame.Models.Map;

namespace TowerDefenceGame.Models.Entities;

/// <summary>
/// 命中した敵の移動速度を下げるタワー。減速率は攻撃力の強化で上がる。
/// <list type="bullet">
/// <item>攻撃力 Lv5「氷結」: 数回に1回、命中した敵を凍らせて止める</item>
/// <item>射程 Lv5「ブリザード」: 命中した敵の周囲の敵もまとめて減速させる</item>
/// <item>攻撃速度 Lv5「凍傷」: 減速させた敵が受けるダメージを増やす</item>
/// </list>
/// </summary>
public sealed class FrostTower : AttackTower
{
    public const int FreezeEveryShots = 3;
    public const float FreezeDuration = 1.0f;
    public const float BlizzardRadius = 45f;
    public const float FrostbiteDamageBonus = 0.25f;

    private const float SlowRateGrowthPerLevel = 0.05f;
    private const float MaxSlowRate = 0.8f;

    public FrostTower(TowerSpec spec, GridPos cell, Vector2 position)
        : base(spec, cell, position)
    {
    }

    public float SlowRate =>
        MathF.Min(MaxSlowRate, Spec.SlowRate + SlowRateGrowthPerLevel * (GetLevel(UpgradeStat.Damage) - 1));

    public float SlowDuration => Spec.SlowDuration;

    protected override HitModifiers CreateModifiers() =>
        IsMastered(UpgradeStat.Damage) && ShotCount % FreezeEveryShots == 0
            ? new(StunDuration: FreezeDuration)
            : HitModifiers.None;

    protected override Projectile CreateProjectile(Enemy target, HitModifiers modifiers) =>
        new FrostShot(Position, target, Damage, Spec.ProjectileSpeed, SlowRate, SlowDuration,
            splashRadius: IsMastered(UpgradeStat.Range) ? BlizzardRadius : 0f,
            frostbiteBonus: IsMastered(UpgradeStat.FireRate) ? FrostbiteDamageBonus : 0f,
            modifiers);
}
