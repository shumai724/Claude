namespace TowerDefenceGame.Developer;

/// <summary>
/// 開発者版の起動口。製品版と同じゲームを、開発者ウィンドウ（F12 で開閉）付きで起動する。
/// クリア記録は製品版とは別のファイルに保存するので、テストのプレイで製品版の記録は変わらない。
/// </summary>
internal static class Program
{
    [STAThread]
    private static void Main()
    {
        var options = new GameLaunchOptions { ProgressPath = DeveloperPaths.Progress };
        GameLauncher.Run(options, (form, app) => new DeveloperController(form, app, new DeveloperSettingsStore(DeveloperPaths.Settings)));
    }
}

/// <summary>開発者版のファイルの保存先（製品版と同じフォルダの、別名のファイル）。</summary>
internal static class DeveloperPaths
{
    private static readonly string Folder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TowerDefenceGame");

    /// <summary>開発者版でのクリア記録。</summary>
    public static string Progress => Path.Combine(Folder, "progress.developer.json");

    /// <summary>開発者ウィンドウの設定（敵の数の倍率など）。</summary>
    public static string Settings => Path.Combine(Folder, "developer.json");
}
