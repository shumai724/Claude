using TowerDefenceGame.Controllers;
using TowerDefenceGame.Developer.Bot;
using TowerDefenceGame.Models.Core;
using TowerDefenceGame.Views.Forms;
using TowerDefenceGame.Views.Rendering;

namespace TowerDefenceGame.Developer;

/// <summary>
/// 開発者版で足す機能をまとめて受け持つ。製品版の画面・Controller には、汎用のイベントを通してだけ関わる。
/// <list type="bullet">
/// <item>開発者ウィンドウ（起動時に開き、F12 で開閉）</item>
/// <item>敵の数の倍率をゲームに反映する</item>
/// <item>全レベル解放（記録は変えずに、ステージ選択でロックを外す）</item>
/// <item>タワーごとのダメージを集計し、ウィンドウとマップ上に表示する</item>
/// <item>bot（自動プレイ）を動かす</item>
/// </list>
/// </summary>
internal sealed class DeveloperController : IDisposable
{
    private const int RefreshIntervalMs = 500;
    private const int WindowGap = 8;

    private readonly MainForm _view;
    private readonly AppController _app;
    private readonly GameController _gameController;
    private readonly DeveloperSettingsStore _store;
    private readonly DeveloperSettings _settings;
    private readonly CombatTracker _tracker = new();
    private readonly DeveloperPainter _painter = new();
    private readonly AutoPlayer _bot = new();
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = RefreshIntervalMs };
    private readonly DeveloperWindow _window = new();
    private bool _overlayVisible = true;

    public DeveloperController(MainForm view, AppController app, DeveloperSettingsStore store)
    {
        _view = view;
        _app = app;
        _gameController = app.GameController;
        _store = store;
        _settings = store.Load();

        // ステージ選択画面を最初に作るより前に設定しておく
        _app.Progress.UnlockAllLevels = _settings.UnlockAllLevels;

        _gameController.GameLoaded += OnGameLoaded;
        _gameController.Ticked += OnTicked;
        _view.Canvas.OverlayPainting += OnOverlayPainting;

        // 製品版のショートカット処理より後に F12 を受け取る（製品版は F12 を使わない）
        _view.KeyPreview = true;
        _view.KeyDown += OnKeyDown;

        _window.SetMultiplier(_settings.EnemyCountMultiplier);
        _window.SetUnlockAllLevels(_settings.UnlockAllLevels);
        _window.EnemyCountMultiplierChanged += OnMultiplierChanged;
        _window.UnlockAllLevelsChanged += OnUnlockAllLevelsChanged;
        _window.CloseRequested += Toggle;
        _window.BotEnabledChanged += _ => RefreshWindow();
        _timer.Tick += (_, _) => RefreshWindow();

        // メインウィンドウの位置が決まってから、その横に開く
        _view.Shown += (_, _) => ShowWindow();
    }

    public void Dispose()
    {
        _timer.Stop();
        _timer.Dispose();
        _window.Dispose();
    }

    private void OnGameLoaded(Game game)
    {
        game.WaveManager.EnemyCountMultiplier = _settings.EnemyCountMultiplier;
        _tracker.Attach(game);
    }

    /// <summary>bot は、ウィンドウのチェックが入っているときだけ動かす。</summary>
    private void OnTicked(float realDelta)
    {
        _bot.Enabled = _window.BotEnabled;
        if (_bot.Enabled && _gameController.CurrentGame is { } game)
        {
            _bot.Update(game, realDelta);
        }
    }

    private void OnOverlayPainting(IRenderer renderer, Game game)
    {
        if (_overlayVisible)
        {
            _painter.Draw(renderer, game, _tracker);
        }
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.F12)
        {
            Toggle();
            e.Handled = true;
        }
    }

    /// <summary>ウィンドウとマップ上の表示をまとめて開閉する。閉じている間も bot と集計は動き続ける。</summary>
    private void Toggle()
    {
        if (_window.Visible)
        {
            _timer.Stop();
            _window.Hide();
            _overlayVisible = false;
            _view.Activate();
        }
        else
        {
            ShowWindow();
        }

        _view.Canvas.Invalidate();
    }

    private void OnMultiplierChanged(float multiplier)
    {
        _settings.EnemyCountMultiplier = multiplier;
        _settings.Normalize();
        _store.Save(_settings);
        if (_gameController.CurrentGame is { } game)
        {
            game.WaveManager.EnemyCountMultiplier = _settings.EnemyCountMultiplier;
        }

        RefreshWindow();
    }

    private void OnUnlockAllLevelsChanged(bool unlockAll)
    {
        _settings.UnlockAllLevels = unlockAll;
        _store.Save(_settings);
        _app.Progress.UnlockAllLevels = unlockAll;
        _app.RefreshStageSelect();
    }

    private void ShowWindow()
    {
        _overlayVisible = true;
        if (!_window.Visible)
        {
            _window.Location = ChooseWindowLocation(_window.Size);
            _window.Show(_view);

            // キーボード操作（Space・数字キーなど）はゲーム画面で受けたいので、フォーカスを戻す
            _view.Activate();
        }

        RefreshWindow();
        _timer.Start();
    }

    private void RefreshWindow()
    {
        if (!_window.Visible)
        {
            return;
        }

        _window.ShowStats(_gameController.CurrentGame, _gameController.SelectedTower, _tracker);
        _window.ShowBot(_bot.Enabled ? _bot.Status : "停止中", _bot.Log, _bot.LogVersion);
    }

    /// <summary>メインウィンドウの右隣に置く。画面からはみ出す場合は左隣、それも無理なら画面内に収める。</summary>
    private Point ChooseWindowLocation(Size size)
    {
        var area = Screen.FromControl(_view).WorkingArea;
        int x = _view.Right + WindowGap;
        if (x + size.Width > area.Right)
        {
            x = _view.Left - WindowGap - size.Width;
        }

        x = Math.Clamp(x, area.Left, Math.Max(area.Left, area.Right - size.Width));
        int y = Math.Clamp(_view.Top, area.Top, Math.Max(area.Top, area.Bottom - size.Height));
        return new Point(x, y);
    }
}
