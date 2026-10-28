using System.Numerics;
using TowerDefenceGame.Models.Core;
using TowerDefenceGame.Models.Data;
using TowerDefenceGame.Models.Map;

namespace TowerDefenceGame.Models.Entities;

/// <summary>経路に沿ってゴールを目指す敵。種類ごとの差は <see cref="EnemySpec"/> で表す。</summary>
public sealed class Enemy : Entity
{
    private readonly Route _route;
    private readonly List<StatusEffect> _effects = [];
    private int _nextWaypoint = 1;

    public Enemy(EnemySpec spec, Route route, float hpMultiplier = 1f)
        : base(route.Start)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(hpMultiplier);
        Spec = spec;
        _route = route;
        MaxHp = Math.Max(1, (int)MathF.Round(spec.MaxHp * hpMultiplier));
        Hp = MaxHp;
    }

    public EnemySpec Spec { get; }

    public int MaxHp { get; }

    public int Hp { get; private set; }

    public float HpRatio => (float)Hp / MaxHp;

    public bool IsDead => Hp <= 0;

    public bool IsSlowed => _effects.Exists(e => e is SlowEffect);

    public bool IsArmorBroken => _effects.Exists(e => e is ArmorBreakEffect);

    public bool IsStunned => _effects.Exists(e => e is StunEffect);

    public bool IsFrostbitten => _effects.Exists(e => e is FrostbiteEffect);

    public bool IsMelting => _effects.Exists(e => e is AcidEffect);

    /// <summary>最後にダメージを与えたタワー。倒れたときは、とどめを刺したタワーになる。</summary>
    public Tower? LastHitBy { get; private set; }

    /// <summary>今かかっている防御力低下の量。かかっていなければ 0。</summary>
    public int ArmorBreakReduction => _effects.OfType<ArmorBreakEffect>().FirstOrDefault()?.Reduction ?? 0;

    /// <summary>受けるダメージに掛かる倍率（凍傷などで増える）。</summary>
    public float DamageTakenMultiplier
    {
        get
        {
            float multiplier = 1f;
            foreach (var effect in _effects)
            {
                multiplier *= effect.DamageTakenMultiplier;
            }

            return multiplier;
        }
    }

    /// <summary>現在の防御力。受けるダメージをこの値だけ減らす。防御力低下でマイナスになると逆に増える。</summary>
    public int Armor
    {
        get
        {
            int armor = Spec.Armor;
            foreach (var effect in _effects)
            {
                armor = effect.ModifyArmor(armor);
            }

            return armor;
        }
    }

    /// <summary>スタートから進んだ距離。先頭の敵を判定するのに使う。</summary>
    public float Progress { get; private set; }

    public float CurrentSpeed
    {
        get
        {
            float speed = Spec.Speed;
            foreach (var effect in _effects)
            {
                speed = effect.ModifySpeed(speed);
            }

            return speed;
        }
    }

    public event Action<Enemy>? Killed;

    /// <summary>HP が減ったときに、減った量と攻撃したタワー（継続ダメージなら効果を付けたタワー）を通知する。</summary>
    public event Action<Enemy, Tower?, int>? Damaged;

    public event Action<Enemy>? ReachedGoal;

    public override void Update(float deltaTime, World world)
    {
        UpdateEffects(deltaTime);
        if (!IsActive)
        {
            // 継続ダメージで倒れた
            return;
        }

        MoveAlongRoute(deltaTime);
    }

    /// <summary>
    /// 防御力を差し引いてダメージを受ける。攻撃が当たれば最低 1 は通る。
    /// 凍傷などで被ダメージが増えている場合は、防御力を引いた後の値に倍率を掛ける。
    /// </summary>
    /// <param name="ignoreArmor">
    /// 防御力を無視する（装甲貫通）。防御力低下でマイナスになっている分の増加はそのまま受ける。
    /// </param>
    /// <param name="source">攻撃したタワー。<see cref="Damaged"/> で通知する。</param>
    public void TakeDamage(int amount, bool ignoreArmor = false, Tower? source = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(amount);
        if (!IsActive || amount == 0)
        {
            return;
        }

        int armor = ignoreArmor ? Math.Min(0, Armor) : Armor;
        int dealt = Math.Max(1, amount - armor);
        LoseHp((int)MathF.Round(dealt * DamageTakenMultiplier), source);
    }

    /// <summary>状態異常を付与する。同じ種類の効果は新しいもので上書きする。</summary>
    public void AddEffect(StatusEffect effect)
    {
        if (!IsActive)
        {
            return;
        }

        _effects.RemoveAll(e => e.GetType() == effect.GetType());
        _effects.Add(effect);
    }

    private void UpdateEffects(float deltaTime)
    {
        foreach (var effect in _effects)
        {
            effect.Update(deltaTime);
            effect.PendingDamage += effect.DamagePerSecond * deltaTime;
        }

        // 継続ダメージは1フレームでは1に満たないので、効果ごとに端数を貯めて整数分だけ減らす
        // （効果ごとに分けるのは、どのタワーのダメージかを通知するため）
        foreach (var effect in _effects)
        {
            int whole = (int)effect.PendingDamage;
            if (whole <= 0)
            {
                continue;
            }

            effect.PendingDamage -= whole;
            LoseHp(whole, effect.Source);
            if (!IsActive)
            {
                break;
            }
        }

        _effects.RemoveAll(e => e.IsExpired);
    }

    private void LoseHp(int amount, Tower? source)
    {
        // 通知するのは実際に減ったHPだけ（倒しきった後の超過分は含めない）
        int lost = Math.Min(Math.Max(0, amount), Hp);
        if (lost == 0)
        {
            return;
        }

        Hp -= lost;
        if (source is not null)
        {
            LastHitBy = source;
        }

        Damaged?.Invoke(this, source, lost);
        if (IsDead)
        {
            Deactivate();
            Killed?.Invoke(this);
        }
    }

    private void MoveAlongRoute(float deltaTime)
    {
        float remaining = CurrentSpeed * deltaTime;
        while (remaining > 0f && _nextWaypoint < _route.Count)
        {
            var waypoint = _route[_nextWaypoint];
            float distance = Vector2.Distance(Position, waypoint);
            if (distance <= remaining)
            {
                Position = waypoint;
                Progress += distance;
                remaining -= distance;
                _nextWaypoint++;
            }
            else
            {
                Position += (waypoint - Position) / distance * remaining;
                Progress += remaining;
                remaining = 0f;
            }
        }

        if (_nextWaypoint >= _route.Count)
        {
            Deactivate();
            ReachedGoal?.Invoke(this);
        }
    }
}
