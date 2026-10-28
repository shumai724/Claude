using TowerDefenceGame.Controllers;
using TowerDefenceGame.Models.Core;
using TowerDefenceGame.Models.Data;
using TowerDefenceGame.Views.Controls;

namespace TowerDefenceGame.Views.Forms;

/// <summary>メインウィンドウ。ユーザー操作をイベントとして Controller に伝え、状態の表示だけを行う。</summary>
public sealed class MainForm : Form
{
    private const int SidePanelWidth = 240;
    private const int SidePadding = 8;
    private const int ContentWidth = SidePanelWidth - SidePadding * 2;

    private readonly FlowLayoutPanel _sidePanel;
    private readonly Button _nextWaveButton;
    private readonly Button _shareButton;
    private readonly Button _pauseButton;
    private readonly Button _speedButton;
    private readonly Button _restartButton;
    private readonly Button _settingsButton;

    public MainForm()
    {
        Text = "Tower Defence";
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        AutoScaleMode = AutoScaleMode.None;
        Font = new Font("Meiryo UI", 9f);

        Canvas = new GameCanvas { Location = Point.Empty, Size = new Size(800, 556) };
        ShopPanel = new TowerShopPanel { Width = ContentWidth, Height = 202 };
        InfoPanel = new TowerInfoPanel { Width = ContentWidth, Height = 230 };

        _nextWaveButton = CreateButton("次のウェーブ", ContentWidth, 40, () => NextWaveRequested?.Invoke());
        _nextWaveButton.Font = new Font(Font, FontStyle.Bold);

        // クリアしたら「次のウェーブ」と入れ替えて出す（同じ大きさなので配置は動かない）
        _shareButton = CreateButton("X でクリアを共有 ↗", ContentWidth, 40, () => ShareRequested?.Invoke());
        _shareButton.Font = _nextWaveButton.Font;
        _shareButton.Visible = false;
        // ボタンの左右マージン（各3px）込みで ContentWidth に収まる幅にする
        _pauseButton = CreateButton("一時停止", 66, 30, () => PauseRequested?.Invoke());
        _speedButton = CreateButton("速度 x1", 66, 30, () => SpeedRequested?.Invoke());
        _restartButton = CreateButton("リスタート", 72, 30, () => RestartRequested?.Invoke());

        var controlRow = new FlowLayoutPanel { Width = ContentWidth, Height = 36, Margin = Padding.Empty, WrapContents = false };
        controlRow.Controls.AddRange([_pauseButton, _speedButton, _restartButton]);

        _settingsButton = CreateButton("⚙ 設定", 96, 30, () => SettingsRequested?.Invoke());
        var stageSelectButton = CreateButton("◀ ステージ選択", 116, 30, () => StageSelectRequested?.Invoke());

        var optionRow = new FlowLayoutPanel { Width = ContentWidth, Height = 36, Margin = Padding.Empty, WrapContents = false };
        optionRow.Controls.AddRange([_settingsButton, stageSelectButton]);

        var helpLabel = new Label
        {
            AutoSize = false,
            Width = ContentWidth,
            Height = 34,
            ForeColor = SystemColors.GrayText,
            Text = "Space：次ウェーブ　P：停止　F：倍速\n1〜7：建設　Esc：解除　M：消音",
        };

        _sidePanel = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Width = SidePanelWidth,
            Padding = new Padding(SidePadding),
        };
        _sidePanel.Controls.AddRange([ShopPanel, InfoPanel, _nextWaveButton, _shareButton, controlRow, optionRow, helpLabel]);

        StageSelectView = new StageSelectView { Dock = DockStyle.Fill, Visible = false };

