using TowerDefenceGame.Models.Core;
using TowerDefenceGame.Models.Entities;
using TowerDefenceGame.Models.Waves;
using TowerDefenceGame.Views.Audio;

namespace TowerDefenceGame.Controllers;

/// <summary>Model と入力のイベントを購読し、対応する効果音と BGM を鳴らす。</summary>
public sealed class SoundController
{
    private readonly ISoundPlayer _player;
    private Game? _game;
    private GameState _lastState;

    public SoundController(ISoundPlayer player, InputController input)
    {
        _player = player;
        input.Feedback += OnInputFeedback;
    }

    /// <summary>新しいゲームのイベントを購読する。前のゲームの購読は解除する。</summary>
    public void Attach(Game game)
    {
        if (_game is not null)
        {
            Subscribe(_game, false);
        }

        _game = game;
        _lastState = game.State;
        Subscribe(game, true);
        _player.PlayMusic(MusicTrack.Battle);
    }

    public void Play(SoundEffect effect) => _player.Play(effect);

    private void Subscribe(Game game, bool subscribe)
    {
        if (subscribe)
        {
            game.TowerPlaced += OnTowerPlaced;
            game.TowerUpgraded += OnTowerUpgraded;
            game.TowerSold += OnTowerSold;
            game.StateChanged += OnStateChanged;
            game.World.ProjectileHit += OnProjectileHit;
            game.World.EnemyKilled += OnEnemyKilled;
            game.World.EnemyReachedGoal += OnEnemyReachedGoal;
            game.World.GoldGenerated += OnGoldGenerated;
            game.WaveManager.WaveStarted += OnWaveStarted;
            game.WaveManager.WaveCompleted += OnWaveCompleted;
        }
        else
        {
            game.TowerPlaced -= OnTowerPlaced;
            game.TowerUpgraded -= OnTowerUpgraded;
            game.TowerSold -= OnTowerSold;
            game.StateChanged -= OnStateChanged;
            game.World.ProjectileHit -= OnProjectileHit;
            game.World.EnemyKilled -= OnEnemyKilled;
            game.World.EnemyReachedGoal -= OnEnemyReachedGoal;
            game.World.GoldGenerated -= OnGoldGenerated;
            game.WaveManager.WaveStarted -= OnWaveStarted;
            game.WaveManager.WaveCompleted -= OnWaveCompleted;
        }
    }

    private void OnInputFeedback(InputFeedback feedback) => _player.Play(feedback switch
    {
        InputFeedback.Selected => SoundEffect.Select,
        InputFeedback.Cancelled => SoundEffect.Cancel,
        _ => SoundEffect.Rejected,
    });

    private void OnTowerPlaced(Tower tower) => _player.Play(SoundEffect.TowerPlaced);

    private void OnTowerUpgraded(Tower tower, UpgradeStat stat) =>
        _player.Play(tower.IsMastered(stat) ? SoundEffect.TowerMastered : SoundEffect.TowerUpgraded);

    private void OnTowerSold(Tower tower) => _player.Play(SoundEffect.TowerSold);

    private void OnProjectileHit(Projectile projectile) => _player.Play(projectile switch
    {
        CannonBall => SoundEffect.CannonHit,
        FrostShot => SoundEffect.FrostHit,
        BreakerShot => SoundEffect.BreakerHit,
        LightningBolt => SoundEffect.LightningHit,
        _ => SoundEffect.ArrowHit,
    });

    private void OnGoldGenerated(GoldMine mine, int amount) => _player.Play(SoundEffect.GoldGenerated);

    private void OnEnemyKilled(Enemy enemy) => _player.Play(SoundEffect.EnemyKilled);

    private void OnEnemyReachedGoal(Enemy enemy) => _player.Play(SoundEffect.EnemyReachedGoal);

    private void OnWaveStarted(Wave wave) => _player.Play(SoundEffect.WaveStarted);

    private void OnWaveCompleted(Wave wave)
    {
        // 最終ウェーブはこの直後にステージクリアの音が鳴るので重ねない
        if (_game?.WaveManager.HasNextWave == true)
        {
            _player.Play(SoundEffect.WaveCompleted);
        }
    }

    private void OnStateChanged(GameState state)
    {
        var previous = _lastState;
        _lastState = state;
        switch (state)
        {
            case GameState.Playing when previous == GameState.Paused:
                _player.Play(SoundEffect.Select);
                break;
            case GameState.GameOver:
                // BGM を止めて勝敗のジングルを聞かせる
                _player.PlayMusic(MusicTrack.None);
                _player.Play(SoundEffect.GameOver);
                break;
            case GameState.StageClear:
                _player.PlayMusic(MusicTrack.None);
                _player.Play(SoundEffect.StageClear);
                break;
            case GameState.Paused:
                _player.Play(SoundEffect.Cancel);
                break;
        }
    }
}
