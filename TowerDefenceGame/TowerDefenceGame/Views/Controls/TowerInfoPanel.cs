using TowerDefenceGame.Models.Entities;
using TowerDefenceGame.Models.Targeting;

namespace TowerDefenceGame.Views.Controls;

/// <summary>
/// 選択中の建物の詳細表示。強化は項目ごとに行い、各行に「Lv・今の値・強化ボタン」を並べる。
/// Lv5 にできるのは1項目だけなので、他の項目が Lv5 になると残りは Lv4 で止まる。
/// </summary>
public sealed class TowerInfoPanel : UserControl
{
    private const int MaxRows = 3;
    private const int RowTop = 26;
    private const int RowHeight = 30;
    private const int ButtonWidth = 76;

    private static readonly Color MasteryColor = Color.FromArgb(200, 120, 0);

    private readonly Label _titleLabel;
    private readonly (Label Label, Button Button)[] _rows = new (Label, Button)[MaxRows];
    private readonly Label _detailLabel;
    private readonly Label _targetingLabel;
    private readonly ComboBox _targetingBox;
    private readonly Button _sellButton;
    private readonly ToolTip _toolTip = new();
    private Tower? _tower;
    private bool _updating;

    public TowerInfoPanel()
    {
        _titleLabel = new Label { Location = new Point(0, 0), AutoSize = false, Height = 24, Font = new Font(Font, FontStyle.Bold) };
        Controls.Add(_titleLabel);

        for (int i = 0; i < MaxRows; i++)
        {
            int y = RowTop + i * RowHeight;
            var label = new Label { Location = new Point(0, y), AutoSize = false, Height = RowHeight - 2, TextAlign = ContentAlignment.MiddleLeft };
            var button = new Button { Location = new Point(0, y + 1), Size = new Size(ButtonWidth, RowHeight - 4), FlatStyle = FlatStyle.Flat, TabStop = false };
            int row = i;
            button.Click += (_, _) => OnUpgradeClicked(row);
            _rows[i] = (label, button);
            Controls.AddRange([label, button]);
        }

        _detailLabel = new Label { Location = new Point(0, RowTop + MaxRows * RowHeight + 2), AutoSize = false, Height = 46, ForeColor = MasteryColor };
        _targetingLabel = new Label { Location = new Point(0, 176), AutoSize = true, Text = "攻撃対象" };
        _targetingBox = new ComboBox
        {
            Location = new Point(70, 172),
            Width = 120,
            DropDownStyle = ComboBoxStyle.DropDownList,
            DisplayMember = nameof(ITargetingStrategy.Name),
            TabStop = false,
        };
        _sellButton = new Button { Location = new Point(0, 200), Height = 28, FlatStyle = FlatStyle.Flat, TabStop = false };

        foreach (var strategy in TargetingStrategies.All)
        {
            _targetingBox.Items.Add(strategy);
        }

        _targetingBox.SelectedIndexChanged += OnTargetingChanged;
        _sellButton.Click += (_, _) => SellRequested?.Invoke();

        Controls.AddRange([_detailLabel, _targetingLabel, _targetingBox, _sellButton]);
        ShowTower(null, 0, false);
    }

    public event Action<UpgradeStat>? UpgradeRequested;

    public event Action? SellRequested;

    public event Action<ITargetingStrategy>? TargetingSelected;

    public void ShowTower(Tower? tower, int gold, bool enabled)
    {
        _tower = tower;
        bool canTarget = tower is AttackTower;
        _targetingLabel.Visible = canTarget;
        _targetingBox.Visible = canTarget;
        _sellButton.Visible = tower is not null;

        if (tower is null)
        {
            _titleLabel.Text = "タワー未選択";
            _detailLabel.ForeColor = SystemColors.ControlText;
            _detailLabel.Text = "マップ上のタワーをクリックすると\n詳細を表示します。";
            foreach (var (label, button) in _rows)
            {
                label.Visible = false;
                button.Visible = false;
            }

            return;
        }

        _titleLabel.Text = tower.MasteredStat is { } mastered
            ? $"{tower.Spec.Name}　★{UpgradeTexts.MasteryName(tower, mastered)}"
            : tower.Spec.Name;

        for (int i = 0; i < MaxRows; i++)
        {
            bool visible = i < tower.UpgradeStats.Count;
            _rows[i].Label.Visible = visible;
            _rows[i].Button.Visible = visible;
            if (visible)
            {
                ShowRow(_rows[i], tower, tower.UpgradeStats[i], gold, enabled);
            }
        }

        _detailLabel.ForeColor = tower.MasteredStat is null ? SystemColors.GrayText : MasteryColor;
        _detailLabel.Text = string.Join("\n", DetailLines(tower));

        if (tower is AttackTower attack)
        {
            _updating = true;
            _targetingBox.SelectedItem = attack.Targeting;
            _targetingBox.Enabled = enabled;
            _updating = false;
        }

        _sellButton.Text = $"売却（+{tower.SellPrice}G）";
        _sellButton.Enabled = enabled;
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        int width = ClientSize.Width;
        _titleLabel.Width = width;
        _detailLabel.Width = width;
        _sellButton.Width = width;
        foreach (var (label, button) in _rows)
        {
            label.Width = width - ButtonWidth - 4;
            button.Left = width - ButtonWidth;
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _toolTip.Dispose();
        }

        base.Dispose(disposing);
    }

