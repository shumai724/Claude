namespace TowerDefenceGame.Models.Entities;

/// <summary>受けるダメージを一定割合増やす状態異常（フロスト攻撃速度 Lv5「凍傷」）。</summary>
public sealed class FrostbiteEffect : StatusEffect
{
    public FrostbiteEffect(float damageBonus, float duration)
        : base(duration)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(damageBonus);
        DamageBonus = damageBonus;
    }

    public float DamageBonus { get; }

    public override float DamageTakenMultiplier => 1f + DamageBonus;
}
