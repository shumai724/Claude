using TowerDefenceGame.Models.Core;
using TowerDefenceGame.Models.Data;
using TowerDefenceGame.Models.Map;

namespace TowerDefenceGame.Models.Waves;

/// <summary>ウェーブの開始・敵の出現・終了判定を管理する。</summary>
public sealed class WaveManager
{
    private readonly IReadOnlyList<Wave> _waves;
    private readonly EnemyFactory _enemyFactory;
    private readonly Route _route;
    private readonly List<SpawnCursor> _cursors = [];
    private int _currentIndex = -1;
    private float _elapsed;

    public WaveManager(IReadOnlyList<Wave> waves, EnemyFactory enemyFactory, Route route)
    {
        _waves = waves;
        _enemyFactory = enemyFactory;
        _route = route;
    }

    /// <summary>直近に開始したウェーブの番号（1始まり）。未開始なら 0。</summary>
    public int CurrentWaveNumber => _currentIndex + 1;

    public int TotalWaves => _waves.Count;

    public Wave? NextWave => HasNextWave ? _waves[_currentIndex + 1] : null;

    /// <summary>直近に開始したウェーブ。まだ1つも始めていなければ null。</summary>
    public Wave? CurrentWave => _currentIndex >= 0 ? _waves[_currentIndex] : null;

    public bool IsWaveInProgress { get; private set; }

    public bool HasNextWave => _currentIndex + 1 < _waves.Count;

    public bool CanStartNextWave => !IsWaveInProgress && HasNextWave;

    public bool AllWavesCleared => !IsWaveInProgress && !HasNextWave;

    /// <summary>
    /// 敵の数に掛ける倍率。1 で定義どおり（ゲーム本体は常に 1 のまま使う）。次に始めるウェーブから反映される。
    /// </summary>
    public float EnemyCountMultiplier { get; set; } = 1f;

    /// <summary>今のウェーブで、これから出てくる敵の数。</summary>
    public int RemainingToSpawn => IsWaveInProgress ? _cursors.Sum(c => c.Count - c.Spawned) : 0;

    /// <summary>倍率を反映した、出現定義1つ分の敵の数。0 にはしない（定義が 0 体のときだけ 0）。</summary>
    public int ScaledCount(SpawnEntry entry) =>
        entry.Count <= 0 ? 0 : Math.Max(1, (int)MathF.Round(entry.Count * EnemyCountMultiplier));

    /// <summary>倍率を反映した、ウェーブ全体の敵の数。</summary>
    public int TotalEnemiesOf(Wave wave) => wave.Entries.Sum(ScaledCount);

    public event Action<Wave>? WaveStarted;

    public event Action<Wave>? WaveCompleted;

    public bool StartNextWave()
    {
        if (!CanStartNextWave)
        {
            return false;
        }

        _currentIndex++;
        _elapsed = 0f;
        _cursors.Clear();
        var wave = _waves[_currentIndex];
        _cursors.AddRange(wave.Entries.Select(e => new SpawnCursor(e, ScaledCount(e))));

        IsWaveInProgress = true;
        WaveStarted?.Invoke(wave);
        return true;
    }

    public void Update(float deltaTime, World world)
    {
        if (!IsWaveInProgress)
        {
            return;
        }

        var wave = _waves[_currentIndex];
        _elapsed += deltaTime;
        foreach (var cursor in _cursors)
        {
            while (!cursor.IsDone && _elapsed >= cursor.NextSpawnTime)
            {
                world.AddEnemy(_enemyFactory.Create(cursor.Entry.EnemyType, _route, wave.HpMultiplier));
                cursor.Spawned++;
            }
        }

        if (_cursors.TrueForAll(c => c.IsDone) && !world.HasActiveEnemies)
        {
            IsWaveInProgress = false;
            WaveCompleted?.Invoke(wave);
        }
    }

    /// <summary>出現定義ごとの進み具合。</summary>
    private sealed class SpawnCursor(SpawnEntry entry, int count)
    {
        public SpawnEntry Entry { get; } = entry;

        /// <summary>倍率を反映した出現数。</summary>
        public int Count { get; } = count;

        public int Spawned { get; set; }

        public bool IsDone => Spawned >= Count;

        public float NextSpawnTime => Entry.StartDelay + Spawned * Entry.Interval;
    }
}