        Controls.Add(Canvas);
        Controls.Add(_sidePanel);
        Controls.Add(StageSelectView);
        LayoutPanels();
    }

    public StageSelectView StageSelectView { get; }

    public GameCanvas Canvas { get; }

    public TowerShopPanel ShopPanel { get; }

    public TowerInfoPanel InfoPanel { get; }

    public event Action? NextWaveRequested;

    public event Action? PauseRequested;

    public event Action? SpeedRequested;

    public event Action? RestartRequested;

    public event Action? CancelRequested;

    /// <summary>ゲーム画面からステージ選択画面に戻りたいとき。</summary>
    public event Action? StageSelectRequested;

    /// <summary>M キーで BGM・効果音の ON/OFF を切り替えたいとき。</summary>
    public event Action? SoundToggleRequested;

    /// <summary>「設定」ボタンが押されたとき。</summary>
    public event Action? SettingsRequested;

    /// <summary>クリア後の「X でクリアを共有」が押されたとき。</summary>
    public event Action? ShareRequested;

    /// <summary>数字キー 1〜9 が押されたときに 0 始まりの番号を通知する。</summary>
    public event Action<int>? TowerHotkeyPressed;

    public void Bind(Game game, InputController input)
    {
        Canvas.Bind(game, input);
        ShopPanel.SetTowers(game.TowerSpecs);
        Text = $"Tower Defence - {game.StageName}" + (game.Level > DifficultyRules.NormalLevel ? $"（Lv{game.Level}）" : "");
        LayoutPanels();
    }

    public void ShowStageSelect()
    {
        Text = "Tower Defence - ステージ選択";
        Canvas.Visible = false;
        _sidePanel.Visible = false;
        StageSelectView.Visible = true;
        StageSelectView.BringToFront();
    }

    public void ShowGame()
    {
        StageSelectView.Visible = false;
        Canvas.Visible = true;
        _sidePanel.Visible = true;
    }

    /// <summary>消音中であることを「設定」ボタンに表示する。</summary>
    public void ShowSoundEnabled(bool enabled)
    {
        _settingsButton.Text = enabled ? "⚙ 設定" : "⚙ 消音中";
        StageSelectView.ShowSoundEnabled(enabled);
    }

    public void UpdateControls(Game game, InputController input, float timeScale)
    {
        bool active = !game.IsOver;
        var waves = game.WaveManager;

        ShopPanel.UpdateState(game.Player.Gold, input.SelectedTowerType, active);
        InfoPanel.ShowTower(input.SelectedTower, game.Player.Gold, active);

        bool cleared = game.State == GameState.StageClear;
        _shareButton.Visible = cleared;
        _nextWaveButton.Visible = !cleared;
        _nextWaveButton.Enabled = game.State == GameState.Playing && waves.CanStartNextWave;
        _nextWaveButton.Text = waves.HasNextWave
            ? $"次のウェーブ ▶（{waves.CurrentWaveNumber + 1}/{waves.TotalWaves}）"
            : "最終ウェーブ";
        _pauseButton.Enabled = game.State is GameState.Playing or GameState.Paused;
        _pauseButton.Text = game.State == GameState.Paused ? "再開" : "一時停止";
        _speedButton.Text = $"速度 x{timeScale:0}";
    }

    public void ShowError(string message) =>
        MessageBox.Show(this, message, "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);

    public bool Confirm(string message) =>
        MessageBox.Show(this, message, "確認", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        // ゲーム用のショートカットはステージ選択画面では使わない
        if (StageSelectView.Visible)
        {
            return base.ProcessCmdKey(ref msg, keyData);
        }

        // ボタンにフォーカスがあってもショートカットが効くよう、ここで先に処理する
        switch (keyData)
        {
            case Keys.Space:
                NextWaveRequested?.Invoke();
                return true;
            case Keys.P:
                PauseRequested?.Invoke();
                return true;
            case Keys.F:
                SpeedRequested?.Invoke();
                return true;
            case Keys.Escape:
                CancelRequested?.Invoke();
                return true;
            case Keys.M:
                SoundToggleRequested?.Invoke();
                return true;
            case >= Keys.D1 and <= Keys.D9:
                TowerHotkeyPressed?.Invoke(keyData - Keys.D1);
                return true;
            case >= Keys.NumPad1 and <= Keys.NumPad9:
                TowerHotkeyPressed?.Invoke(keyData - Keys.NumPad1);
                return true;
            default:
                return base.ProcessCmdKey(ref msg, keyData);
        }
    }

    private static Button CreateButton(string text, int width, int height, Action onClick)
    {
        var button = new Button
        {
            Text = text,
            Width = width,
            Height = height,
            FlatStyle = FlatStyle.Flat,
            TabStop = false,
        };
        button.Click += (_, _) => onClick();
        return button;
    }

    private void LayoutPanels()
    {
        int height = Math.Max(Canvas.Height, _sidePanel.PreferredSize.Height);
        Canvas.Height = height;
        _sidePanel.Location = new Point(Canvas.Width, 0);
        _sidePanel.Height = height;
        ClientSize = new Size(Canvas.Width + SidePanelWidth, height);
    }
}
