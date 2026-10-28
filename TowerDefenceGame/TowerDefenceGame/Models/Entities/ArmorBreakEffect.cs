namespace TowerDefenceGame.Models.Entities;

/// <summary>防御力を一定値下げる状態異常。防御力はマイナスにもなり、その分ダメージが増える。</summary>
public sealed class ArmorBreakEffect : StatusEffect
{
    public ArmorBreakEffect(int reduction, float duration)
        : base(duration)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(reduction);
        Reduction = reduction;
    }

    public int Reduction { get; }

    public override int ModifyArmor(int armor) => armor - Reduction;
}
