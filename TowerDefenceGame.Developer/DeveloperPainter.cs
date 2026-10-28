using System.Numerics;
using TowerDefenceGame.Models.Core;
using TowerDefenceGame.Models.Entities;
using TowerDefenceGame.Views.Rendering;

namespace TowerDefenceGame.Developer;

/// <summary>開発者モードの情報をマップに重ねて描く。各攻撃タワーの上に与えたダメージの合計を出す。</summary>
internal sealed class DeveloperPainter
{
    private static readonly Rgba LabelBack = new(0, 0, 0, 170);
    private static readonly Rgba TagBack = new(120, 40, 160, 220);

    private const float LabelFontSize = 9f;
    private const float LabelHeight = 14f;
    private const float TagFontSize = 10f;

    public void Draw(IRenderer renderer, Game game, CombatTracker tracker)
    {
        float tileSize = game.Map.TileSize;
        foreach (var tower in game.World.Towers)
        {
            if (tower is AttackTower)
            {
                DrawDamageLabel(renderer, tower, tracker.For(tower).TotalDamage, tileSize);
            }
        }

        DrawTag(renderer, game);
    }

    /// <summary>ダメージを短く表す（12345 → 12.3k）。</summary>
    public static string FormatShort(long value) => value switch
    {
        >= 1_000_000 => $"{value / 1_000_000.0:0.0}M",
        >= 10_000 => $"{value / 1000.0:0.0}k",
        _ => value.ToString(),
    };

    private static void DrawDamageLabel(IRenderer renderer, Tower tower, long damage, float tileSize)
    {
        string text = FormatShort(damage);
        float width = 8f + EstimateWidth(text, LabelFontSize);
        // タワーの上端に重ねる。最上段のマスでも画面外に出ないよう、上端より上には出さない
        var center = new Vector2(tower.Position.X, MathF.Max(LabelHeight / 2f, tower.Position.Y - tileSize * 0.5f));
        renderer.FillRect(center.X - width / 2f, center.Y - LabelHeight / 2f, width, LabelHeight, LabelBack);
        renderer.DrawText(text, center, Palette.White, LabelFontSize, TextAnchor.Center, bold: true);
    }

    /// <summary>左上に「開発者モード中」であることと、敵の数の倍率を出す。</summary>
    private static void DrawTag(IRenderer renderer, Game game)
    {
        var waves = game.WaveManager;
        string text = $"DEV  敵の数 ×{waves.EnemyCountMultiplier:0.0}";
        float width = 14f + EstimateWidth(text, TagFontSize);
        renderer.FillRect(4, 4, width, 20, TagBack);
        renderer.DrawText(text, new Vector2(10, 14), Palette.White, TagFontSize, TextAnchor.MiddleLeft, bold: true);
    }

    /// <summary>文字列のおおよその表示幅（全角は半角の約2倍として見積もる）。背景の大きさを決めるのに使う。</summary>
    private static float EstimateWidth(string text, float fontSize)
    {
        float width = 0f;
        foreach (char c in text)
        {
            width += c > 0x7F ? fontSize * 1.34f : fontSize * 0.7f;
        }

        return width;
    }
}
