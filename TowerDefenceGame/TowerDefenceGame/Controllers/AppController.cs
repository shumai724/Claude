using TowerDefenceGame.Models.Core;
using TowerDefenceGame.Models.Data;
using TowerDefenceGame.Models.Progress;
using TowerDefenceGame.Models.Settings;
using TowerDefenceGame.Views.Audio;
using TowerDefenceGame.Views.Forms;
using TowerDefenceGame.Views.Sharing;

namespace TowerDefenceGame.Controllers;

/// <summary>アプリ全体の画面遷移（ステージ選択 ⇔ ゲーム）と、どの画面からも開ける設定画面を管理する。</summary>
public sealed class AppController : IDisposable
{
    private readonly MainForm _view;
    private readonly StageSelectController _stageSelect;
    private readonly GameController _gameController;
    private readonly SettingsController _settings;
    private readonly ISoundPlayer _sound;
    private readonly ProgressStore _progress;

    /// <summary>直近にクリアした結果。X で共有するときに使う。</summary>
    private ClearReport? _lastClear;

    public AppController(MainForm view, string assetsDirectory, ISoundPlayer sound, ProgressStore progress, SettingsStore settingsStore)
    {
        _view = view;
        _sound = sound;
        _progress = progress;
        _settings = new SettingsController(sound, settingsStore);
        _stageSelect = new StageSelectController(
            view.StageSelectView,
            Path.Combine(assetsDirectory, "Stages"),
            Path.Combine(assetsDirectory, "difficulty.json"),
            progress,
            sound);
        _gameController = new GameController(view, assetsDirectory, sound);

        _stageSelect.StageChosen += OnStageChosen;
        _gameController.ExitRequested += ShowStageSelect;
        _gameController.StageCleared += OnStageCleared;

        _view.SettingsRequested += OpenSettings;
        _view.StageSelectView.SettingsRequested += OpenSettings;
        _view.SoundToggleRequested += _settings.ToggleSound;
        _view.ShareRequested += ShareLastClear;
        _settings.Changed += ShowSoundStatus;
    }

    /// <summary>ステージ選択画面を表示する。遊べるステージが1つもなければ false。</summary>
    public bool Start()
    {
        ShowSoundStatus();
        if (!_stageSelect.Refresh())
        {
            _view.ShowError("遊べるステージが見つかりませんでした。Assets/Stages フォルダを確認してください。");
            return false;
        }

        _view.ShowStageSelect();
        _sound.PlayMusic(MusicTrack.StageSelect);
        return true;
    }

    /// <summary>ゲーム画面の Controller。起動時に機能を足したい場合（開発者版など）に使う。</summary>
    public GameController GameController => _gameController;

    /// <summary>クリア記録。起動時に機能を足したい場合（開発者版など）に使う。</summary>
    public ProgressStore Progress => _progress;

    /// <summary>ステージ選択画面の一覧（解放状況など）を作り直す。</summary>
    public void RefreshStageSelect() => _stageSelect.Refresh();

    public void Dispose() => _gameController.Dispose();

    private void OnStageChosen(StageSummary stage, DifficultyRules rules)
    {
        _lastClear = null;
        if (_gameController.StartStage(stage, rules))
        {
            _view.ShowGame();
        }
    }

    private void ShowStageSelect()
    {
        _gameController.Stop();
        _stageSelect.Refresh();
        _view.ShowStageSelect();
        _sound.PlayMusic(MusicTrack.StageSelect);
    }

    private void OnStageCleared(StageSummary stage, Game game)
    {
        var (isFirstClear, isNewBest) = _stageSelect.RecordClear(stage, game.Level, game.Player.Score, game.Player.Lives);

        _lastClear = new ClearReport(
            game.StageName,
            game.Level,
            game.Player.Score,
            game.Player.Lives,
            game.Player.InitialLives,
            game.WaveManager.TotalWaves,
            isFirstClear,
            isNewBest);
    }

    /// <summary>直近のクリア結果を、X の投稿画面（ブラウザ）で開く。開けなければ投稿文をクリップボードに入れる。</summary>
    private void ShareLastClear()
    {
        if (_lastClear is null)
        {
            return;
        }

        string text = XShare.ComposeText(_lastClear);
        if (XShare.TryOpen(text))
        {
            return;
        }

        try
        {
            Clipboard.SetText(text);
            _view.ShowError("ブラウザを開けなかったため、投稿文をクリップボードにコピーしました。\nX の投稿画面に貼り付けてください。");
        }
        catch (System.Runtime.InteropServices.ExternalException)
        {
            _view.ShowError($"ブラウザを開けませんでした。次の文面を X に投稿してください。\n\n{text}");
        }
    }

    /// <summary>設定画面を開いている間はゲームを止めておく。</summary>
    private void OpenSettings()
    {
        bool wasPlaying = _gameController.PauseIfPlaying();
        _settings.ShowDialog(_view);
        _gameController.ResumeIf(wasPlaying);
    }

    private void ShowSoundStatus() => _view.ShowSoundEnabled(_settings.SoundEnabled);
}
