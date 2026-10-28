using TowerDefenceGame.Models.Core;
using TowerDefenceGame.Models.Entities;
using TowerDefenceGame.Models.Map;
using TowerDefenceGame.Models.Targeting;

namespace TowerDefenceGame.Controllers;

/// <summary>
/// プレイヤーの選択状態（建設するタワー・選択中のタワー・カーソル位置）を保持し、
/// View からの操作を Game への命令に変換する。
/// </summary>
public sealed class InputController
{
    private Game? _game;

    public TowerType? SelectedTowerType { get; private set; }

    public Tower? SelectedTower { get; private set; }

    public GridPos? HoverCell { get; private set; }

    /// <summary>選択状態や、選択中タワーの内容が変わったときに発生する。</summary>
    public event Action? SelectionChanged;

    /// <summary>操作の結果を通知する。成功した建設・強化・売却は Game 側のイベントで通知される。</summary>
    public event Action<InputFeedback>? Feedback;

    public void Attach(Game game)
    {
        _game = game;
        SelectedTowerType = null;
        SelectedTower = null;
        HoverCell = null;
        SelectionChanged?.Invoke();
    }

    public void SelectTowerType(TowerType type)
    {
        if (_game is null || _game.IsOver)
        {
            return;
        }

        if (SelectedTowerType == type)
        {
            SelectedTowerType = null;
            SelectionChanged?.Invoke();
            Feedback?.Invoke(InputFeedback.Cancelled);
            return;
        }

        if (!_game.Player.CanAfford(_game.GetTowerSpec(type).Cost))
        {
            Feedback?.Invoke(InputFeedback.Rejected);
            return;
        }

        SelectedTowerType = type;
        SelectedTower = null;
        SelectionChanged?.Invoke();
        Feedback?.Invoke(InputFeedback.Selected);
    }

    public void HandleCellClick(GridPos cell)
    {
        if (_game is null)
        {
            return;
        }

        if (SelectedTowerType is { } type)
        {
            if (_game.TryPlaceTower(type, cell))
            {
                SelectedTowerType = null;
                SelectionChanged?.Invoke();
                return;
            }

            // 建設できないマスに既存タワーがあれば、そのタワーの選択に切り替える
            if (_game.GetTowerAt(cell) is null)
            {
                Feedback?.Invoke(InputFeedback.Rejected);
                return;
            }

            SelectedTowerType = null;
        }

        var previous = SelectedTower;
        SelectedTower = _game.GetTowerAt(cell);
        SelectionChanged?.Invoke();

        if (SelectedTower is not null)
        {
            Feedback?.Invoke(InputFeedback.Selected);
        }
        else if (previous is not null)
        {
            Feedback?.Invoke(InputFeedback.Cancelled);
        }
    }

    public void SetHoverCell(GridPos? cell) => HoverCell = cell;

    public void CancelSelection()
    {
        if (SelectedTowerType is null && SelectedTower is null)
        {
            return;
        }

        SelectedTowerType = null;
        SelectedTower = null;
        SelectionChanged?.Invoke();
        Feedback?.Invoke(InputFeedback.Cancelled);
    }

    public void UpgradeSelectedTower(UpgradeStat stat)
    {
        if (_game is null || SelectedTower is null)
        {
            return;
        }

        if (_game.TryUpgradeTower(SelectedTower, stat))
        {
            SelectionChanged?.Invoke();
        }
        else
        {
            Feedback?.Invoke(InputFeedback.Rejected);
        }
    }

    public void SellSelectedTower()
    {
        if (_game is not null && SelectedTower is not null && _game.SellTower(SelectedTower))
        {
            SelectedTower = null;
            SelectionChanged?.Invoke();
        }
    }

    public void SetTargeting(ITargetingStrategy strategy)
    {
        if (SelectedTower is not AttackTower attackTower)
        {
            return;
        }

        attackTower.Targeting = strategy;
        SelectionChanged?.Invoke();
        Feedback?.Invoke(InputFeedback.Selected);
    }
}
