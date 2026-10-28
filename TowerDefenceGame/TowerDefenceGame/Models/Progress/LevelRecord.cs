namespace TowerDefenceGame.Models.Progress;

/// <summary>1つの難易度レベルのクリア記録。</summary>
public sealed record LevelRecord
{
    public int BestScore { get; init; }

    /// <summary>クリア時に残っていたライフの最大値。</summary>
    public int BestLives { get; init; }
}
