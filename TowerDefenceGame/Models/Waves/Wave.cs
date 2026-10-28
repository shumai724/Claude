namespace TowerDefenceGame.Models.Waves;

/// <summary>1回分の敵の襲来。</summary>
public sealed record Wave
{
    public required int Number { get; init; }

    public required IReadOnlyList<SpawnEntry> Entries { get; init; }

    /// <summary>このウェーブの敵のHP倍率。</summary>
    public float HpMultiplier { get; init; } = 1f;

    /// <summary>ウェーブを凌いだときのボーナス所持金。</summary>
    public int Bonus { get; init; }

    public int TotalEnemies => Entries.Sum(e => e.Count);
}
