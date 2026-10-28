using TowerDefenceGame.Models.Entities;

namespace TowerDefenceGame.Models.Map;

/// <summary>マップの1マス。</summary>
public sealed class Tile
{
    public Tile(GridPos cell, TileType type)
    {
        Cell = cell;
        Type = type;
    }

    public GridPos Cell { get; }

    public TileType Type { get; }

    public Tower? Tower { get; private set; }

    public bool IsBuildable => Type == TileType.Grass && Tower is null;

    public bool IsWalkable => IsWalkableType(Type);

    public static bool IsWalkableType(TileType type) =>
        type is TileType.Road or TileType.Spawn or TileType.Goal;

    public void PlaceTower(Tower tower)
    {
        if (!IsBuildable)
        {
            throw new InvalidOperationException($"マス {Cell} には建設できません。");
        }

        Tower = tower;
    }

    public void RemoveTower() => Tower = null;
}
