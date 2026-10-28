using TowerDefenceGame.Controllers;
using TowerDefenceGame.Models.Progress;
using TowerDefenceGame.Models.Settings;
using TowerDefenceGame.Views.Audio;
using TowerDefenceGame.Views.Forms;

namespace TowerDefenceGame;

/// <summary>起動時の設定。記録や設定の保存先を変えたい場合（開発者版など）に使う。</summary>
public sealed record GameLaunchOptions
{
    /// <summary>クリア記録の保存先。</summary>
    public string ProgressPath { get; init; } = ProgressStore.DefaultPath;

    /// <summary>システム設定（音量など）の保存先。</summary>
    public string SettingsPath { get; init; } = SettingsStore.DefaultPath;
}

/// <summary>
/// ゲームを組み立てて起動する。製品版の <see cref="Program"/> から呼ぶ。
/// 別のプロジェクト（開発者版など）から、記録の保存先を変えたり機能を足したりして起動することもできる。
/// </summary>
public static class GameLauncher
{
    /// <param name="extend">
    /// 画面と Controller を作った直後（ウィンドウを表示する前）に呼ぶ。機能を足したい場合に使い、
    /// 戻り値はゲーム終了時に破棄する。製品版では使わない。
    /// </param>
    public static void Run(GameLaunchOptions options, Func<MainForm, AppController, IDisposable?>? extend = null)
    {
        Application.SetHighDpiMode(HighDpiMode.DpiUnaware);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        using ISoundPlayer sound = (ISoundPlayer?)NAudioSoundPlayer.TryCreate() ?? new NullSoundPlayer();
        using var form = new MainForm();
        using var app = new AppController(
            form,
            Path.Combine(AppContext.BaseDirectory, "Assets"),
            sound,
            new ProgressStore(options.ProgressPath),
            new SettingsStore(options.SettingsPath));
        using var extension = extend?.Invoke(form, app);

        form.Load += (_, _) =>
        {
            if (!app.Start())
            {
                form.BeginInvoke(new Action(form.Close));
            }
        };

        Application.Run(form);
    }
}
