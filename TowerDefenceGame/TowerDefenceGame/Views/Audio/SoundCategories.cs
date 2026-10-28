using TowerDefenceGame.Models.Settings;

namespace TowerDefenceGame.Views.Audio;

/// <summary>効果音と分類の対応、および設定画面での表示順・表示名と試聴音。</summary>
public static class SoundCategories
{
    /// <summary>設定画面に並べる順。BGM を先頭に置く。</summary>
    public static IReadOnlyList<SoundCategory> All { get; } =
    [
        SoundCategory.Music,
        SoundCategory.Interface,
        SoundCategory.Building,
        SoundCategory.Hit,
        SoundCategory.Enemy,
        SoundCategory.Gold,
        SoundCategory.Progress,
    ];

    public static SoundCategory Of(SoundEffect effect) => effect switch
    {
        SoundEffect.Select or SoundEffect.Cancel or SoundEffect.Rejected => SoundCategory.Interface,
        SoundEffect.TowerPlaced or SoundEffect.TowerUpgraded or SoundEffect.TowerMastered
            or SoundEffect.TowerSold => SoundCategory.Building,
        SoundEffect.ArrowHit or SoundEffect.CannonHit or SoundEffect.FrostHit
            or SoundEffect.BreakerHit or SoundEffect.LightningHit => SoundCategory.Hit,
        SoundEffect.EnemyKilled or SoundEffect.EnemyReachedGoal => SoundCategory.Enemy,
        SoundEffect.GoldGenerated => SoundCategory.Gold,
        _ => SoundCategory.Progress,
    };

    public static string DisplayName(SoundCategory category) => category switch
    {
        SoundCategory.Interface => "操作音",
        SoundCategory.Building => "建設・強化・売却",
        SoundCategory.Hit => "攻撃の命中",
        SoundCategory.Enemy => "撃破・被ダメージ",
        SoundCategory.Gold => "金鉱の産出",
        SoundCategory.Progress => "ウェーブ・勝敗",
        SoundCategory.Music => "BGM",
        _ => category.ToString(),
    };

    /// <summary>設定画面でスライダーを動かしたときに鳴らす代表的な音。BGM は流れている曲で確かめるので null。</summary>
    public static SoundEffect? Sample(SoundCategory category) => category switch
    {
        SoundCategory.Music => null,
        SoundCategory.Interface => SoundEffect.Select,
        SoundCategory.Building => SoundEffect.TowerPlaced,
        SoundCategory.Hit => SoundEffect.CannonHit,
        SoundCategory.Enemy => SoundEffect.EnemyKilled,
        SoundCategory.Gold => SoundEffect.GoldGenerated,
        _ => SoundEffect.WaveStarted,
    };
}
