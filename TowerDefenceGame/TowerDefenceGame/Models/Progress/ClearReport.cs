namespace TowerDefenceGame.Models.Progress;

/// <summary>ステージをクリアしたときの結果。X での共有などに使う。</summary>
/// <param name="IsFirstClear">そのレベルを初めてクリアしたか。</param>
/// <param name="IsNewBest">そのレベルのベストスコアを更新したか（初クリアも含む）。</param>
public sealed record ClearReport(
    string StageName,
    int Level,
    int Score,
    int Lives,
    int InitialLives,
    int TotalWaves,
    bool IsFirstClear,
    bool IsNewBest);
