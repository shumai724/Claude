using TowerDefenceGame.Models.Core;
using TowerDefenceGame.Models.Data;
using TowerDefenceGame.Models.Waves;

namespace TowerDefenceGame.Developer.Bot;

/// <summary>敵1種類分（同じ種類はまとめる）。Hp はウェーブの HP 倍率を掛けた後の値。</summary>
internal readonly record struct WaveGroup(EnemySpec Spec, int Count, int Hp);

/// <summary>bot が備える相手となるウェーブの要約（敵の総HP・出現ペース・一番硬い敵など）。</summary>
internal sealed class WaveProfile
{
    private WaveProfile(IReadOnlyList<WaveGroup> groups, float duration)
    {
        Groups = groups;
        TotalCount = groups.Sum(g => g.Count);
        TotalHp = groups.Sum(g => (float)g.Count * g.Hp);
        Duration = MathF.Max(1f, duration);
        SpawnRate = TotalCount / Duration;
        AverageSpeed = TotalHp > 0f ? groups.Sum(g => g.Spec.Speed * g.Count * g.Hp) / TotalHp : 1f;

        // 防御力の分だけ実質的に硬いとみなして、一番倒しにくい敵を選ぶ
        Toughest = groups.MaxBy(g => g.Hp * (1f + g.Spec.Armor * 0.1f));
    }

    public IReadOnlyList<WaveGroup> Groups { get; }

    public int TotalCount { get; }

    public float TotalHp { get; }

    /// <summary>最後の1体が出るまでの時間（秒）。</summary>
    public float Duration { get; }

    /// <summary>1秒あたりに出てくる敵の数。</summary>
    public float SpawnRate { get; }

    /// <summary>HP で重み付けした平均の移動速度。</summary>
    public float AverageSpeed { get; }

    public WaveGroup Toughest { get; }

    public bool HasArmored => Groups.Any(g => g.Spec.Armor > 0);

    /// <summary>敵の数の倍率（開発者モード）と HP 倍率を反映して要約する。</summary>
    public static WaveProfile? From(Game game, Wave? wave)
    {
        if (wave is null)
        {
            return null;
        }

        var waves = game.WaveManager;
        var groups = wave.Entries
            .GroupBy(e => e.EnemyType)
            .Select(g =>
            {
                var spec = game.GetEnemySpec(g.Key);
                int hp = Math.Max(1, (int)MathF.Round(spec.MaxHp * wave.HpMultiplier));
                return new WaveGroup(spec, g.Sum(waves.ScaledCount), hp);
            })
            .Where(g => g.Count > 0)
            .ToList();

        if (groups.Count == 0)
        {
            return null;
        }

        float duration = wave.Entries.Max(e => e.StartDelay + Math.Max(0, waves.ScaledCount(e) - 1) * e.Interval);
        return new WaveProfile(groups, duration);
    }
}
