using TowerDefenceGame.Models.Entities;
using TowerDefenceGame.Models.Map;

namespace TowerDefenceGame.Models.Data;

/// <summary>敵の種類からステータス定義を引き当てて生成する。</summary>
public sealed class EnemyFactory
{
    private readonly IReadOnlyDictionary<EnemyType, EnemySpec> _specs;

    public EnemyFactory(IReadOnlyDictionary<EnemyType, EnemySpec> specs)
    {
        _specs = specs;
    }

    public bool CanCreate(EnemyType type) => _specs.ContainsKey(type);

    public EnemySpec GetSpec(EnemyType type) =>
        _specs.TryGetValue(type, out var spec) ? spec : throw new KeyNotFoundException($"敵 {type} の定義がありません。");

    public Enemy Create(EnemyType type, Route route, float hpMultiplier = 1f)
    {
        if (!_specs.TryGetValue(type, out var spec))
        {
            throw new KeyNotFoundException($"敵 {type} の定義がありません。");
        }

        return new Enemy(spec, route, hpMultiplier);
    }
}
