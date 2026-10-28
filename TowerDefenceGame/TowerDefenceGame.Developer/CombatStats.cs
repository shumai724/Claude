using TowerDefenceGame.Models.Core;
using TowerDefenceGame.Models.Entities;
using TowerDefenceGame.Models.Waves;

namespace TowerDefenceGame.Developer;

/// <summary>
/// タワー1基が与えたダメージと撃破数。
/// ダメージは実際に減らした HP で数え、倒しきった後の超過分（オーバーキル）は含めない。
/// </summary>
internal sealed class CombatStats
{
    /// <summary>設置してから与えたダメージの合計。</summary>
    public long TotalDamage { get; private set; }

    /// <summary>直近に始まったウェーブで与えたダメージ。</summary>
    public long WaveDamage { get; private set; }

    /// <summary>とどめを刺した敵の数。</summary>
    public int Kills { get; private set; }

    /// <summary>ウェーブ進行中に設置されていた時間（ゲーム内の秒）。DPS の計算に使う。</summary>
    public float ActiveSeconds { get; private set; }

    /// <summary>ウェーブ進行中の平均 DPS。</summary>
    public float DamagePerSecond => ActiveSeconds > 0f ? TotalDamage / ActiveSeconds : 0f;

    public void RecordDamage(int amount)
    {
        TotalDamage += amount;
        WaveDamage += amount;
    }

    public void RecordKill() => Kills++;

    public void BeginWave() => WaveDamage = 0;

    public void AddActiveTime(float seconds) => ActiveSeconds += seconds;
}

/// <summary>
/// ゲームのイベント（敵へのダメージ・撃破・ウェーブ開始・更新）を見て、タワーごとの <see cref="CombatStats"/> を集計する。
/// ゲーム本体は何も記録しないので、集計はすべてここで行う。
/// </summary>
internal sealed class CombatTracker
{
    private readonly Dictionary<Tower, CombatStats> _stats = [];
    private Game? _game;

    /// <summary>新しいゲームの集計を始める。前のゲームの集計は捨てる。</summary>
    public void Attach(Game game)
    {
        if (_game is not null)
        {
            _game.World.EnemyDamaged -= OnEnemyDamaged;
            _game.World.EnemyKilled -= OnEnemyKilled;
            _game.WaveManager.WaveStarted -= OnWaveStarted;
            _game.Updated -= OnUpdated;
        }

        _game = game;
        _stats.Clear();
        game.World.EnemyDamaged += OnEnemyDamaged;
        game.World.EnemyKilled += OnEnemyKilled;
        game.WaveManager.WaveStarted += OnWaveStarted;
        game.Updated += OnUpdated;
    }

    /// <summary>そのタワーの集計。まだ何もしていなければ空の集計を返す。</summary>
    public CombatStats For(Tower tower)
    {
        if (!_stats.TryGetValue(tower, out var stats))
        {
            stats = new CombatStats();
            _stats[tower] = stats;
        }

        return stats;
    }

    private void OnEnemyDamaged(Enemy enemy, Tower? source, int amount)
    {
        if (source is not null)
        {
            For(source).RecordDamage(amount);
        }
    }

    private void OnEnemyKilled(Enemy enemy)
    {
        if (enemy.LastHitBy is { } tower)
        {
            For(tower).RecordKill();
        }
    }

    private void OnWaveStarted(Wave wave)
    {
        foreach (var stats in _stats.Values)
        {
            stats.BeginWave();
        }
    }

    private void OnUpdated(float deltaTime)
    {
        if (_game is not { WaveManager.IsWaveInProgress: true } game)
        {
            return;
        }

        foreach (var tower in game.World.Towers)
        {
            For(tower).AddActiveTime(deltaTime);
        }
    }
}
