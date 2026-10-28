using TowerDefenceGame.Models.Entities;

namespace TowerDefenceGame.Models.Targeting;

/// <summary>タワーが射程内の敵からどれを狙うかを決める戦略。</summary>
public interface ITargetingStrategy
{
    /// <summary>画面表示用の名前。</summary>
    string Name { get; }

    Enemy? SelectTarget(Tower tower, IEnumerable<Enemy> candidates);
}
