using System.Text.Json;
using TowerDefenceGame.Models.Core;
using TowerDefenceGame.Models.Data;
using TowerDefenceGame.Models.Entities;
using TowerDefenceGame.Views.Audio;
using TowerDefenceGame.Views.Forms;

namespace TowerDefenceGame.Controllers;

/// <summary>
/// ゲーム画面を担当する。ゲームループを回し、View のイベントを Model への操作に結び付ける。
/// 選択されたステージの読み込みとリスタートもここで行う。
/// </summary>
public sealed class GameController : IDisposable
{
    private const int FrameIntervalMs = 15;
    private const float MaxFrameTime = 0.1f;
    private const float FastTimeScale = 2f;

    /// <summary>司令塔の強化や金鉱の進み具合など、イベントの無い変化を詳細パネルに反映する間隔（秒）。</summary>
    private const float PanelRefreshInterval = 0.25f;

    private readonly MainForm _view;
    private readonly string _assetsDirectory;
    private readonly InputController _input = new();
    private readonly SoundController _sound;
    private readonly GameClock _clock = new();
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = FrameIntervalMs };
    private StageSummary? _stage;
    private DifficultyRules _rules = DifficultyRules.Normal;
    private Game? _game;
    private float _timeScale = 1f;
    private float _sincePanelRefresh;

    public GameController(MainForm view, string assetsDirectory, ISoundPlayer soundPlayer)
    {
        _view = view;
        _assetsDirectory = assetsDirectory;
        _sound = new SoundController(soundPlayer, _input);

        _timer.Tick += OnTick;
        _input.SelectionChanged += RefreshView;

        _view.Canvas.CellClicked += _input.HandleCellClick;
        _view.Canvas.CellHovered += _input.SetHoverCell;
        _view.Canvas.CancelRequested += _input.CancelSelection;
        _view.ShopPanel.TowerTypeSelected += _input.SelectTowerType;
        _view.InfoPanel.UpgradeRequested += _input.UpgradeSelectedTower;
        _view.InfoPanel.SellRequested += _input.SellSelectedTower;
        _view.InfoPanel.TargetingSelected += _input.SetTargeting;
        _view.CancelRequested += _input.CancelSelection;
        _view.TowerHotkeyPressed += OnTowerHotkeyPressed;
        _view.NextWaveRequested += OnNextWaveRequested;
        _view.PauseRequested += OnPauseRequested;
        _view.SpeedRequested += OnSpeedRequested;
        _view.RestartRequested += OnRestartRequested;
        _view.StageSelectRequested += OnStageSelectRequested;
    }

    /// <summary>プレイヤーがステージ選択画面に戻ることを決めたときに発生する。</summary>
    public event Action? ExitRequested;

    /// <summary>ステージをクリアしたときに発生する。</summary>
    public event Action<StageSummary, Game>? StageCleared;

    /// <summary>ゲームを読み込んだ直後（最初のウェーブより前）に発生する。リスタートでも発生する。</summary>
    public event Action<Game>? GameLoaded;

    /// <summary>遊んでいる（または最後に遊んだ）ゲーム。まだ読み込んでいなければ null。</summary>
    public Game? CurrentGame => _game;

    /// <summary>ゲームループの1回ごとに、実時間の経過秒数を通知する（bot の思考に使う）。</summary>
    public event Action<float>? Ticked;

    /// <summary>プレイヤーが選択中のタワー。</summary>
    public Tower? SelectedTower => _input.SelectedTower;

    /// <summary>ステージを読み込んでゲームループを開始する。読み込みに失敗したら false。</summary>
    public bool StartStage(StageSummary stage, DifficultyRules rules)
    {
        var (previousStage, previousRules) = (_stage, _rules);
        (_stage, _rules) = (stage, rules);
        if (!TryLoadGame())
        {
            (_stage, _rules) = (previousStage, previousRules);
            return false;
        }

        _clock.Reset();
        _timer.Start();
        return true;
    }

    /// <summary>ゲームループを止める。ステージ選択画面に戻るときに呼ぶ。</summary>
    public void Stop() => _timer.Stop();

    /// <summary>プレイ中なら一時停止する。設定画面などを開く前に呼び、戻り値を <see cref="ResumeIf"/> に渡す。</summary>
    public bool PauseIfPlaying()
    {
        if (_game is not { State: GameState.Playing })
        {
            return false;
        }

        _game.Pause();
        return true;
    }

    public void ResumeIf(bool wasPlaying)
    {
        if (wasPlaying)
        {
            _game?.Resume();
        }
    }

    public void Dispose()
    {
        _timer.Stop();
        _timer.Dispose();
    }

    private bool TryLoadGame()
    {
        Game game;
        try
        {
            game = LoadGame();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException
                                       or InvalidDataException or ArgumentException or KeyNotFoundException)
        {
            _view.ShowError($"ステージの読み込みに失敗しました。\n\n{ex.Message}");
            return false;
        }

        _game = game;
        _timeScale = 1f;
        game.Player.Changed += RefreshView;
        game.StateChanged += OnStateChanged;
        game.WaveManager.WaveStarted += _ => RefreshView();
        game.WaveManager.WaveCompleted += _ => RefreshView();

        _sound.Attach(game);
        _input.Attach(game);
        _view.Bind(game, _input);
        GameLoaded?.Invoke(game);
        game.Start();
        RefreshView();
        return true;
    }

    private Game LoadGame()
    {
        if (_stage is null)
        {
            throw new InvalidOperationException("ステージが選択されていません。");
        }

        // 難易度の補正は、読み込んだ定義に掛けてから使う（定義ファイル自体は共通）
        var towerSpecs = _rules.Apply(SpecLoader.LoadTowerSpecs(Path.Combine(_assetsDirectory, "towers.json")));
        var enemySpecs = _rules.Apply(SpecLoader.LoadEnemySpecs(Path.Combine(_assetsDirectory, "enemies.json")));

        // マップのマスはタワー設置で書き換わるので、プレイのたびにファイルから読み直す
        var stage = StageLoader.Load(_stage.FilePath, _rules);
        return new Game(stage, new TowerFactory(towerSpecs), new EnemyFactory(enemySpecs));
    }

    private void OnStateChanged(GameState state)
    {
        RefreshView();
        if (state == GameState.StageClear && _stage is not null && _game is not null)
        {
            StageCleared?.Invoke(_stage, _game);
        }
    }

    private void OnStageSelectRequested()
    {
        // タワーを置いたりウェーブを始めたりした後なら、進行が失われることを確認する
        bool hasProgress = _game is { IsOver: false }
            && (_game.WaveManager.CurrentWaveNumber > 0 || _game.World.Towers.Count > 0);
        if (hasProgress && !_view.Confirm("ステージ選択に戻りますか？\n現在のプレイ内容は失われます。"))
        {
            return;
        }

        ExitRequested?.Invoke();
    }

    private void OnTick(object? sender, EventArgs e)
    {
        if (_game is null)
        {
            return;
        }

        // ウィンドウのドラッグなどで長く止まった直後に敵がワープしないよう上限を設ける
        float realDelta = MathF.Min(_clock.Tick(), MaxFrameTime);
        _game.Update(realDelta * _timeScale);
        Ticked?.Invoke(realDelta);
        _view.Canvas.Invalidate();

        _sincePanelRefresh += realDelta;
        if (_sincePanelRefresh >= PanelRefreshInterval)
        {
            _sincePanelRefresh = 0f;
            RefreshView();
        }
    }

    private void OnTowerHotkeyPressed(int index)
    {
        if (_game?.TowerSpecs.ElementAtOrDefault(index) is { } spec)
        {
            _input.SelectTowerType(spec.Type);
        }
    }

    private void OnNextWaveRequested()
    {
        // 開始できたときの音は WaveStarted イベント経由で SoundController が鳴らす
        if (_game is { IsOver: false } && !_game.StartNextWave())
        {
            _sound.Play(SoundEffect.Rejected);
        }
    }

    private void OnPauseRequested()
    {
        if (_game is null)
        {
            return;
        }

        if (_game.State == GameState.Paused)
        {
            _game.Resume();
        }
        else
        {
            _game.Pause();
        }
    }

    private void OnSpeedRequested()
    {
        _timeScale = _timeScale == 1f ? FastTimeScale : 1f;
        _sound.Play(SoundEffect.Select);
        RefreshView();
    }

    private void OnRestartRequested()
    {
        if (_game is { IsOver: false } && !_view.Confirm("最初からやり直しますか？"))
        {
            return;
        }

        TryLoadGame();
    }

    private void RefreshView()
    {
        if (_game is not null)
        {
            _view.UpdateControls(_game, _input, _timeScale);
        }
    }
}
