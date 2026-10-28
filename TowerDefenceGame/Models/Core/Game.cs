using TowerDefenceGame.Models.Data;
using TowerDefenceGame.Models.Entities;
using TowerDefenceGame.Models.Map;
using TowerDefenceGame.Models.Waves;

namespace TowerDefenceGame.Models.Core;

/// <summary>1ステージ分のゲーム進行を管理するモデルのルート。</summary>
public sealed class Game
{
    private const int ScorePerReward = 10;

    private readonly TowerFactory _towerFactory;
    private readonly EnemyFactory _enemyFactory;

    public Game(StageData stage, TowerFactory towerFactory, EnemyFactory enemyFactory)
    {
        ArgumentNullException.ThrowIfNull(stage);
        ArgumentNullException.ThrowIfNull(towerFactory);
        ArgumentNullException.ThrowIfNull(enemyFactory);

        foreach (var entry in stage.Waves.SelectMany(w => w.Entries))
        {
            if (!enemyFactory.CanCreate(entry.EnemyType))
            {
                throw new ArgumentException($"敵 {entry.EnemyType} の定義がありません。", nameof(enemyFactory));
            }
        }

        _towerFactory = towerFactory;
        _enemyFactory = enemyFactory;
        StageName = stage.Name;
        Level = stage.Level;
        Map = stage.Map;
        Player = new Player(stage.InitialGold, stage.InitialLives);
        World = new World();
        WaveManager = new WaveManager(stage.Waves, enemyFactory, Map.Route);

        World.EnemyKilled += OnEnemyKilled;
        World.EnemyReachedGoal += OnEnemyReachedGoal;
        World.GoldGenerated += OnGoldGenerated;
        WaveManager.WaveCompleted += OnWaveCompleted;
    }

    public string StageName { get; }

    /// <summary>遊んでいる難易度レベル。</summary>
    public int Level { get; }

    public GameState State { get; private set; } = GameState.Ready;

    public GameMap Map { get; }

    public Player Player { get; }

    public World World { get; }

    public WaveManager WaveManager { get; }

    public IEnumerable<TowerSpec> TowerSpecs => _towerFactory.Specs;

    public bool IsOver => State is GameState.GameOver or GameState.StageClear;


    public event Action<GameState>? StateChanged;

    public event Action<Tower>? TowerPlaced;

    /// <summary>建物のいずれかの項目を強化したときに発生する。</summary>
    public event Action<Tower, UpgradeStat>? TowerUpgraded;

    public event Action<Tower>? TowerSold;

    /// <summary>プレイ中に1回更新するたびに、進めたゲーム内の秒数を通知する（倍速の分を含む）。</summary>
    public event Action<float>? Updated;

    public void Start()
    {
        if (State == GameState.Ready)
        {
            SetState(GameState.Playing);
        }
    }

    public void Update(float deltaTime)
    {
        if (State != GameState.Playing)
        {
            return;
        }

        WaveManager.Update(deltaTime, World);
        World.IsWaveInProgress = WaveManager.IsWaveInProgress;
        World.Update(deltaTime);
        World.RemoveInactive();
        Updated?.Invoke(deltaTime);
        CheckEndCondition();
    }

    public void Pause()
    {
        if (State == GameState.Playing)
        {
            SetState(GameState.Paused);
        }
    }

    public void Resume()
    {
        if (State == GameState.Paused)
        {
            SetState(GameState.Playing);
        }
    }

    public bool StartNextWave() => State == GameState.Playing && WaveManager.StartNextWave();

    public TowerSpec GetTowerSpec(TowerType type) => _towerFactory.GetSpec(type);

    /// <summary>難易度の補正を掛けた後の敵のステータス。</summary>
    public EnemySpec GetEnemySpec(EnemyType type) => _enemyFactory.GetSpec(type);

    /// <summary>
    /// マップに置かずにタワーを作る。強化した後のステータスを前もって確かめたいときに使う。
    /// 作ったタワーはゲームには一切影響しない。
    /// </summary>
    public Tower CreatePreview(TowerType type, GridPos cell) => _towerFactory.Create(type, cell, Map.CellToWorld(cell));

    public Tower? GetTowerAt(GridPos cell) => Map.GetTile(cell)?.Tower;

    public bool CanPlaceTower(TowerType type, GridPos cell) =>
        !IsOver && Map.CanBuild(cell) && Player.CanAfford(GetTowerSpec(type).Cost);

    public bool TryPlaceTower(TowerType type, GridPos cell)
    {
        if (!CanPlaceTower(type, cell))
        {
            return false;
        }

        Player.TrySpend(GetTowerSpec(type).Cost);
        var tower = _towerFactory.Create(type, cell, Map.CellToWorld(cell));
        Map.GetTile(cell)!.PlaceTower(tower);
        World.AddTower(tower);
        TowerPlaced?.Invoke(tower);
        return true;
    }

    public bool CanUpgradeTower(Tower tower, UpgradeStat stat) =>
        !IsOver && tower.IsActive && tower.CanUpgrade(stat) && Player.CanAfford(tower.GetUpgradeCost(stat));

    public bool TryUpgradeTower(Tower tower, UpgradeStat stat)
    {
        if (!CanUpgradeTower(tower, stat))
        {
            return false;
        }

        Player.TrySpend(tower.GetUpgradeCost(stat));
        tower.Upgrade(stat);
        TowerUpgraded?.Invoke(tower, stat);
        return true;
    }

    public bool SellTower(Tower tower)
    {
        if (IsOver || !tower.IsActive)
        {
            return false;
        }

        int price = tower.SellPrice;
        Map.GetTile(tower.Cell)?.RemoveTower();
        World.RemoveTower(tower);
        Player.Earn(price);
        TowerSold?.Invoke(tower);
        return true;
    }

    private void OnEnemyKilled(Enemy enemy)
    {
        Player.Earn(enemy.Spec.Reward);
        Player.AddScore(enemy.Spec.Reward * ScorePerReward);
    }

    private void OnEnemyReachedGoal(Enemy enemy) => Player.LoseLife(enemy.Spec.DamageToBase);

    private void OnWaveCompleted(Wave wave) => Player.Earn(wave.Bonus);

    private void OnGoldGenerated(GoldMine mine, int amount) => Player.Earn(amount);

    private void CheckEndCondition()
    {
        if (Player.IsDead)
        {
            SetState(GameState.GameOver);
        }
        else if (WaveManager.AllWavesCleared)
        {
            SetState(GameState.StageClear);
        }
    }

    private void SetState(GameState state)
    {
        if (State == state)
        {
            return;
        }

        State = state;
        StateChanged?.Invoke(state);
    }
}
