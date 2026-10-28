using System.Numerics;
using TowerDefenceGame.Models.Map;

namespace TowerDefenceGame.Developer.Bot;

/// <summary>
/// 敵の経路を一定間隔の点に区切ったもの。
/// 「あるマスに置いたタワーが、道のどれだけを射程に収めるか」を、射程内の点の数で数えるのに使う。
/// 射程内の点の集合はビット列で持ち、重なり（フロストと主力の射程の重なりなど）も高速に数えられるようにする。
/// </summary>
internal sealed class RouteSamples
{
    /// <summary>点の間隔（ワールド座標）。</summary>
    public const float Spacing = 5f;

    /// <summary>この割合より後ろ（ゴール寄り）の区間は、最後の砦として少し重く見る。</summary>
    private const float GoalSideStart = 0.75f;
    private const float GoalSideWeight = 1.15f;

    private readonly Vector2[] _points;
    private readonly float[] _goalWeights;
    private readonly Dictionary<(int Col, int Row, int Radius), ulong[]> _cache = [];

    public RouteSamples(Route route)
    {
        var points = new List<Vector2> { route[0] };
        var progress = new List<float> { 0f };
        float traveled = 0f;
        for (int i = 1; i < route.Count; i++)
        {
            var from = route[i - 1];
            var to = route[i];
            float length = Vector2.Distance(from, to);
            int steps = Math.Max(1, (int)MathF.Ceiling(length / Spacing));
            for (int s = 1; s <= steps; s++)
            {
                points.Add(Vector2.Lerp(from, to, (float)s / steps));
                progress.Add(traveled + length * s / steps);
            }

            traveled += length;
        }

        _points = [.. points];
        _goalWeights = progress
            .Select(p => traveled > 0f && p / traveled >= GoalSideStart ? GoalSideWeight : 1f)
            .ToArray();
    }

    public int Count => _points.Length;

    public float GoalWeight(int index) => _goalWeights[index];

    /// <summary>中心から半径以内にある点の集合（ビット列）。マスと半径ごとに覚えておく。</summary>
    public ulong[] InRange(GridPos cell, Vector2 center, float radius)
    {
        var key = (cell.Col, cell.Row, (int)MathF.Round(radius));
        if (_cache.TryGetValue(key, out var cached))
        {
            return cached;
        }

        var bits = BitSet.Create(_points.Length);
        float radiusSquared = radius * radius;
        for (int i = 0; i < _points.Length; i++)
        {
            if (Vector2.DistanceSquared(center, _points[i]) <= radiusSquared)
            {
                BitSet.Set(bits, i);
            }
        }

        _cache[key] = bits;
        return bits;
    }
}

/// <summary>ulong の配列をビット列として扱う小さな道具。</summary>
internal static class BitSet
{
    public static ulong[] Create(int length) => new ulong[(length + 63) / 64];

    public static void Set(ulong[] bits, int index) => bits[index >> 6] |= 1UL << (index & 63);

    public static int Count(ulong[] bits)
    {
        int count = 0;
        foreach (ulong word in bits)
        {
            count += BitOperations.PopCount(word);
        }

        return count;
    }

    public static int CountAnd(ulong[] a, ulong[] b)
    {
        int count = 0;
        for (int i = 0; i < a.Length; i++)
        {
            count += BitOperations.PopCount(a[i] & b[i]);
        }

        return count;
    }

    /// <summary>立っているビットの番号ごとに counts を 1 増やす。</summary>
    public static void AddTo(ulong[] bits, int[] counts)
    {
        for (int w = 0; w < bits.Length; w++)
        {
            ulong word = bits[w];
            while (word != 0)
            {
                counts[(w << 6) + BitOperations.TrailingZeroCount(word)]++;
                word &= word - 1;
            }
        }
    }

    /// <summary>立っているビットの番号ごとに weight(番号) を足し合わせる。</summary>
    public static float Sum(ulong[] bits, Func<int, float> weight)
    {
        float sum = 0f;
        for (int w = 0; w < bits.Length; w++)
        {
            ulong word = bits[w];
            while (word != 0)
            {
                sum += weight((w << 6) + BitOperations.TrailingZeroCount(word));
                word &= word - 1;
            }
        }

        return sum;
    }
}
