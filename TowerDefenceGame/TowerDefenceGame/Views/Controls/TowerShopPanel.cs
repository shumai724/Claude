using TowerDefenceGame.Models.Data;
using TowerDefenceGame.Models.Entities;

namespace TowerDefenceGame.Views.Controls;

/// <summary>建設できるタワーの一覧。ボタンで建設するタワーを選ぶ。</summary>
public sealed class TowerShopPanel : UserControl
{
    private static readonly Color SelectedColor = Color.FromArgb(255, 230, 140);

    private readonly FlowLayoutPanel _list;
    private readonly ToolTip _toolTip = new();
    private readonly Dictionary<TowerType, (Button Button, TowerSpec Spec)> _items = [];

    public TowerShopPanel()
    {
        var header = new Label
        {
            Text = "タワー建設",
            Dock = DockStyle.Top,
            Height = 24,
            Font = new Font(Font, FontStyle.Bold),
        };
        // 種類が増えても縦に伸びすぎないよう、2列で並べる
        _list = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Margin = Padding.Empty,
        };

        Controls.Add(_list);
        Controls.Add(header);
    }

    public event Action<TowerType>? TowerTypeSelected;

    public void SetTowers(IEnumerable<TowerSpec> specs)
    {
        foreach (var (button, _) in _items.Values)
        {
            _list.Controls.Remove(button);
            button.Dispose();
        }

        _items.Clear();

        int index = 1;
        foreach (var spec in specs)
        {
            var type = spec.Type;
            var button = new Button
            {
                Text = $"{index} {spec.Name}\n{spec.Cost}G",
                TextAlign = ContentAlignment.MiddleLeft,
                Width = (Width - 12) / 2,
                Height = 38,
                FlatStyle = FlatStyle.Flat,
                TabStop = false,
                UseVisualStyleBackColor = false,
            };
            button.Click += (_, _) => TowerTypeSelected?.Invoke(type);
            _toolTip.SetToolTip(button, spec.Description);
            _list.Controls.Add(button);
            _items[type] = (button, spec);
            index++;
        }
    }

    public void UpdateState(int gold, TowerType? selected, bool enabled)
    {
        foreach (var (type, (button, spec)) in _items)
        {
            button.Enabled = enabled && gold >= spec.Cost;
            button.BackColor = selected == type ? SelectedColor : SystemColors.Control;
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
}
