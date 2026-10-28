using TowerDefenceGame.Models.Entities;

namespace TowerDefenceGame.Models.Data;

/// <summary>タワー・敵のステータス定義ファイルを読み込む。</summary>
public static class SpecLoader
{
    public static IReadOnlyDictionary<TowerType, TowerSpec> LoadTowerSpecs(string filePath) =>
        Load<TowerSpec, TowerType>(filePath, s => s.Type, s => s.Validate());

    public static IReadOnlyDictionary<EnemyType, EnemySpec> LoadEnemySpecs(string filePath) =>
        Load<EnemySpec, EnemyType>(filePath, s => s.Type, s => s.Validate());

    private static Dictionary<TKey, TSpec> Load<TSpec, TKey>(
        string filePath, Func<TSpec, TKey> keySelector, Action<TSpec> validate)
        where TKey : notnull
    {
        var specs = new Dictionary<TKey, TSpec>();
        foreach (var spec in JsonFile.Read<List<TSpec>>(filePath))
        {
            validate(spec);
            if (!specs.TryAdd(keySelector(spec), spec))
            {
                throw new InvalidDataException($"{Path.GetFileName(filePath)}: {keySelector(spec)} が重複しています。");
            }
        }

        return specs;
    }
}
