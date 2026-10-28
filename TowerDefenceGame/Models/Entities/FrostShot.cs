using System.Numerics;
using TowerDefenceGame.Models.Core;

namespace TowerDefenceGame.Models.Entities;

/// <summary>命中した敵を減速させる氷の弾。</summary>
public sealed class FrostShot : Projectile
{
    /// <param name="splashRadius">周囲の敵も減速させる範囲（フロスト射程 Lv5「ブリザード」）。0 なら標的だけ。</param>
    /// <param name="frostbiteBonus">減速させた敵の被ダメージ増加率（フロスト攻撃速度 Lv5「凍傷」）。0 なら付けない。</param>
    public FrostShot(Vector2 origin, Enemy target, int damage, float speed, float slowRate, float slowDuration,
        float splashRadius, float frostbiteBonus, HitModifiers modifiers)
        : base(origin, target, damage, speed, modifiers)
    {
        SlowRate = slowRate;
        SlowDuration = slowDuration;
        SplashRadius = splashRadius;
        FrostbiteBonus = frostbiteBonus;
    }

    public float SlowRate { get; }

    public float SlowDuration { get; }

    public float SplashRadius { get; }

    public float FrostbiteBonus { get; }

    protected override bool OnHit(World world)
    {
        if (!Target.IsActive)
        {
            return false;
        }

        Chill(Target);
        if (SplashRadius > 0f)
        {
            foreach (var enemy in world.FindEnemiesInRange(Position, SplashRadius))
            {
                if (enemy != Target)
                {
                    Chill(enemy);
                }
            }
        }

        // 凍傷は先に付けておくので、この弾自身のダメージから効果が乗る
        Strike(Target, Damage);
        return true;
    }

    private void Chill(Enemy enemy)
    {
        enemy.AddEffect(new SlowEffect(SlowRate, SlowDuration));
        if (FrostbiteBonus > 0f)
        {
            enemy.AddEffect(new FrostbiteEffect(FrostbiteBonus, SlowDuration));
        }
    }
}
