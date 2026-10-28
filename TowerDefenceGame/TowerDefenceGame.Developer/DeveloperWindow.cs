using TowerDefenceGame.Models.Core;
using TowerDefenceGame.Models.Entities;

namespace TowerDefenceGame.Developer;

/// <summary>
/// 開発者モードのウィンドウ。敵の数の倍率を変える操作と、タワーごとのダメージの一覧を表示する。
/// メインウィンドウの横に開いたままにして使う（ゲームは止めない）。
/// </summary>
internal sealed class DeveloperWindow : Form
{
    private static readonly decimal[] Presets = [0.5m, 1m, 2m, 5m];

    private readonly NumericUpDown _multiplierInput;
    private readonly Label _summaryLabel;
    private readonly Label _totalLabel;
    private readonly ListView _towerList;
    private readonly CheckBox _botCheckBox;
    private readonly CheckBox _unlockCheckBox;
    private readonly Label _botStatusLabel;
    private readonly TextBox _botLog;
    private int _shownLogVersion = -1;
    private readonly Font _selectedFont;
    private bool _updating;

    public DeveloperWindow()
    {
        Text = "開発者モード（F12 で開閉）";
        FormBorderStyle = FormBorderStyle.SizableToolWindow;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        AutoScaleMode = AutoScaleMode.None;
        Font = new Font("Meiryo UI", 9f);
        ClientSize = new Size(640, 668);
        MinimumSize = new Size(480, 420);
        _selectedFont = new Font(Font, FontStyle.Bold);

        // --- 敵の数 ---
        var multiplierCaption = new Label { Text = "敵の数の倍率", AutoSize = true, Margin = new Padding(3, 7, 6, 3) };
        _multiplierInput = new NumericUpDown
        {
            Minimum = (decimal)DeveloperSettings.MinEnemyCountMultiplier,
            Maximum = (decimal)DeveloperSettings.MaxEnemyCountMultiplier,
            Increment = 0.1m,
            DecimalPlaces = 1,
            Value = 1m,
            Width = 64,
            Margin = new Padding(3, 4, 8, 3),
        };
        _multiplierInput.ValueChanged += (_, _) =>
        {
            if (!_updating)
            {
                EnemyCountMultiplierChanged?.Invoke((float)_multiplierInput.Value);
            }
        };

        var multiplierRow = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 34, WrapContents = false, Padding = new Padding(8, 4, 8, 0) };
        multiplierRow.Controls.Add(multiplierCaption);
        multiplierRow.Controls.Add(_multiplierInput);
        foreach (decimal preset in Presets)
        {
            var button = new Button { Text = $"×{preset:0.#}", Width = 52, Height = 26, FlatStyle = FlatStyle.Flat, TabStop = false };
            button.Click += (_, _) => _multiplierInput.Value = preset;
            multiplierRow.Controls.Add(button);
        }

        var multiplierNote = new Label
        {
            Dock = DockStyle.Top,
            Height = 22,
            Padding = new Padding(12, 2, 8, 0),
            ForeColor = SystemColors.GrayText,
            Text = "次に始めるウェーブから反映されます。開発者版のクリア記録は、製品版とは別のファイルに保存されます。",
        };

