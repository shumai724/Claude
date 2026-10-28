using TowerDefenceGame.Models.Entities;

namespace TowerDefenceGame.Models.Targeting;

/// <summary>最もゴールに近い（最も進んでいる）敵を狙う。</summary>
public sealed class FirstTargeting : ITargetingStrategy
{
    public string Name => "先頭";

    public Enemy? SelectTarget(Tower tower, IEnumerable<Enemy> candidates) =>
        candidates.MaxBy(e => e.Progress);
}
