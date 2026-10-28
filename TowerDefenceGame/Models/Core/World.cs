using System.Numerics;
using TowerDefenceGame.Models.Entities;

namespace TowerDefenceGame.Models.Core;

/// <summary>フィールド上に存在するタワー・敵・弾をまとめて管理する。</summary>
public sealed class World
{
    private readonly List<Tower> _towers = [];
    private readonly List<Enemy> _enemies = [];
    private readonly List<Projectile> _projectiles = [];

    public IReadOnlyList<Tower> Towers => _towers;

    public IReadOnlyList<Enemy> Enemies => _enemies;

    public IReadOnlyList<Projectile> Projectiles => _projectiles;

    public bool HasActiveEnemies => _enemies.Exists(e => e.IsActive);

    /// <summary>ウェーブ進行中か。金鉱はこの間だけゴールドを生む。Game が毎フレーム設定する。</summary>
    public bool IsWaveInProgress { get; set; }

    public event Action<Enemy>? EnemyKilled;

    public event Action<Enemy>? EnemyReachedGoal;

    /// <summary>敵の HP が減ったとき（敵・攻撃したタワー・減った量）。</summary>
    public event Action<Enemy, Tower?, int>? EnemyDamaged;

    /// <summary>弾が着弾して効果を与えたときに発生する。</summary>
    public event Action<Projectile>? ProjectileHit;

    /// <summary>金鉱がゴールドを生み出したときに発生する。</summary>
    public event Action<GoldMine, int>? GoldGenerated;

    public void AddTower(Tower tower) => _towers.Add(tower);

    public void RemoveTower(Tower tower)
    {
        tower.Deactivate();
        _towers.Remove(tower);
    }

    public void AddEnemy(Enemy enemy)
    {
        enemy.Killed += OnEnemyKilled;
        enemy.ReachedGoal += OnEnemyReachedGoal;
        enemy.Damaged += OnEnemyDamaged;
        _enemies.Add(enemy);
    }

    public void AddProjectile(Projectile projectile) => _projectiles.Add(projectile);

    public IEnumerable<Enemy> FindEnemiesInRange(Vector2 center, float radius)
    {
        float radiusSquared = radius * radius;
        foreach (var enemy in _enemies)
        {
            if (enemy.IsActive && Vector2.DistanceSquared(center, enemy.Position) <= radiusSquared)
            {
                yield return enemy;
            }
        }
    }

    /// <summary>
    /// その位置にいる攻撃タワーが受ける強化。司令塔の範囲が重なっている場合は、
    /// 攻撃力・攻撃速度それぞれで最も大きい値を採用する（足し算はしない）。
    /// </summary>
    public TowerBuff GetBuffAt(Vector2 position)
    {
        float damage = 0f;
        float fireRate = 0f;
        float range = 0f;
        int extraTargets = 0;
        foreach (var tower in _towers)
        {
            if (tower is SupportTower { IsActive: true } support && support.Covers(position))
            {
                damage = MathF.Max(damage, support.DamageBonus);
                fireRate = MathF.Max(fireRate, support.FireRateBonus);
                range = MathF.Max(range, support.RangeBonus);
                extraTargets = Math.Max(extraTargets, support.ExtraTargets);
            }
        }

        return new TowerBuff(damage, fireRate, range, extraTargets);
    }

    public void Update(float deltaTime)
    {
        // 更新中に弾が追加されるため、foreach ではなくインデックスで回す
        for (int i = 0; i < _towers.Count; i++)
        {
            if (_towers[i].IsActive)
            {
                _towers[i].Update(deltaTime, this);
            }
        }

        for (int i = 0; i < _enemies.Count; i++)
        {
            if (_enemies[i].IsActive)
            {
                _enemies[i].Update(deltaTime, this);
            }
        }

        for (int i = 0; i < _projectiles.Count; i++)
        {
            if (_projectiles[i].IsActive)
            {
                _projectiles[i].Update(deltaTime, this);
            }
        }
    }

    public void RemoveInactive()
    {
        _enemies.RemoveAll(e => !e.IsActive);
        _projectiles.RemoveAll(p => !p.IsActive);
    }

    internal void NotifyProjectileHit(Projectile projectile) => ProjectileHit?.Invoke(projectile);

    internal void NotifyGoldGenerated(GoldMine mine, int amount) => GoldGenerated?.Invoke(mine, amount);

    private void OnEnemyKilled(Enemy enemy) => EnemyKilled?.Invoke(enemy);

    private void OnEnemyReachedGoal(Enemy enemy) => EnemyReachedGoal?.Invoke(enemy);

    private void OnEnemyDamaged(Enemy enemy, Tower? source, int amount) => EnemyDamaged?.Invoke(enemy, source, amount);
}
