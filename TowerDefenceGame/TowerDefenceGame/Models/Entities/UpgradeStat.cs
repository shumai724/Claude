namespace TowerDefenceGame.Models.Entities;

/// <summary>
/// 建物の強化項目。項目ごとに Lv1〜<see cref="Tower.MaxLevel"/> まで個別に強化する。
/// どの項目を持つかは建物の種類で決まる。
/// </summary>
public enum UpgradeStat
{
    // 攻撃タワー
    Damage,
    Range,
    FireRate,

    // 金鉱
    GoldAmount,
    GoldSpeed,

    // 司令塔
    BuffPower,
    BuffRange,
}
