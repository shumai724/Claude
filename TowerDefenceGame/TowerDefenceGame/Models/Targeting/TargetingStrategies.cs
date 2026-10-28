namespace TowerDefenceGame.Models.Targeting;

/// <summary>選択可能な攻撃対象選択戦略の一覧。戦略は状態を持たないので共有インスタンスを使う。</summary>
public static class TargetingStrategies
{
    public static readonly ITargetingStrategy First = new FirstTargeting();
    public static readonly ITargetingStrategy Nearest = new NearestTargeting();
    public static readonly ITargetingStrategy Strongest = new StrongestTargeting();

    public static ITargetingStrategy Default => First;

    public static IReadOnlyList<ITargetingStrategy> All { get; } = [First, Nearest, Strongest];
}
