using System.Numerics;

namespace TowerDefenceGame.Views.Rendering;

/// <summary>
/// 描画処理の抽象。Painter はこのインターフェースだけを使うので、
/// WPF などへ移行する場合は実装クラスを差し替えるだけで済む。
/// </summary>
public interface IRenderer
{
    void Clear(Rgba color);

    void FillRect(float x, float y, float width, float height, Rgba color);

    void DrawRect(float x, float y, float width, float height, Rgba color, float thickness = 1f);

    void FillCircle(Vector2 center, float radius, Rgba color);

    void DrawCircle(Vector2 center, float radius, Rgba color, float thickness = 1f);

    void DrawLine(Vector2 from, Vector2 to, Rgba color, float thickness = 1f);

    void DrawText(string text, Vector2 position, Rgba color, float size, TextAnchor anchor = TextAnchor.TopLeft, bool bold = false);
}
