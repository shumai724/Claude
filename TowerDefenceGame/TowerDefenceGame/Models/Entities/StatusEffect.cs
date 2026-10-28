namespace TowerDefenceGame.Models.Entities;

/// <summary>敵に一定時間かかる状態異常の基底クラス。影響するステータスだけをオーバーライドする。</summary>
public abstract class StatusEffect
{
    protected StatusEffect(float duration)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(duration);
        Duration = duration;
    }

    public float Duration { get; }

    public float Elapsed { get; private set; }

    /// <summary>この効果を付けたタワー。継続ダメージをタワーごとに記録するのに使う。</summary>
    public Tower? Source { get; init; }

    /// <summary>継続ダメージのうち、まだ HP に反映していない端数。</summary>
    internal float PendingDamage { get; set; }

    public bool IsExpired => Elapsed >= Duration;

    /// <summary>受けるダメージに掛かる倍率（1 で変化なし）。</summary>
    public virtual float DamageTakenMultiplier => 1f;

    /// <summary>効果中に毎秒受ける、防御力を無視した継続ダメージ。</summary>
    public virtual float DamagePerSecond => 0f;

    public void Update(float deltaTime) => Elapsed += deltaTime;

    public virtual float ModifySpeed(float speed) => speed;

    public virtual int ModifyArmor(int armor) => armor;
}
