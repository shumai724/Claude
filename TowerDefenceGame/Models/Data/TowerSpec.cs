using TowerDefenceGame.Models.Entities;

namespace TowerDefenceGame.Models.Data;

/// <summary>
/// 建物の種類ごとの基本ステータス（Lv1 時点）。
/// 種類によって使う項目が異なり、使わない項目は 0 のままでよい。
/// </summary>
public sealed record TowerSpec
{
    public required TowerType Type { get; init; }

    public required string Name { get; init; }

    public string Description { get; init; } = "";

    public required int Cost { get; init; }

    /// <summary>
    /// 強化費用の基準値。1項目を Lv1→2 は ×0.5、2→3 は ×1、3→4 は ×1.5、4→5 は ×3（<see cref="Tower.GetUpgradeCost"/>）。
    /// </summary>
    public required int UpgradeCost { get; init; }

    /// <summary>攻撃タワーの射程、司令塔の効果範囲。</summary>
    public float Range { get; init; }

    // ---- 攻撃タワー共通 ----
    public int Damage { get; init; }

    /// <summary>攻撃間隔（秒）。</summary>
    public float FireInterval { get; init; }

    public float ProjectileSpeed { get; init; }

    // ---- キャノン ----
    public float SplashRadius { get; init; }

    // ---- フロスト ----
    public float SlowRate { get; init; }

    public float SlowDuration { get; init; }

    // ---- ブレイカー ----
    public int ArmorReduction { get; init; }

    public float ArmorBreakDuration { get; init; }

    // ---- ライトニング ----
    /// <summary>最初の標的の後に飛び移る回数。</summary>
    public int ChainCount { get; init; }

    public float ChainRange { get; init; }

    /// <summary>1回飛び移るごとにダメージに掛かる倍率（0〜1）。</summary>
    public float ChainFalloff { get; init; }

    // ---- 金鉱 ----
    public int GoldAmount { get; init; }

    /// <summary>産出間隔（秒）。</summary>
    public float GoldInterval { get; init; }

    // ---- 司令塔 ----
    /// <summary>攻撃力の増加率（0.25 で +25%）。</summary>
    public float DamageBuff { get; init; }

    /// <summary>攻撃速度の増加率（0.2 で +20%）。</summary>
    public float FireRateBuff { get; init; }

    public bool IsAttackTower => Type is not (TowerType.GoldMine or TowerType.Support);

    public void Validate()
    {
        Require(Cost >= 0 && UpgradeCost >= 0 && Range >= 0 && Damage >= 0, "基本ステータス");

        if (IsAttackTower)
        {
            // ライトニングは即着弾なので弾速は使わない
            bool needsProjectile = Type != TowerType.Tesla;
            Require(Range > 0 && FireInterval > 0 && (!needsProjectile || ProjectileSpeed > 0),
                "range / fireInterval / projectileSpeed");
        }

        switch (Type)
        {
            case TowerType.Cannon:
                Require(SplashRadius > 0, "splashRadius");
                break;
            case TowerType.Frost:
                Require(SlowRate is > 0 and < 1 && SlowDuration > 0, "slowRate / slowDuration");
                break;
            case TowerType.Breaker:
                Require(ArmorReduction > 0 && ArmorBreakDuration > 0, "armorReduction / armorBreakDuration");
                break;
            case TowerType.Tesla:
                Require(ChainCount >= 0 && ChainRange > 0 && ChainFalloff is > 0 and <= 1, "chainCount / chainRange / chainFalloff");
                break;
            case TowerType.GoldMine:
                Require(GoldAmount > 0 && GoldInterval > 0, "goldAmount / goldInterval");
                break;
            case TowerType.Support:
                Require(Range > 0 && DamageBuff >= 0 && FireRateBuff >= 0 && DamageBuff + FireRateBuff > 0,
                    "range / damageBuff / fireRateBuff");
                break;
        }
    }

    private void Require(bool condition, string fields)
    {
        if (!condition)
        {
            throw new InvalidDataException($"タワー {Type} の {fields} の値が不正です。");
        }
    }
}
