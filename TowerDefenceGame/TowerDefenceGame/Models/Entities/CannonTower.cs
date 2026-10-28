using System.Numerics;
using TowerDefenceGame.Models.Core;
using TowerDefenceGame.Models.Data;
using TowerDefenceGame.Models.Map;

namespace TowerDefenceGame.Models.Entities;

/// <summary>
/// 着弾地点の周囲にまとめてダメージを与えるタワー。爆風範囲は射程の強化で広がる。
/// <list type="bullet">
/// <item>攻撃力 Lv5「衝撃弾」: 爆風に巻き込んだ敵をすべて短時間止める</item>
/// <item>射程 Lv5「クラスター弾」: 着弾すると近くの別の敵へ子弾が飛び散る</item>
/// <item>攻撃速度 Lv5「連装砲」: 攻撃のたびに、少し遅れてもう1発撃つ</item>
/// </list>
/// </summary>
public sealed class CannonTower : AttackTower
{
    public const float ShockStunDuration = 0.4f;
    public const int ClusterFragments = 2;
    public const float FollowUpDelay = 0.3f;
    public const float FollowUpDamageRate = 0.7f;

    private const float SplashGrowthPerLevel = 0.08f;

    private float _followUpTimer;

    public CannonTower(TowerSpec spec, GridPos cell, Vector2 position)
        : base(spec, cell, position)
    {
    }

    public float SplashRadius => Spec.SplashRadius * StatScale(UpgradeStat.Range, SplashGrowthPerLevel);

    public override void Update(float deltaTime, World world)
    {
        // 連装砲の2発目は、その時点で狙っている敵へ撃つ（1発目の標的が倒れていても無駄にしない）
        if (_followUpTimer > 0f)
        {
            _followUpTimer -= deltaTime;
            if (_followUpTimer <= 0f && Target is { IsActive: true } target)
            {
                int damage = (int)MathF.Round(Damage * FollowUpDamageRate);
                world.AddProjectile(new CannonBall(Position, target, damage, Spec.ProjectileSpeed, SplashRadius, 0, HitModifiers.None)
                {
                    Source = this,
                });
            }
        }

        base.Update(deltaTime, world);
    }

    protected override HitModifiers CreateModifiers() =>
        IsMastered(UpgradeStat.Damage) ? new(StunDuration: ShockStunDuration) : HitModifiers.None;

    protected override void OnFired(World world)
    {
        if (IsMastered(UpgradeStat.FireRate))
        {
            _followUpTimer = FollowUpDelay;
        }
    }

    protected override Projectile CreateProjectile(Enemy target, HitModifiers modifiers) =>
        new CannonBall(Position, target, Damage, Spec.ProjectileSpeed, SplashRadius,
            IsMastered(UpgradeStat.Range) ? ClusterFragments : 0, modifiers);
}
