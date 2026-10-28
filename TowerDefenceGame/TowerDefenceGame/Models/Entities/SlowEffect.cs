namespace TowerDefenceGame.Models.Entities;

/// <summary>移動速度を一定割合下げる状態異常。</summary>
public sealed class SlowEffect : StatusEffect
{
    private const float MaxSlowRate = 0.95f;

    public SlowEffect(float slowRate, float duration)
        : base(duration)
    {
        SlowRate = Math.Clamp(slowRate, 0f, MaxSlowRate);
    }

    public float SlowRate { get; }

    public override float ModifySpeed(float speed) => speed * (1f - SlowRate);
}
