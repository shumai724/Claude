namespace TowerDefenceGame.Models.Entities;

/// <summary>移動を完全に止める状態異常。</summary>
public sealed class StunEffect : StatusEffect
{
    public StunEffect(float duration)
        : base(duration)
    {
    }

    public override float ModifySpeed(float speed) => 0f;
}
