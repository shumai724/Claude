namespace TowerDefenceGame.Views.Audio;

/// <summary>ゲーム内で鳴らす効果音の種類。</summary>
public enum SoundEffect
{
    // UI 操作
    Select,
    Cancel,
    Rejected,

    // タワー
    TowerPlaced,
    TowerUpgraded,

    /// <summary>項目を Lv5 にして追加効果が付いたとき。</summary>
    TowerMastered,
    TowerSold,

    // 命中・敵
    ArrowHit,
    CannonHit,
    FrostHit,
    BreakerHit,
    LightningHit,
    EnemyKilled,
    GoldGenerated,
    EnemyReachedGoal,

    // 進行
    WaveStarted,
    WaveCompleted,
    GameOver,
    StageClear,
}
