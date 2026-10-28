using TowerDefenceGame.Models.Map;

namespace TowerDefenceGame.Models.Data;

/// <summary>
/// ハードモード用に、道の近くの草地の一部を建設できない荒れ地に変える。
/// ステージごとに固定の乱数を使うので、同じステージなら毎回同じ場所が荒れ地になる。
/// </summary>
public static class TileBlocker
{
    /// <summary>この距離（マス）以内に道がある草地を、荒れ地にする候補とする。</summary>
    private const int NearRoadDistance = 2;

    /// <returns>荒れ地にしたマスの数。</returns>
    public static int Block(TileType[,] layout, float ratio, int seed)
    {
        int columns = layout.GetLength(0);
        int rows = layout.GetLength(1);
        var candidates = new List<GridPos>();
        for (int col = 0; col < columns; col++)
        {
            for (int row = 0; row < rows; row++)
            {
                if (layout[col, row] == TileType.Grass && IsNearRoad(layout, col, row))
                {
                    candidates.Add(new GridPos(col, row));
                }
            }
        }

        var random = new Random(seed);
        int count = (int)MathF.Round(candidates.Count * ratio);
        foreach (var cell in candidates.OrderBy(_ => random.Next()).Take(count))
        {
            layout[cell.Col, cell.Row] = TileType.Wasteland;
        }

        return count;
    }

    /// <summary>
    /// 文字列から実行のたびに変わらないシード値を作る（FNV-1a）。
    /// string.GetHashCode はプロセスごとに値が変わるので使えない。
    /// </summary>
    public static int StableSeed(string text)
    {
        unchecked
        {
            uint hash = 2166136261;
            foreach (char c in text)
            {
                hash = (hash ^ c) * 16777619;
            }

            return (int)hash;
        }
    }

    private static bool IsNearRoad(TileType[,] layout, int col, int row)
    {
        for (int dc = -NearRoadDistance; dc <= NearRoadDistance; dc++)
        {
            for (int dr = -NearRoadDistance; dr <= NearRoadDistance; dr++)
            {
                int c = col + dc;
                int r = row + dr;
                if (c >= 0 && c < layout.GetLength(0) && r >= 0 && r < layout.GetLength(1) && Tile.IsWalkableType(layout[c, r]))
                {
                    return true;
                }
            }
        }

        return false;
    }
}
