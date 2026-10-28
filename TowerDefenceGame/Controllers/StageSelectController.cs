using System.Text.Json;
using TowerDefenceGame.Models.Data;
using TowerDefenceGame.Models.Progress;
using TowerDefenceGame.Views.Audio;
using TowerDefenceGame.Views.Controls;

namespace TowerDefenceGame.Controllers;

/// <summary>
/// ステージ選択画面を担当する。ステージ一覧・クリア記録・各難易度レベルの補正を View に渡し、
/// 選ばれたステージとレベルを通知する。
/// </summary>
public sealed class StageSelectController
{
    private static readonly IReadOnlyList<DifficultyRules> NormalOnly = [DifficultyRules.Normal];

    private readonly StageSelectView _view;
    private readonly string _stagesDirectory;
    private readonly string _difficultyPath;
    private readonly ProgressStore _progress;
    private readonly ISoundPlayer _sound;
    private readonly Dictionary<string, IReadOnlyList<DifficultyRules>> _levels = [];

    public StageSelectController(StageSelectView view, string stagesDirectory, string difficultyPath, ProgressStore progress, ISoundPlayer sound)
    {
        _view = view;
        _stagesDirectory = stagesDirectory;
        _difficultyPath = difficultyPath;
        _progress = progress;
        _sound = sound;
        _view.StageSelected += OnStageSelected;
    }

    public event Action<StageSummary, DifficultyRules>? StageChosen;

    /// <summary>ステージフォルダと難易度設定を読み直して一覧を更新する。遊べるステージが1つもなければ false。</summary>
    public bool Refresh()
    {
        var catalog = StageCatalog.Load(_stagesDirectory);
        var errors = catalog.Errors.ToList();
        ResolveLevels(catalog.Stages, errors);
        _view.SetStages(catalog.Stages, _progress, stage => _levels.GetValueOrDefault(stage.Id) ?? NormalOnly, errors);
        return catalog.Stages.Count > 0;
    }

    /// <summary>クリアを記録する。初クリアだったか・ベストを更新したかを返す。</summary>
    public (bool IsFirstClear, bool IsNewBest) RecordClear(StageSummary stage, int level, int score, int lives)
    {
        bool isFirstClear = _progress.Get(stage.Id)?.Get(level) is null;
        bool isNewBest = _progress.RecordClear(stage.Id, level, score, lives);
        return (isFirstClear, isNewBest);
    }

    /// <summary>
    /// 共通の各レベルの補正にステージごとの上書きを重ねて、各ステージで遊べるレベルを決める。
    /// 設定が壊れていても Lv1 は遊べるよう、問題のあるステージは Lv1 だけにする。
    /// </summary>
    private void ResolveLevels(IReadOnlyList<StageSummary> stages, List<string> errors)
    {
        _levels.Clear();
        IReadOnlyList<DifficultyRules> common;
        try
        {
            common = DifficultyRules.LoadLevels(_difficultyPath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or InvalidDataException)
        {
            errors.Add($"{Path.GetFileName(_difficultyPath)}: {ex.Message}（Lv2 以上は選べません）");
            return;
        }

        foreach (var stage in stages)
        {
            try
            {
                _levels[stage.Id] = stage.Data.ResolveLevels(common);
            }
            catch (InvalidDataException ex)
            {
                errors.Add($"{Path.GetFileName(stage.FilePath)} の levels: {ex.Message}（このステージの Lv2 以上は選べません）");
            }
        }
    }

    private void OnStageSelected(StageSummary stage, int level)
    {
        var rules = _levels.GetValueOrDefault(stage.Id)?.ElementAtOrDefault(level - 1)
            ?? (level == DifficultyRules.NormalLevel ? DifficultyRules.Normal : null);
        if (rules is null || !_progress.IsUnlocked(stage.Id, level))
        {
            _sound.Play(SoundEffect.Rejected);
            return;
        }

        _sound.Play(SoundEffect.Select);
        StageChosen?.Invoke(stage, rules);
    }
}
