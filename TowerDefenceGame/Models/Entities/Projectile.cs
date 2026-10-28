using System.Numerics;
using TowerDefenceGame.Models.Core;

namespace TowerDefenceGame.Models.Entities;

/// <summary>標的を追尾して飛ぶ弾の基底クラス。</summary>
public abstract class Projectile : Entity
{
    private Vector2 _aimPoint;

    protected Projectile(Vector2 origin, Enemy target, int damage, float speed, HitModifiers modifiers)
        : base(origin)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(speed);
        Target = target;
        Damage = damage;
        Speed = speed;
        Modifiers = modifiers;
        _aimPoint = target.Position;
    }

    public Enemy Target { get; }

    public int Damage { get; }

    public float Speed { get; }

    public HitModifiers Modifiers { get; }

    /// <summary>この弾を撃ったタワー。与えたダメージをタワーごとに記録するのに使う。</summary>
    public Tower? Source { get; internal set; }

    public override void Update(float deltaTime, World world)
    {
        // 標的が途中で倒れても、最後に見えていた位置までは飛ぶ
        if (Target.IsActive)
        {
            _aimPoint = Target.Position;
        }

        var toAim = _aimPoint - Position;
        float distance = toAim.Length();
        float step = Speed * deltaTime;
        if (distance <= step)
        {
            Position = _aimPoint;
            Impact(world);
            Deactivate();
            return;
        }

        Position += toAim / distance * step;
    }

    /// <summary>着弾時の処理。何かに命中した（効果を与えた）ら true を返す。</summary>
    protected abstract bool OnHit(World world);

    /// <summary>追加効果（防御無視・スタン）を反映してダメージを与える。</summary>
    protected void Strike(Enemy enemy, int damage)
    {
        // スタンは倒れなかった場合に意味があるので、ダメージより先に付けておく
        if (Modifiers.StunDuration > 0f)
        {
            enemy.AddEffect(new StunEffect(Modifiers.StunDuration));
        }

        enemy.TakeDamage(damage, Modifiers.PierceArmor, Source);
    }

    /// <summary>着弾処理と、命中の通知をまとめて行う。</summary>
    protected void Impact(World world)
    {
        if (OnHit(world))
        {
            world.NotifyProjectileHit(this);
        }
    }
}
