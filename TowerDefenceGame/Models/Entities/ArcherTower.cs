using System.Numerics;
using TowerDefenceGame.Models.Core;
using TowerDefenceGame.Models.Data;
using TowerDefenceGame.Models.Map;

namespace TowerDefenceGame.Models.Entities;

/// <summary>
/// 単体の敵に素早く矢を撃つタワー。
/// <list type="bullet">
/// <item>攻撃力 Lv5「徹甲矢」: 敵の防御力を無視する</item>
/// <item>射程 Lv5「三本矢」: 射程内の別の敵2体にも同時に矢を撃つ</item>
/// <item>攻撃速度 Lv5「速射」: 同じ敵を撃ち続けるほど攻撃速度が上がる</item>
/// </list>
/// </summary>
public sealed class ArcherTower : AttackTower
{
    public const int VolleyExtraTargets = 2;
    public const float RapidFireBonusPerShot = 0.2f;
    public const int RapidFireMaxStacks = 5;

    private Enemy? _focusTarget;
    private int _focusStacks;

    public ArcherTower(TowerSpec spec, GridPos cell, Vector2 position)
        : base(spec, cell, position)
    {
    }

    /// <summary>「速射」で上がっている攻撃速度の割合。</summary>
    public float RapidFireBonus => IsMastered(UpgradeStat.FireRate) ? RapidFireBonusPerShot * _focusStacks : 0f;

    protected override int ExtraTargets => IsMastered(UpgradeStat.Range) ? VolleyExtraTargets : 0;

    protected override float FireRateMultiplier => 1f + RapidFireBonus;

    protected override HitModifiers CreateModifiers() => new(PierceArmor: IsMastered(UpgradeStat.Damage));

    protected override void OnFired(World world)
    {
        // 標的が変わったら集中が途切れ、攻撃速度は元に戻る
        if (Target == _focusTarget)
        {
            _focusStacks = Math.Min(RapidFireMaxStacks, _focusStacks + 1);
        }
        else
        {
            _focusTarget = Target;
            _focusStacks = 0;
        }
    }

    protected override Projectile CreateProjectile(Enemy target, HitModifiers modifiers) =>
        new Arrow(Position, target, Damage, Spec.ProjectileSpeed, modifiers);
}
