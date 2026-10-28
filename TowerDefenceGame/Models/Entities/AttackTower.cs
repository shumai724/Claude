using System.Numerics;
using TowerDefenceGame.Models.Core;
using TowerDefenceGame.Models.Data;
using TowerDefenceGame.Models.Map;
using TowerDefenceGame.Models.Targeting;

namespace TowerDefenceGame.Models.Entities;

/// <summary>
/// 射程内の敵を狙って弾を撃つ建物の基底クラス。
/// 強化項目は攻撃力・射程・攻撃速度で、Lv5 にした項目に応じてタワーごとに異なる追加効果が付く。
/// 追加効果の中身は各派生クラスが、下のフック（<see cref="ExtraTargets"/> など）を上書きして決める。
/// </summary>
public abstract class AttackTower : Tower
{
    private static readonly UpgradeStat[] Stats = [UpgradeStat.Damage, UpgradeStat.Range, UpgradeStat.FireRate];

    // 1項目ずつしか上がらない分、以前の「全部まとめて上がる」強化と費用対効果が釣り合うよう伸び幅を大きめにしている
    private const float DamageGrowthPerLevel = 0.5f;
    private const float RangeGrowthPerLevel = 0.12f;
    private const float FireIntervalFactorPerLevel = 0.85f;

    private float _cooldown;

    protected AttackTower(TowerSpec spec, GridPos cell, Vector2 position)
        : base(spec, cell, position, Stats)
    {
    }

    public Enemy? Target { get; private set; }

    public ITargetingStrategy Targeting { get; set; } = TargetingStrategies.Default;

    /// <summary>周囲の司令塔から受けている強化。毎フレーム更新される。</summary>
    public TowerBuff Buff { get; private set; }

    public int Damage =>
        (int)MathF.Round(Spec.Damage * StatScale(UpgradeStat.Damage, DamageGrowthPerLevel) * (1f + Buff.DamageBonus));

    public override float Range =>
        Spec.Range * StatScale(UpgradeStat.Range, RangeGrowthPerLevel) * (1f + Buff.RangeBonus);

    public float FireInterval =>
        Spec.FireInterval * StatPower(UpgradeStat.FireRate, FireIntervalFactorPerLevel) / (1f + Buff.FireRateBonus) / FireRateMultiplier;

    /// <summary>1回の攻撃で狙える敵の数（Lv5 効果と司令塔「索敵網」で増える）。</summary>
    public int TargetCount => 1 + ExtraTargets + Buff.ExtraTargets;

    /// <summary>これまでに攻撃した回数。「n 回に1回」の追加効果に使う。</summary>
    protected int ShotCount { get; private set; }

    /// <summary>Lv5 効果で増える、同時に狙える敵の数。</summary>
    protected virtual int ExtraTargets => 0;

    /// <summary>Lv5 効果による攻撃速度の倍率（1 で変化なし）。</summary>
    protected virtual float FireRateMultiplier => 1f;

    public override void Update(float deltaTime, World world)
    {
        Buff = world.GetBuffAt(Position);
        _cooldown = MathF.Max(0f, _cooldown - deltaTime);

        var candidates = world.FindEnemiesInRange(Position, Range).ToList();
        Target = Targeting.SelectTarget(this, candidates);
        if (Target is null || _cooldown > 0f)
        {
            return;
        }

        ShotCount++;
        var modifiers = CreateModifiers();

        // 2体目以降は、すでに狙った敵を除いて同じ選び方で決める
        var target = Target;
        for (int i = 0; i < TargetCount && target is not null; i++)
        {
            var projectile = CreateProjectile(target, modifiers);
            projectile.Source = this;
            world.AddProjectile(projectile);
            candidates.Remove(target);
            target = Targeting.SelectTarget(this, candidates);
        }

        OnFired(world);
        _cooldown = FireInterval;
    }

    /// <summary>今回の攻撃の弾に乗せる追加効果。</summary>
    protected virtual HitModifiers CreateModifiers() => HitModifiers.None;

    /// <summary>攻撃した直後に呼ばれる。攻撃の積み重ねで変わる Lv5 効果に使う。</summary>
    protected virtual void OnFired(World world)
    {
    }

    protected abstract Projectile CreateProjectile(Enemy target, HitModifiers modifiers);
}
