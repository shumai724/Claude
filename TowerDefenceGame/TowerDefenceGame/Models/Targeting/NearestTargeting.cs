using System.Numerics;
using TowerDefenceGame.Models.Entities;

namespace TowerDefenceGame.Models.Targeting;

/// <summary>タワーに最も近い敵を狙う。</summary>
public sealed class NearestTargeting : ITargetingStrategy
{
    public string Name => "最寄り";

    public Enemy? SelectTarget(Tower tower, IEnumerable<Enemy> candidates) =>
        candidates.MinBy(e => Vector2.DistanceSquared(tower.Position, e.Position));
}
