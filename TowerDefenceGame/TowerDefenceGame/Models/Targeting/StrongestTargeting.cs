using TowerDefenceGame.Models.Entities;

namespace TowerDefenceGame.Models.Targeting;

/// <summary>残りHPが最も多い敵を狙う。同じHPなら先頭を優先する。</summary>
public sealed class StrongestTargeting : ITargetingStrategy
{
    public string Name => "最大HP";

    public Enemy? SelectTarget(Tower tower, IEnumerable<Enemy> candidates) =>
        candidates.MaxBy(e => (e.Hp, e.Progress));
}
