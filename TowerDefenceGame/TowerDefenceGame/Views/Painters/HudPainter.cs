using System.Numerics;
using TowerDefenceGame.Models.Core;
using TowerDefenceGame.Models.Data;
using TowerDefenceGame.Views.Rendering;

namespace TowerDefenceGame.Views.Painters;

/// <summary>画面下部のステータスバーと、一時停止・ゲーム終了時のメッセージを描画する。</summary>
public sealed class HudPainter
{
    private const float FontSize = 15f;
    private const float Padding = 12f;

    /// <param name="top">ステータスバーの上端のY座標。</param>
    public void DrawStatusBar(IRenderer renderer, Game game, float top, float width, float height)
    {
        renderer.FillRect(0, top, width, height, Palette.HudBack);

        var player = game.Player;
        var waves = game.WaveManager;
        float y = top + height / 2f;
        float x = Padding;

        // Lv2 以上は左端にレベルのバッジを出し、残りの表示を右へずらす（右端の案内文と重ならないように）
        if (game.Level > DifficultyRules.NormalLevel)
        {
            renderer.FillRect(x - 4, y - 11, 52, 22, Palette.LevelBadge);
            renderer.DrawText($"Lv{game.Level}", new Vector2(x + 22, y), Palette.White, FontSize - 2, TextAnchor.Center, bold: true);
            x += 62;
        }

        renderer.DrawText($"所持金 {player.Gold}G", new Vector2(x, y), Palette.HudGold, FontSize, TextAnchor.MiddleLeft, bold: true);
        renderer.DrawText($"ライフ {player.Lives}", new Vector2(x + 150, y), Palette.HudLife, FontSize, TextAnchor.MiddleLeft, bold: true);
        renderer.DrawText($"ウェーブ {waves.CurrentWaveNumber}/{waves.TotalWaves}", new Vector2(x + 260, y), Palette.HudText, FontSize, TextAnchor.MiddleLeft);
        renderer.DrawText($"スコア {player.Score}", new Vector2(x + 390, y), Palette.HudText, FontSize, TextAnchor.MiddleLeft);
        renderer.DrawText(StatusMessage(game), new Vector2(width - Padding, y), Palette.HudSub, FontSize - 2, TextAnchor.MiddleRight);
    }

    /// <summary>マップ全体を覆うメッセージ。表示するものがなければ何もしない。</summary>
    public void DrawOverlay(IRenderer renderer, Game game, float width, float height)
    {
        (string Title, string Sub)? message = game.State switch
        {
            GameState.Paused => ("一時停止中", "P キーまたは「再開」ボタンで再開"),
            GameState.GameOver => ("GAME OVER", $"ウェーブ {game.WaveManager.CurrentWaveNumber} で陥落　「リスタート」で再挑戦 /「ステージ選択」で戻る"),
            GameState.StageClear => ("STAGE CLEAR!", $"スコア {game.Player.Score}　残りライフ {game.Player.Lives}　「X でクリアを共有」で結果をポスト /「ステージ選択」で次へ"),
            _ => null,
        };

        if (message is not { } m)
        {
            return;
        }

        var center = new Vector2(width / 2f, height / 2f);
        renderer.FillRect(0, 0, width, height, Palette.Overlay);
        renderer.DrawText(m.Title, center - new Vector2(0, 22), Palette.White, 44f, TextAnchor.Center, bold: true);
        renderer.DrawText(m.Sub, center + new Vector2(0, 28), Palette.HudText, 16f, TextAnchor.Center);
    }

    private static string StatusMessage(Game game)
    {
        var waves = game.WaveManager;
        if (game.IsOver)
        {
            return "";
        }

        if (waves.IsWaveInProgress)
        {
            return "ウェーブ進行中";
        }

        return waves.NextWave is { } next
            ? $"Space で次のウェーブ（敵 {waves.TotalEnemiesOf(next)} 体）"
            : "";
    }
}
