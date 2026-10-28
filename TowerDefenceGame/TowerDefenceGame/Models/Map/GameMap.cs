using System.Numerics;

namespace TowerDefenceGame.Models.Map;

/// <summary>ステージのマス目と敵の経路を保持する。</summary>
public sealed class GameMap
{
    private readonly Tile[,] _tiles;

    /// <param name="layout">マスの種類。[列, 行] の順で指定する。</param>
    /// <param name="tileSize">1マスの一辺の長さ（ワールド座標）。</param>
    /// <param name="routeCells">経路の通過点となるマス（曲がり角）。</param>
    public GameMap(TileType[,] layout, float tileSize, IReadOnlyList<GridPos> routeCells)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(tileSize);

        Columns = layout.GetLength(0);
        Rows = layout.GetLength(1);
        TileSize = tileSize;

        _tiles = new Tile[Columns, Rows];
        for (int col = 0; col < Columns; col++)
        {
            for (int row = 0; row < Rows; row++)
            {
                _tiles[col, row] = new Tile(new GridPos(col, row), layout[col, row]);
            }
        }

        foreach (var cell in routeCells)
        {
            if (GetTile(cell) is not { IsWalkable: true })
            {
                throw new ArgumentException($"経路のマス {cell} が道ではありません。", nameof(routeCells));
            }
        }

        Route = new Route(routeCells.Select(CellToWorld));
    }

    public int Columns { get; }

    public int Rows { get; }

    public float TileSize { get; }

    public float Width => Columns * TileSize;

    public float Height => Rows * TileSize;

    public Route Route { get; }

    public IEnumerable<Tile> Tiles => _tiles.Cast<Tile>();

    public bool Contains(GridPos cell) =>
        cell.Col >= 0 && cell.Col < Columns && cell.Row >= 0 && cell.Row < Rows;

    public Tile? GetTile(GridPos cell) => Contains(cell) ? _tiles[cell.Col, cell.Row] : null;

    public bool CanBuild(GridPos cell) => GetTile(cell)?.IsBuildable == true;

    /// <summary>マスの中心のワールド座標を返す。</summary>
    public Vector2 CellToWorld(GridPos cell) =>
        new((cell.Col + 0.5f) * TileSize, (cell.Row + 0.5f) * TileSize);

    public GridPos WorldToCell(Vector2 position) =>
        new((int)MathF.Floor(position.X / TileSize), (int)MathF.Floor(position.Y / TileSize));
}
