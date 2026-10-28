using TowerDefenceGame.Models.Map;

namespace TowerDefenceGame.Models.Data;

/// <summary>マップの道をたどってスタートからゴールまでの経路（曲がり角のマス）を求める。</summary>
public static class RouteFinder
{
    private static readonly (int Col, int Row)[] Directions = [(1, 0), (-1, 0), (0, 1), (0, -1)];

    /// <param name="layout">[列, 行] の順のマス種別。</param>
    public static IReadOnlyList<GridPos> FindRoute(TileType[,] layout, GridPos start, GridPos goal)
    {
        int columns = layout.GetLength(0);
        int rows = layout.GetLength(1);
        var previous = new Dictionary<GridPos, GridPos>();
        var visited = new HashSet<GridPos> { start };
        var queue = new Queue<GridPos>();
        queue.Enqueue(start);

        // 道は分岐しない前提だが、分岐していても最短経路を選べるよう幅優先探索にする
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (current == goal)
            {
                break;
            }

            foreach (var (dc, dr) in Directions)
            {
                var next = current.Offset(dc, dr);
                bool inside = next.Col >= 0 && next.Col < columns && next.Row >= 0 && next.Row < rows;
                if (!inside || !Tile.IsWalkableType(layout[next.Col, next.Row]) || !visited.Add(next))
                {
                    continue;
                }

                previous[next] = current;
                queue.Enqueue(next);
            }
        }

        if (!visited.Contains(goal))
        {
            throw new InvalidDataException("スタートからゴールまで道がつながっていません。");
        }

        var cells = new List<GridPos> { goal };
        while (cells[^1] != start)
        {
            cells.Add(previous[cells[^1]]);
        }

        cells.Reverse();
        return KeepCorners(cells);
    }

    /// <summary>直進している途中のマスを除き、始点・曲がり角・終点だけを残す。</summary>
    private static List<GridPos> KeepCorners(List<GridPos> cells)
    {
        var corners = new List<GridPos> { cells[0] };
        for (int i = 1; i < cells.Count - 1; i++)
        {
            var incoming = (cells[i].Col - cells[i - 1].Col, cells[i].Row - cells[i - 1].Row);
            var outgoing = (cells[i + 1].Col - cells[i].Col, cells[i + 1].Row - cells[i].Row);
            if (incoming != outgoing)
            {
                corners.Add(cells[i]);
            }
        }

        corners.Add(cells[^1]);
        return corners;
    }
}
