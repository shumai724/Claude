using System.Numerics;
using TowerDefenceGame.Models.Core;

namespace TowerDefenceGame.Models.Entities;

/// <summary>着弾地点の周囲の敵すべてにダメージを与える砲弾。</summary>
public sealed class CannonBall : Projectile
{
    public const float FragmentDamageRate = 0.5f;
    public const float FragmentSplashRate = 0.6f;
    private const float FragmentSearchRange = 90f;

    /// <param name="fragments">着弾後に飛び散る子弾の数（キャノン射程 Lv5「クラスター弾」）。0 なら飛ばない。</param>
    public CannonBall(Vector2 origin, Enemy target, int damage, float speed, float splashRadius, int fragments, HitModifiers modifiers)
        : base(origin, target, damage, speed, modifiers)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(fragments);
        SplashRadius = splashRadius;
        Fragments = fragments;
    }

    public float SplashRadius { get; }

    public int Fragments { get; }

    /// <summary>クラスター弾から飛び散った子弾か。小さく描く。</summary>
    public bool IsFragment { get; private init; }

    protected override bool OnHit(World world)
    {
        // TakeDamage で敵が倒れても列挙が崩れないようにコピーしてから処理する
        foreach (var enemy in world.FindEnemiesInRange(Position, SplashRadius).ToList())
        {
            Strike(enemy, Damage);
        }

        if (Fragments > 0)
        {
            Scatter(world);
        }

        // 敵がいなくても爆発はするので、常に命中扱いにする
        return true;
    }

    /// <summary>着弾地点の近くにいる別の敵へ子弾を飛ばす。子弾はそれ以上分裂しない。</summary>
    private void Scatter(World world)
    {
        var position = Position;
        var targets = world.FindEnemiesInRange(position, FragmentSearchRange)
            .Where(e => e != Target)
            .OrderBy(e => Vector2.DistanceSquared(position, e.Position))
            .Take(Fragments);
        int damage = Math.Max(1, (int)MathF.Round(Damage * FragmentDamageRate));
        foreach (var target in targets)
        {
            world.AddProjectile(new CannonBall(position, target, damage, Speed, SplashRadius * FragmentSplashRate, 0, HitModifiers.None)
            {
                IsFragment = true,
                Source = Source,
            });
        }
    }
}
