using System.Numerics;
using TowerDefenceGame.Models.Core;

namespace TowerDefenceGame.Models.Entities;

/// <summary>標的1体にダメージを与える矢。</summary>
public sealed class Arrow : Projectile
{
    public Arrow(Vector2 origin, Enemy target, int damage, float speed, HitModifiers modifiers)
        : base(origin, target, damage, speed, modifiers)
    {
    }

    protected override bool OnHit(World world)
    {
        if (!Target.IsActive)
        {
            return false;
        }

        Strike(Target, Damage);
        return true;
    }
}
