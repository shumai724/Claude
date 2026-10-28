using System.Numerics;
using TowerDefenceGame.Models.Data;
using TowerDefenceGame.Models.Map;

namespace TowerDefenceGame.Models.Entities;

/// <summary>
/// 命中した敵の防御力を一定時間下げるタワー。他のタワーのダメージが通りやすくなる。
/// 防御力の下げ幅は攻撃力の強化で大きくなる。
/// <list type="bullet">
/// <item>攻撃力 Lv5「腐食」: 命中するたびに防御力低下が重なる</item>
/// <item>射程 Lv5「酸の飛沫」: 命中した敵の周囲の敵の防御力もまとめて下げる</item>
/// <item>攻撃速度 Lv5「溶解」: 防御力を下げた敵に、効果中ずっと継続ダメージを与える</item>
/// </list>
/// </summary>
public sealed class BreakerTower : AttackTower
{
    public const int CorrosionMaxStacks = 3;
    public const float AcidSplashRadius = 45f;

    /// <summary>「溶解」の毎秒ダメージ（攻撃力に対する割合）。</summary>
    public const float AcidDamageRate = 1.0f;

    public BreakerTower(TowerSpec spec, GridPos cell, Vector2 position)
        : base(spec, cell, position)
    {
    }

    public int ArmorReduction => Spec.ArmorReduction + (GetLevel(UpgradeStat.Damage) - 1);

    /// <summary>重ねがけで到達できる防御力低下の上限。「腐食」が無ければ1回分。</summary>
    public int MaxArmorReduction => ArmorReduction * (IsMastered(UpgradeStat.Damage) ? CorrosionMaxStacks : 1);

    public float ArmorBreakDuration => Spec.ArmorBreakDuration;

    public float AcidDamagePerSecond => IsMastered(UpgradeStat.FireRate) ? Damage * AcidDamageRate : 0f;

    protected override Projectile CreateProjectile(Enemy target, HitModifiers modifiers) =>
        new BreakerShot(Position, target, Damage, Spec.ProjectileSpeed, ArmorReduction, MaxArmorReduction, ArmorBreakDuration,
            splashRadius: IsMastered(UpgradeStat.Range) ? AcidSplashRadius : 0f,
            acidDamagePerSecond: AcidDamagePerSecond,
            modifiers);
}
