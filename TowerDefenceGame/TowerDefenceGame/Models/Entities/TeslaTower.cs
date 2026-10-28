using System.Numerics;
using TowerDefenceGame.Models.Data;
using TowerDefenceGame.Models.Map;

namespace TowerDefenceGame.Models.Entities;

/// <summary>
/// 標的から近くの敵へ次々と連鎖する電撃を放つタワー。連鎖数は射程の強化で増える。
/// <list type="bullet">
/// <item>攻撃力 Lv5「過電流」: 連鎖してもダメージが減らない</item>
/// <item>射程 Lv5「分岐雷」: 電撃が最初の標的から2方向に分かれて連鎖する</item>
/// <item>攻撃速度 Lv5「麻痺」: 数回に1回、電撃を受けた敵すべてをしびれさせて止める</item>
/// </list>
/// </summary>
public sealed class TeslaTower : AttackTower
{
    public const int ForkBranches = 2;
    public const int ParalyzeEveryShots = 3;
    public const float ParalyzeDuration = 0.5f;

    public TeslaTower(TowerSpec spec, GridPos cell, Vector2 position)
        : base(spec, cell, position)
    {
    }

    /// <summary>最初の標的の後に何体まで飛び移るか（分岐したときは1本あたり）。射程 Lv3・Lv5 で1体ずつ増える。</summary>
    public int ChainCount => Spec.ChainCount + (GetLevel(UpgradeStat.Range) - 1) / 2;

    public float ChainRange => Spec.ChainRange;

    /// <summary>1回飛び移るごとにダメージに掛かる倍率。「過電流」なら減衰しない。</summary>
    public float ChainFalloff => IsMastered(UpgradeStat.Damage) ? 1f : Spec.ChainFalloff;

    /// <summary>最初の標的から何本に分かれて連鎖するか。</summary>
    public int Branches => IsMastered(UpgradeStat.Range) ? ForkBranches : 1;

    protected override HitModifiers CreateModifiers() =>
        IsMastered(UpgradeStat.FireRate) && ShotCount % ParalyzeEveryShots == 0
            ? new(StunDuration: ParalyzeDuration)
            : HitModifiers.None;

    protected override Projectile CreateProjectile(Enemy target, HitModifiers modifiers) =>
        new LightningBolt(Position, target, Damage, ChainCount, Branches, ChainRange, ChainFalloff, modifiers);
}