        // --- 全レベル解放 ---
        _unlockCheckBox = new CheckBox
        {
            Text = "全レベル解放（ステージ選択ですべてのレベルを選べる。クリア記録は変えない）",
            AutoSize = true,
            Margin = new Padding(3, 4, 3, 3),
        };
        _unlockCheckBox.CheckedChanged += (_, _) =>
        {
            if (!_updating)
            {
                UnlockAllLevelsChanged?.Invoke(_unlockCheckBox.Checked);
            }
        };
        var unlockRow = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 28, WrapContents = false, Padding = new Padding(8, 2, 8, 0) };
        unlockRow.Controls.Add(_unlockCheckBox);

        // --- bot ---
        _botCheckBox = new CheckBox
        {
            Text = "bot に自動プレイさせる（建設・強化・ウェーブ開始を自動で行う）",
            AutoSize = true,
            Margin = new Padding(3, 4, 3, 3),
        };
        _botCheckBox.CheckedChanged += (_, _) => BotEnabledChanged?.Invoke(_botCheckBox.Checked);
        var botRow = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 28, WrapContents = false, Padding = new Padding(8, 2, 8, 0) };
        botRow.Controls.Add(_botCheckBox);
        _botStatusLabel = new Label { Dock = DockStyle.Top, Height = 22, Padding = new Padding(12, 2, 8, 0), ForeColor = Color.MediumPurple };

        // --- 状況 ---
        _summaryLabel = new Label { Dock = DockStyle.Top, Height = 24, Padding = new Padding(12, 6, 8, 0) };
        _totalLabel = new Label { Dock = DockStyle.Top, Height = 24, Padding = new Padding(12, 2, 8, 0), Font = _selectedFont };

        // --- タワーごとのダメージ ---
        _towerList = new BufferedListView
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            GridLines = true,
            HeaderStyle = ColumnHeaderStyle.Nonclickable,
            MultiSelect = false,
        };
        AddColumn("#", 32, HorizontalAlignment.Right);
        AddColumn("タワー", 84, HorizontalAlignment.Left);
        AddColumn("位置", 56, HorizontalAlignment.Left);
        AddColumn("強化（攻/射/速）", 120, HorizontalAlignment.Left);
        AddColumn("総ダメージ", 82, HorizontalAlignment.Right);
        AddColumn("今ウェーブ", 76, HorizontalAlignment.Right);
        AddColumn("撃破", 48, HorizontalAlignment.Right);
        AddColumn("DPS", 56, HorizontalAlignment.Right);
        AddColumn("割合", 52, HorizontalAlignment.Right);

        var listPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8, 4, 8, 4) };
        listPanel.Controls.Add(_towerList);

        var logCaption = new Label { Dock = DockStyle.Top, Height = 20, Padding = new Padding(0, 2, 0, 0), Text = "bot の判断ログ（新しいものが下）" };
        _botLog = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            WordWrap = true,
            BackColor = SystemColors.Window,
            Font = new Font("Meiryo UI", 8.5f),
        };
        var logPanel = new Panel { Dock = DockStyle.Bottom, Height = 170, Padding = new Padding(8, 0, 8, 4) };
        logPanel.Controls.Add(_botLog);
        logPanel.Controls.Add(logCaption);

        var footnote = new Label
        {
            Dock = DockStyle.Bottom,
            Height = 52,
            Padding = new Padding(12, 4, 8, 4),
            ForeColor = SystemColors.GrayText,
            Text = "ダメージは実際に減らしたHPで数えます（倒しきった後の超過分は含みません）。DPS はウェーブ進行中の平均です。\n"
                + "司令塔の強化・ブレイカーの防御力低下・フロストの凍傷で増えた分は、攻撃したタワーの側に入ります。\n"
                + "選択中のタワーは太字で表示します。",
        };

        // Dock は後から追加したものほど外側（上）に積まれるので、下に置きたいものから順に追加する
        Controls.Add(listPanel);
        Controls.Add(logPanel);
        Controls.Add(footnote);
        Controls.Add(_totalLabel);
        Controls.Add(_summaryLabel);
        Controls.Add(_botStatusLabel);
        Controls.Add(botRow);
        Controls.Add(unlockRow);
        Controls.Add(multiplierNote);
        Controls.Add(multiplierRow);
    }

    /// <summary>倍率が変更されたときに発生する。</summary>
    public event Action<float>? EnemyCountMultiplierChanged;

    /// <summary>閉じるボタンか F12 で、開発者モードを終えたいときに発生する。</summary>
    public event Action? CloseRequested;

    /// <summary>「全レベル解放」のチェックが切り替わったときに発生する。</summary>
    public event Action<bool>? UnlockAllLevelsChanged;

    /// <summary>「全レベル解放」の表示を合わせる（変更イベントは発生させない）。</summary>
    public void SetUnlockAllLevels(bool unlockAll)
    {
        _updating = true;
        _unlockCheckBox.Checked = unlockAll;
        _updating = false;
    }

    /// <summary>bot のチェックが切り替わったときに発生する。</summary>
    public event Action<bool>? BotEnabledChanged;

    /// <summary>bot に自動プレイさせるか（チェックの状態）。</summary>
    public bool BotEnabled => _botCheckBox.Checked;

    /// <summary>bot の状況と判断ログを表示する。ログは増えた分だけ書き足し、一番下までスクロールする。</summary>
    public void ShowBot(string status, IReadOnlyList<string> log, int logVersion)
    {
        _botStatusLabel.Text = $"bot：{status}";
        if (logVersion == _shownLogVersion)
        {
            return;
        }

        _shownLogVersion = logVersion;
        _botLog.Text = string.Join(Environment.NewLine, log);
        _botLog.SelectionStart = _botLog.TextLength;
        _botLog.ScrollToCaret();
    }

    /// <summary>倍率の表示を合わせる（変更イベントは発生させない）。</summary>
    public void SetMultiplier(float multiplier)
    {
        _updating = true;
        _multiplierInput.Value = Math.Clamp((decimal)MathF.Round(multiplier, 1), _multiplierInput.Minimum, _multiplierInput.Maximum);
        _updating = false;
    }

    /// <summary>ゲームの状況とタワーごとのダメージを表示し直す。</summary>
    public void ShowStats(Game? game, Tower? selected, CombatTracker tracker)
    {
        if (game is null)
        {
            _summaryLabel.Text = "ステージを選ぶと、ここにタワーごとのダメージが表示されます。";
            _totalLabel.Text = "";
            _towerList.Items.Clear();
            return;
        }

        ShowSummary(game);

        var towers = game.World.Towers
            .Where(t => t is AttackTower && t.IsActive)
            .OrderByDescending(t => tracker.For(t).TotalDamage)
            .ToList();
        long total = towers.Sum(t => tracker.For(t).TotalDamage);
        long waveTotal = towers.Sum(t => tracker.For(t).WaveDamage);
        _totalLabel.Text = $"全タワーの合計ダメージ {total:N0}（このウェーブ {waveTotal:N0}）　撃破 {towers.Sum(t => tracker.For(t).Kills):N0} 体";

        _towerList.BeginUpdate();
        _towerList.Items.Clear();
        for (int i = 0; i < towers.Count; i++)
        {
            var tower = towers[i];
            var stats = tracker.For(tower);
            double share = total > 0 ? (double)stats.TotalDamage / total : 0;
            var item = new ListViewItem(new[]
            {
                $"{i + 1}",
                tower.Spec.Name,
                $"{tower.Cell.Col}, {tower.Cell.Row}",
                LevelText(tower),
                $"{stats.TotalDamage:N0}",
                $"{stats.WaveDamage:N0}",
                $"{stats.Kills:N0}",
                $"{stats.DamagePerSecond:0.0}",
                $"{share:0.0%}",
            });

            if (tower == selected)
            {
                item.Font = _selectedFont;
                item.BackColor = Color.LightYellow;
            }

            _towerList.Items.Add(item);
        }

        _towerList.EndUpdate();
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == Keys.F12)
        {
            CloseRequested?.Invoke();
            return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        // 閉じるボタンでは破棄せずに隠すだけにする（アプリ終了時などはそのまま閉じる）
        if (e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            CloseRequested?.Invoke();
            return;
        }

        base.OnFormClosing(e);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _selectedFont.Dispose();
        }

        base.Dispose(disposing);
    }

    /// <summary>強化レベルを「攻/射/速」の順に並べる。Lv5 には ★ を付ける。</summary>
    private static string LevelText(Tower tower) =>
        string.Join(" / ", tower.UpgradeStats.Select(s => tower.IsMastered(s) ? $"{tower.GetLevel(s)}★" : $"{tower.GetLevel(s)}"));

    private void ShowSummary(Game game)
    {
        var waves = game.WaveManager;
        int onField = game.World.Enemies.Count(e => e.IsActive);
        string progress = waves.IsWaveInProgress
            ? $"ウェーブ {waves.CurrentWaveNumber}/{waves.TotalWaves} 進行中　フィールドの敵 {onField} 体・出現待ち {waves.RemainingToSpawn} 体"
            : $"ウェーブ {waves.CurrentWaveNumber}/{waves.TotalWaves}";

        string next = waves.NextWave is { } wave
            ? $"　次のウェーブ {waves.TotalEnemiesOf(wave)} 体（定義 {wave.TotalEnemies} 体）"
            : "";

        _summaryLabel.Text = progress + next;
    }

    private void AddColumn(string text, int width, HorizontalAlignment alignment) =>
        _towerList.Columns.Add(new ColumnHeader { Text = text, Width = width, TextAlign = alignment });

    /// <summary>0.5秒ごとに作り直してもちらつかないよう、ダブルバッファを有効にした ListView。</summary>
    private sealed class BufferedListView : ListView
    {
        public BufferedListView()
        {
            DoubleBuffered = true;
        }
    }
}
