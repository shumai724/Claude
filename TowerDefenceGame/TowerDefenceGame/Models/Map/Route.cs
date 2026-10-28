using System.Numerics;

namespace TowerDefenceGame.Models.Map;

/// <summary>敵がたどる経路。通過点（ワールド座標）の並びで表す。</summary>
/// <remarks>System.IO.Path との名前衝突を避けるため、クラス図の Path を Route と命名している。</remarks>
public sealed class Route
{
    private readonly List<Vector2> _waypoints;

    public Route(IEnumerable<Vector2> waypoints)
    {
        _waypoints = waypoints.ToList();
        if (_waypoints.Count < 2)
        {
            throw new ArgumentException("経路には2点以上の通過点が必要です。", nameof(waypoints));
        }

        for (int i = 1; i < _waypoints.Count; i++)
        {
            TotalLength += Vector2.Distance(_waypoints[i - 1], _waypoints[i]);
        }
    }

    public int Count => _waypoints.Count;

    public Vector2 this[int index] => _waypoints[index];

    public Vector2 Start => _waypoints[0];

    public Vector2 Goal => _waypoints[^1];

    public float TotalLength { get; }
}
