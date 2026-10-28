using System.Numerics;
using TowerDefenceGame.Controllers;
using TowerDefenceGame.Models.Core;
using TowerDefenceGame.Models.Map;
using TowerDefenceGame.Views.Painters;
using TowerDefenceGame.Views.Rendering;

namespace TowerDefenceGame.Views.Controls;

/// <summary>マップとエンティティを描画し、マウス操作をマス単位のイベントとして通知する。</summary>
public sealed class GameCanvas : Control
{
    public const int StatusBarHeight = 36;

    private readonly GdiRenderer _renderer = new();
    private readonly MapPainter _mapPainter = new();
    private readonly EntityPainter _entityPainter = new();
    private readonly SelectionPainter _selectionPainter = new();
    private readonly HudPainter _hudPainter = new();
    private Game? _game;
    private InputController? _input;

    public GameCanvas()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint
            | ControlStyles.OptimizedDoubleBuffer
            | ControlStyles.UserPaint
            | ControlStyles.ResizeRedraw,
            true);
        DoubleBuffered = true;
        BackColor = Color.Black;
    }

    /// <summary>
    /// タワー・敵を描いた後、ステータスバーより前に呼ばれる。マップの上に情報を重ねて描きたい場合に使う（開発者版など）。
    /// </summary>
    public event Action<IRenderer, Game>? OverlayPainting;

    public event Action<GridPos>? CellClicked;

    public event Action<GridPos?>? CellHovered;

    public event Action? CancelRequested;

    public void Bind(Game game, InputController input)
    {
        _game = game;
        _input = input;
        Size = new Size((int)MathF.Ceiling(game.Map.Width), (int)MathF.Ceiling(game.Map.Height) + StatusBarHeight);
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        if (_game is null || _input is null)
        {
            e.Graphics.Clear(BackColor);
            return;
        }

        var map = _game.Map;
        _renderer.Begin(e.Graphics);
        _renderer.Clear(Palette.Background);
        _mapPainter.Draw(_renderer, map);
        _selectionPainter.Draw(_renderer, _game, _input);
        _entityPainter.Draw(_renderer, _game.World, map.TileSize);
        OverlayPainting?.Invoke(_renderer, _game);

        // サイドパネルに合わせてキャンバスが縦に伸びた場合も、マップより下はすべてステータスバーにする
        _hudPainter.DrawStatusBar(_renderer, _game, map.Height, Width, Math.Max(StatusBarHeight, Height - map.Height));
        _hudPainter.DrawOverlay(_renderer, _game, map.Width, map.Height);
    }

    protected override void OnMouseClick(MouseEventArgs e)
    {
        base.OnMouseClick(e);
        if (e.Button == MouseButtons.Right)
        {
            CancelRequested?.Invoke();
        }
        else if (e.Button == MouseButtons.Left && TryGetCell(e.Location, out var cell))
        {
            CellClicked?.Invoke(cell);
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        CellHovered?.Invoke(TryGetCell(e.Location, out var cell) ? cell : null);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        CellHovered?.Invoke(null);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _renderer.Dispose();
        }

        base.Dispose(disposing);
    }

    private bool TryGetCell(Point point, out GridPos cell)
    {
        cell = default;
        if (_game is null)
        {
            return false;
        }

        cell = _game.Map.WorldToCell(new Vector2(point.X, point.Y));
        return _game.Map.Contains(cell);
    }
}
