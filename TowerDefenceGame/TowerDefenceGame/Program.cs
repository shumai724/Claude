namespace TowerDefenceGame;

/// <summary>製品版の起動口。開発者向けの機能は別プロジェクト（TowerDefenceGame.Developer）にある。</summary>
internal static class Program
{
    [STAThread]
    private static void Main() => GameLauncher.Run(new GameLaunchOptions());
}