    private void ShowRow((Label Label, Button Button) row, Tower tower, UpgradeStat stat, int gold, bool enabled)
    {
        int level = tower.GetLevel(stat);
        string star = tower.IsMastered(stat) ? "★" : "";
        row.Label.Text = $"{UpgradeTexts.StatName(stat)} {star}Lv{level}　{UpgradeTexts.StatValue(tower, stat)}";
        row.Label.ForeColor = tower.IsMastered(stat) ? MasteryColor : SystemColors.ControlText;

        string mastery = $"Lv5 効果「{UpgradeTexts.MasteryName(tower, stat)}」\n{UpgradeTexts.MasteryDescription(tower, stat)}";
        if (tower.IsMastered(stat))
        {
            row.Button.Text = "MAX";
            row.Button.Enabled = false;
            _toolTip.SetToolTip(row.Button, mastery);
        }
        else if (tower.IsBlockedByMastery(stat))
        {
            // 他の項目がすでに Lv5 なので、この項目は Lv4 で打ち止め
            row.Button.Text = "上限";
            row.Button.Enabled = false;
            _toolTip.SetToolTip(row.Button, "Lv5 にできるのは1つの項目だけです。");
        }
        else
        {
            int cost = tower.GetUpgradeCost(stat);
            bool toMastery = level == Tower.MaxLevel - 1;
            row.Button.Text = toMastery ? $"★{cost}G" : $"+{cost}G";
            row.Button.Enabled = enabled && gold >= cost;
            _toolTip.SetToolTip(row.Button, $"Lv{level + 1} に強化（{cost}G）\n\n{mastery}"
                + (toMastery ? "\n\n※ Lv5 にできるのは1つの項目だけです。" : ""));
        }
    }

    /// <summary>行の下に出す補足：Lv5 効果か、タワー固有の性能。</summary>
    private static IEnumerable<string> DetailLines(Tower tower)
    {
        // 1行がパネル幅に収まるよう短く書く（詳しい説明はボタンのツールチップに出す）
        if (tower.MasteredStat is { } mastered)
        {
            yield return $"★{UpgradeTexts.MasteryName(tower, mastered)}：{UpgradeTexts.MasteryShort(tower, mastered)}";
        }

        switch (tower)
        {
            case AttackTower { Buff.IsActive: true } buffed:
                yield return $"司令塔 攻+{buffed.Buff.DamageBonus:P0} 速+{buffed.Buff.FireRateBonus:P0}"
                    + (buffed.Buff.RangeBonus > 0 ? $" 射+{buffed.Buff.RangeBonus:P0}" : "")
                    + (buffed.Buff.ExtraTargets > 0 ? " 同時+1" : "");
                break;
            case GoldMine mine:
                yield return $"次の産出 {mine.Progress:P0}" + (mine.IsRushing ? "（ラッシュ中）" : "");
                yield return "※ウェーブ中のみ産出";
                break;
        }

        string? special = tower switch
        {
            ArcherTower { RapidFireBonus: > 0f } archer => $"速射中 攻撃速度+{archer.RapidFireBonus:P0}",
            CannonTower cannon => $"爆風範囲 {cannon.SplashRadius:F0}（射程で拡大）",
            FrostTower frost => $"減速 {frost.SlowRate:P0}・{frost.SlowDuration:F1}秒（攻撃力で上昇）",
            BreakerTower breaker when breaker.MaxArmorReduction > breaker.ArmorReduction =>
                $"防御 -{breaker.ArmorReduction}（最大-{breaker.MaxArmorReduction}）・{breaker.ArmorBreakDuration:F1}秒",
            BreakerTower breaker => $"防御 -{breaker.ArmorReduction}・{breaker.ArmorBreakDuration:F1}秒（攻撃力で上昇）",
            TeslaTower tesla => $"連鎖 {tesla.ChainCount}体"
                + (tesla.Branches > 1 ? $"×{tesla.Branches}" : "")
                + $"・減衰{tesla.ChainFalloff:P0}（射程で増加）",
            _ => null,
        };
        if (special is not null)
        {
            yield return special;
        }
    }

    private void OnUpgradeClicked(int row)
    {
        if (_tower is not null && row < _tower.UpgradeStats.Count)
        {
            UpgradeRequested?.Invoke(_tower.UpgradeStats[row]);
        }
    }

    private void OnTargetingChanged(object? sender, EventArgs e)
    {
        if (!_updating && _targetingBox.SelectedItem is ITargetingStrategy strategy)
        {
            TargetingSelected?.Invoke(strategy);
        }
    }
}
