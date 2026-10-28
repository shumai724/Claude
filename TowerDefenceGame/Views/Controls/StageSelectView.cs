using TowerDefenceGame.Models.Data;
using TowerDefenceGame.Models.Progress;

namespace TowerDefenceGame.Views.Controls;

/// <summary>ステージ選択画面。ステージのカードを並べ、クリックされたステージを通知する。</summary>
public sealed class StageSelectView : UserControl
{
    private static readonly Color Background = Color.FromArgb(32, 36, 44);

    private readonly FlowLayoutPanel _cards;
    private readonly Label _errorLabel;
    private readonly Button _settingsButton;

    public StageSelectView()
    {
        BackColor = Background;

        var title = new Label
        {
            Text = "ステージ選択",
            Dock = DockStyle.Fill,
            ForeColor = Color.White,
            Font = new Font("Meiryo UI", 22f, FontStyle.Bold, GraphicsUnit.Pixel),
            Padding = new Padding(4, 2, 0, 0),
        };
        _settingsButton = new Button
        {
            Text = "⚙ 設定",
            Dock = DockStyle.Right,
            Width = 120,
            FlatStyle = FlatStyle.Flat,
            ForeColor = Color.White,
            TabStop = false,
        };
        _settingsButton.FlatAppearance.BorderColor = Color.FromArgb(110, 118, 132);
        _settingsButton.Click += (_, _) => SettingsRequested?.Invoke();

        var header = new Panel { Dock = DockStyle.Top, Height = 56, Padding = new Padding(12, 12, 16, 8) };
        header.Controls.Add(title);
        header.Controls.Add(_settingsButton);
        var subtitle = new Label
        {
            Text = "遊ぶステージとレベルを選んでください。レベルをクリアすると次のレベルが解放されます（数字にカーソルを合わせると内容を表示）。",
            Dock = DockStyle.Top,
            Height = 30,
            ForeColor = Color.FromArgb(170, 178, 192),
            Padding = new Padding(18, 4, 0, 0),
        };
        _cards = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            Padding = new Padding(10, 6, 10, 10),
        };
        _errorLabel = new Label
        {
            Dock = DockStyle.Bottom,
            AutoSize = false,
            Height = 0,
            ForeColor = Color.FromArgb(255, 170, 90),
            Padding = new Padding(16, 0, 16, 8),
        };

        // Dock は後から追加したものほど先に配置されるので、Fill を最初に追加する
        Controls.Add(_cards);
        Controls.Add(_errorLabel);
        Controls.Add(subtitle);
        Controls.Add(header);
    }

    /// <summary>ステージとレベルが選ばれたとき。</summary>
    public event Action<StageSummary, int>? StageSelected;

    public event Action? SettingsRequested;

    public void ShowSoundEnabled(bool enabled) => _settingsButton.Text = enabled ? "⚙ 設定" : "⚙ 消音中";

    /// <param name="levelsFor">ステージごとの各レベルの補正（Lv1 から順）。</param>
    public void SetStages(
        IReadOnlyList<StageSummary> stages, ProgressStore progress, Func<StageSummary, IReadOnlyList<DifficultyRules>> levelsFor, IReadOnlyList<string> errors)
    {
        _cards.SuspendLayout();
        foreach (Control card in _cards.Controls.Cast<Control>().ToList())
        {
            _cards.Controls.Remove(card);
            card.Dispose();
        }

        for (int i = 0; i < stages.Count; i++)
        {
            var card = new StageCard(stages[i], progress.Get(stages[i].Id), i + 1, levelsFor(stages[i]));
            card.LevelSelected += level => StageSelected?.Invoke(card.Stage, level);
            _cards.Controls.Add(card);
        }

        _cards.ResumeLayout();

        _errorLabel.Text = errors.Count == 0 ? "" : "読み込めなかったファイル:\n" + string.Join("\n", errors);
        _errorLabel.Height = errors.Count == 0 ? 0 : 24 + errors.Count * 16;
    }
}
