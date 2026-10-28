using TowerDefenceGame.Models.Entities;

namespace TowerDefenceGame.Models.Waves;

/// <summary>ウェーブ内で「どの敵を・何体・どの間隔で」出すかの定義。</summary>
public sealed record SpawnEntry
{
    public required EnemyType EnemyType { get; init; }

    public required int Count { get; init; }

    /// <summary>出現間隔（秒）。</summary>
    public float Interval { get; init; } = 1f;

    /// <summary>ウェーブ開始から最初の1体が出るまでの時間（秒）。</summary>
    public float StartDelay { get; init; }
}
