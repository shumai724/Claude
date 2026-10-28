using TowerDefenceGame.Models.Settings;
using TowerDefenceGame.Views.Audio;

namespace TowerDefenceGame.Views.Forms;

/// <summary>
/// システム設定のダイアログ。編集中の設定は <see cref="PreviewChanged"/> でその場で通知し、
/// OK で確定、キャンセルなら呼び出し側が元の設定に戻す。
/// </summary>
public sealed class SettingsDialog : Form
{
    private const int SliderWidth = 220;

    private readonly AudioSettings _settings;
    private readonly CheckBox _enabledCheckBox;
    private readonly (TrackBar Slider, Label Value) _master;
    private readonly Dictionary<SoundCategory, (TrackBar Slider, Label Value)> _categories = [];
    private bool _updating;

    public SettingsDialog(AudioSettings settings, bool audioAvailable)
    {
        _settings = settings.Clone();

        Text = "設定";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        AutoScaleMode = AutoScaleMode.None;
        Font = new Font("Meiryo UI", 9f);
        ClientSize = new Size(440, 466);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(16, 12, 16, 8),
            ColumnCount = 3,
            AutoSize = true,
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, SliderWidth + 6));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 50));

        layout.Controls.Add(Heading("サウンド"), 0, 0);
        layout.SetColumnSpan(layout.GetControlFromPosition(0, 0)!, 3);

        _enabledCheckBox = new CheckBox { Text = "BGM・効果音を鳴らす（ゲーム中は M キーでも切り替え）", AutoSize = true, Checked = _settings.Enabled };
        _enabledCheckBox.CheckedChanged += (_, _) =>
        {
            if (_updating)
            {
                return;
            }

            _settings.Enabled = _enabledCheckBox.Checked;
            UpdateEnabledState();
            PreviewChanged?.Invoke(_settings.Clone());
            if (_settings.Enabled)
            {
                SampleRequested?.Invoke(SoundEffect.Select);
            }
        };
        layout.Controls.Add(_enabledCheckBox, 0, 1);
        layout.SetColumnSpan(_enabledCheckBox, 3);

        _master = AddSliderRow(layout, 2, "マスター音量", bold: true, onChanged: value => _settings.MasterVolume = value,
            sample: SoundEffect.WaveCompleted);

        int row = 3;
        foreach (var category in SoundCategories.All)
        {
            _categories[category] = AddSliderRow(layout, row++, SoundCategories.DisplayName(category), bold: false,
                onChanged: value => _settings.SetCategoryVolume(category, value), sample: SoundCategories.Sample(category));
        }

        var hint = new Label
        {
            AutoSize = true,
            ForeColor = SystemColors.GrayText,
            Margin = new Padding(3, 8, 3, 3),
            Text = audioAvailable
                ? "スライダーを離すと、その種類の音を試聴できます（BGM は流れている曲で確認）。"
                : "音声デバイスが見つからないため、BGM・効果音は鳴りません。",
        };
        layout.Controls.Add(hint, 0, row);
        layout.SetColumnSpan(hint, 3);

        var resetButton = new Button { Text = "初期値に戻す", Width = 110, Height = 30 };
        var okButton = new Button { Text = "OK", Width = 90, Height = 30, DialogResult = DialogResult.OK };
        var cancelButton = new Button { Text = "キャンセル", Width = 90, Height = 30, DialogResult = DialogResult.Cancel };
        resetButton.Click += (_, _) => ResetToDefaults();

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            FlowDirection = FlowDirection.RightToLeft,
            Height = 48,
            Padding = new Padding(12, 8, 12, 8),
        };
        buttons.Controls.AddRange([cancelButton, okButton, resetButton]);

        Controls.Add(layout);
        Controls.Add(buttons);
        AcceptButton = okButton;
        CancelButton = cancelButton;

        ShowValues();
    }

    /// <summary>編集中の設定。OK で閉じたときの確定値として使う。</summary>
    public AudioSettings Settings => _settings.Clone();

    /// <summary>設定が変わるたびに発生する。呼び出し側はすぐに音へ反映する。</summary>
    public event Action<AudioSettings>? PreviewChanged;

    /// <summary>試聴したい音があるときに発生する。</summary>
    public event Action<SoundEffect>? SampleRequested;

    private static Label Heading(string text) => new()
    {
        Text = text,
        AutoSize = true,
        Font = new Font("Meiryo UI", 11f, FontStyle.Bold),
        Margin = new Padding(3, 0, 3, 6),
    };

    private static int ToPercent(float volume) => (int)MathF.Round(volume * 100f);

    private (TrackBar Slider, Label Value) AddSliderRow(
        TableLayoutPanel layout, int row, string caption, bool bold, Action<float> onChanged, SoundEffect? sample)
    {
        var label = new Label
        {
            Text = caption,
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Font = bold ? new Font(Font, FontStyle.Bold) : Font,
        };
        var slider = new TrackBar
        {
            Minimum = 0,
            Maximum = 100,
            TickStyle = TickStyle.None,
            SmallChange = 5,
            LargeChange = 10,
            Width = SliderWidth,
            AutoSize = false,
            Height = 30,
        };
        var value = new Label { AutoSize = true, Anchor = AnchorStyles.Left };

        slider.ValueChanged += (_, _) =>
        {
            value.Text = $"{slider.Value}%";
            if (_updating)
            {
                return;
            }

            onChanged(slider.Value / 100f);
            PreviewChanged?.Invoke(_settings.Clone());
        };

        // ドラッグ中に鳴らし続けないよう、操作を終えたときにだけ試聴する
        if (sample is SoundEffect effect)
        {
            slider.MouseUp += (_, _) => SampleRequested?.Invoke(effect);
            slider.KeyUp += (_, _) => SampleRequested?.Invoke(effect);
        }

        layout.Controls.Add(label, 0, row);
        layout.Controls.Add(slider, 1, row);
        layout.Controls.Add(value, 2, row);
        return (slider, value);
    }

    private void ResetToDefaults()
    {
        var defaults = new AudioSettings();
        _settings.Enabled = defaults.Enabled;
        _settings.MasterVolume = defaults.MasterVolume;
        _settings.CategoryVolumes.Clear();
        ShowValues();
        PreviewChanged?.Invoke(_settings.Clone());
        SampleRequested?.Invoke(SoundEffect.Select);
    }

    private void ShowValues()
    {
        _updating = true;
        _enabledCheckBox.Checked = _settings.Enabled;
        _master.Slider.Value = ToPercent(_settings.MasterVolume);
        _master.Value.Text = $"{_master.Slider.Value}%";
        foreach (var (category, (slider, value)) in _categories)
        {
            slider.Value = ToPercent(_settings.GetCategoryVolume(category));
            value.Text = $"{slider.Value}%";
        }

        _updating = false;
        UpdateEnabledState();
    }

    /// <summary>音を切っている間は音量スライダーを操作できないようにする。</summary>
    private void UpdateEnabledState()
    {
        _master.Slider.Enabled = _settings.Enabled;
        foreach (var (slider, _) in _categories.Values)
        {
            slider.Enabled = _settings.Enabled;
        }
    }
}
