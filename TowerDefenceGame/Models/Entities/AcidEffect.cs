namespace TowerDefenceGame.Models.Entities;

/// <summary>防御力を無視して毎秒ダメージを与え続ける状態異常（ブレイカー攻撃速度 Lv5「溶解」）。</summary>
public sealed class AcidEffect : StatusEffect
{
    private readonly float _damagePerSecond;

    public AcidEffect(float damagePerSecond, float duration)
        : base(duration)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(damagePerSecond);
        _damagePerSecond = damagePerSecond;
    }

    public override float DamagePerSecond => _damagePerSecond;
}
