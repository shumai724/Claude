using System.Numerics;
using TowerDefenceGame.Models.Entities;
using TowerDefenceGame.Models.Map;

namespace TowerDefenceGame.Models.Data;

/// <summary>タワーの種類からステータス定義を引き当てて生成する。</summary>
public sealed class TowerFactory
{
    private readonly IReadOnlyDictionary<TowerType, TowerSpec> _specs;

    public TowerFactory(IReadOnlyDictionary<TowerType, TowerSpec> specs)
    {
        _specs = specs;
    }

    /// <summary>ショップに並べる順（種類の定義順）のステータス一覧。</summary>
    public IEnumerable<TowerSpec> Specs => _specs.Values.OrderBy(s => s.Type);

    public TowerSpec GetSpec(TowerType type) =>
        _specs.TryGetValue(type, out var spec) ? spec : throw new KeyNotFoundException($"タワー {type} の定義がありません。");

    public Tower Create(TowerType type, GridPos cell, Vector2 position)
    {
        var spec = GetSpec(type);
        return type switch
        {
            TowerType.Archer => new ArcherTower(spec, cell, position),
            TowerType.Cannon => new CannonTower(spec, cell, position),
            TowerType.Frost => new FrostTower(spec, cell, position),
            TowerType.Breaker => new BreakerTower(spec, cell, position),
            TowerType.Tesla => new TeslaTower(spec, cell, position),
            TowerType.GoldMine => new GoldMine(spec, cell, position),
            TowerType.Support => new SupportTower(spec, cell, position),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
        };
    }
}
