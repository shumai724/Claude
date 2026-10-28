using System.ComponentModel;
using System.Diagnostics;
using TowerDefenceGame.Models.Data;
using TowerDefenceGame.Models.Progress;

namespace TowerDefenceGame.Views.Sharing;

/// <summary>
/// クリア結果を X（旧 Twitter）に共有する。
/// X の Web Intent（投稿画面を文面入りで開く URL）をブラウザで開くだけなので、
/// API キーやログイン連携は要らない。投稿するかどうかは、開いた画面でプレイヤーが決める。
/// </summary>
public static class XShare
{
    /// <summary>投稿に付けるハッシュタグ（# は付けない）。ゲームの名前が決まったら変える。</summary>
    public const string Hashtag = "TowerDefence";

    private const string IntentUrl = "https://x.com/intent/post";

    /// <summary>投稿文を作る。開いた画面で編集もできる。</summary>
    public static string ComposeText(ClearReport report)
    {
        string level = report.Level > DifficultyRules.NormalLevel ? $" Lv{report.Level}" : "";
        string badge = report.IsFirstClear ? "（初クリア！）" : report.IsNewBest ? "（自己ベスト更新！）" : "";
        string lives = report.Lives >= report.InitialLives
            ? $"ノーダメージ（ライフ {report.Lives}/{report.InitialLives}）"
            : $"残りライフ {report.Lives}/{report.InitialLives}";

        var lines = new List<string>
        {
            $"タワーディフェンス「{report.StageName}」{level}をクリア！🏰",
            $"スコア {report.Score:N0}{badge}",
            $"{lives}・全{report.TotalWaves}ウェーブ",
        };

        lines.Add($"#{Hashtag}");
        return string.Join("\n", lines);
    }

    /// <summary>投稿文を入れた X の投稿画面の URL。</summary>
    public static string BuildUrl(string text) => $"{IntentUrl}?text={Uri.EscapeDataString(text)}";

    /// <summary>既定のブラウザで X の投稿画面を開く。開けなかったら false。</summary>
    public static bool TryOpen(string text)
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo(BuildUrl(text)) { UseShellExecute = true });
            return true;
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException or PlatformNotSupportedException)
        {
            return false;
        }
    }
}
