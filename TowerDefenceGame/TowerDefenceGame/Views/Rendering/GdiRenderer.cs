using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Numerics;

namespace TowerDefenceGame.Views.Rendering;

/// <summary>WinForms（System.Drawing / GDI+）による <see cref="IRenderer"/> 実装。</summary>
public sealed class GdiRenderer : IRenderer, IDisposable
{
    private const string FontFamilyName = "Meiryo UI";

    private readonly Dictionary<Rgba, SolidBrush> _brushes = [];
    private readonly Dictionary<(Rgba Color, float Thickness), Pen> _pens = [];
    private readonly Dictionary<(float Size, bool Bold), Font> _fonts = [];
    private readonly Dictionary<TextAnchor, StringFormat> _formats = [];
    private Graphics? _graphics;

    private Graphics Graphics =>
        _graphics ?? throw new InvalidOperationException("Begin() を呼び出してから描画してください。");

    /// <summary>描画先を設定する。OnPaint の先頭で毎回呼び出す。</summary>
    public void Begin(Graphics graphics)
    {
        _graphics = graphics;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
    }

    public void Clear(Rgba color) => Graphics.Clear(ToColor(color));

    public void FillRect(float x, float y, float width, float height, Rgba color) =>
        Graphics.FillRectangle(GetBrush(color), x, y, width, height);

    public void DrawRect(float x, float y, float width, float height, Rgba color, float thickness = 1f) =>
        Graphics.DrawRectangle(GetPen(color, thickness), x, y, width, height);

    public void FillCircle(Vector2 center, float radius, Rgba color) =>
        Graphics.FillEllipse(GetBrush(color), center.X - radius, center.Y - radius, radius * 2, radius * 2);

    public void DrawCircle(Vector2 center, float radius, Rgba color, float thickness = 1f) =>
        Graphics.DrawEllipse(GetPen(color, thickness), center.X - radius, center.Y - radius, radius * 2, radius * 2);

    public void DrawLine(Vector2 from, Vector2 to, Rgba color, float thickness = 1f) =>
        Graphics.DrawLine(GetPen(color, thickness), from.X, from.Y, to.X, to.Y);

    public void DrawText(string text, Vector2 position, Rgba color, float size, TextAnchor anchor = TextAnchor.TopLeft, bool bold = false) =>
        Graphics.DrawString(text, GetFont(size, bold), GetBrush(color), position.X, position.Y, GetFormat(anchor));

    public void Dispose()
    {
        foreach (var brush in _brushes.Values) brush.Dispose();
        foreach (var pen in _pens.Values) pen.Dispose();
        foreach (var font in _fonts.Values) font.Dispose();
        foreach (var format in _formats.Values) format.Dispose();
        _brushes.Clear();
        _pens.Clear();
        _fonts.Clear();
        _formats.Clear();
    }

    private static Color ToColor(Rgba c) => Color.FromArgb(c.A, c.R, c.G, c.B);

    private SolidBrush GetBrush(Rgba color)
    {
        if (!_brushes.TryGetValue(color, out var brush))
        {
            brush = new SolidBrush(ToColor(color));
            _brushes[color] = brush;
        }

        return brush;
    }

    private Pen GetPen(Rgba color, float thickness)
    {
        if (!_pens.TryGetValue((color, thickness), out var pen))
        {
            pen = new Pen(ToColor(color), thickness)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round,
            };
            _pens[(color, thickness)] = pen;
        }

        return pen;
    }

    private Font GetFont(float size, bool bold)
    {
        if (!_fonts.TryGetValue((size, bold), out var font))
        {
            font = new Font(FontFamilyName, size, bold ? FontStyle.Bold : FontStyle.Regular, GraphicsUnit.Pixel);
            _fonts[(size, bold)] = font;
        }

        return font;
    }

    private StringFormat GetFormat(TextAnchor anchor)
    {
        if (!_formats.TryGetValue(anchor, out var format))
        {
            format = anchor switch
            {
                TextAnchor.Center => new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center },
                TextAnchor.MiddleLeft => new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center },
                TextAnchor.MiddleRight => new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center },
                _ => new StringFormat(),
            };
            _formats[anchor] = format;
        }

        return format;
    }
}
