using System.Numerics;
using TowerDefenceGame.Models.Core;
using TowerDefenceGame.Models.Entities;
using TowerDefenceGame.Models.Map;

namespace TowerDefenceGame.Developer.Bot;

/// <summary>
/// bot が守りの強さを見積もるための、タワー1基分の性能の写し。
/// 実際のタワー（または <see cref="Game.CreatePreview"/> で作った仮のタワー）から作るので、
/// 強化の伸び方などはゲーム本体の計算をそのまま使う。司令塔の強化は含まない（見積もり側で掛ける）。
/// </summary>
internal sealed record BotTower
{
    public required TowerType Type { get; init; }

    public required GridPos Cell { get; init; }

    public required Vector2 Position { get; init; }

    /// <summary>このタワーにかけた金額（建設費＋強化費）。司令塔の置き場所の評価に使う。</summary>
    public int Value { get; init; }

    public float Range { get; init; }

    // ---- 攻撃タワー ----
    public int Damage { get; init; }

    public float Interval { get; init; } = 1f;

    /// <summary>1回の攻撃で追加で狙える敵の数（アーチャー「三本矢」など）。</summary>
    public int ExtraTargets { get; init; }

    /// <summary>Lv5 効果などをまとめた火力の倍率（連装砲で約1.7倍など）。</summary>
    public float DamageFactor { get; init; } = 1f;

    public bool PierceArmor { get; init; }

    public float Splash { get; init; }

    public int ChainCount { get; init; }

    public int Branches { get; init; } = 1;

    public float ChainRange { get; init; }

    public float Falloff { get; init; } = 1f;

    public float SlowRate { get; init; }

    /// <summary>減速させた敵の被ダメージ増加率（フロスト「凍傷」）。</summary>
    public float DamageTakenBonus { get; init; }

    public int ArmorReduction { get; init; }

    public float AcidDps { get; init; }

    // ---- 司令塔 ----
    public float BuffDamage { get; init; }

    public float BuffFireRate { get; init; }

    public float BuffRange { get; init; }

    public int BuffExtraTargets { get; init; }

    public bool IsAttack => Type is not (TowerType.GoldMine or TowerType.Support);

    public bool IsSupport => Type == TowerType.Support;

    /// <param name="tower">司令塔の強化を受けていないタワー（仮のタワー）を渡す。</param>
    public static BotTower From(Tower tower, int value)
    {
        var basic = new BotTower
        {
            Type = tower.Spec.Type,
            Cell = tower.Cell,
            Position = tower.Position,
            Value = value,
            Range = tower.Range,
        };

        return tower switch
        {
            ArcherTower a => Attack(basic, a) with
            {
                PierceArmor = a.IsMastered(UpgradeStat.Damage),
                // 「速射」は同じ敵を撃ち続けると最大2倍。平均してこのくらいと見る
                DamageFactor = a.IsMastered(UpgradeStat.FireRate) ? 1.5f : 1f,
            },
            CannonTower c => Attack(basic, c) with
            {
                Splash = c.SplashRadius,
                DamageFactor = (c.IsMastered(UpgradeStat.FireRate) ? 1f + CannonTower.FollowUpDamageRate : 1f)
                    * (c.IsMastered(UpgradeStat.Damage) ? 1.1f : 1f)
                    * (c.IsMastered(UpgradeStat.Range) ? 1.3f : 1f),
            },
            FrostTower f => Attack(basic, f) with
            {
                SlowRate = f.SlowRate,
                DamageTakenBonus = f.IsMastered(UpgradeStat.FireRate) ? FrostTower.FrostbiteDamageBonus : 0f,
            },
            BreakerTower b => Attack(basic, b) with
            {
                ArmorReduction = b.MaxArmorReduction,
                AcidDps = b.AcidDamagePerSecond,
            },
            TeslaTower t => Attack(basic, t) with
            {
                ChainCount = t.ChainCount,
                Branches = t.Branches,
                ChainRange = t.ChainRange,
                Falloff = t.ChainFalloff,
                DamageFactor = t.IsMastered(UpgradeStat.FireRate) ? 1.1f : 1f,
            },
            SupportTower s => basic with
            {
                BuffDamage = s.DamageBonus,
                BuffFireRate = s.FireRateBonus,
                BuffRange = s.RangeBonus,
                BuffExtraTargets = s.ExtraTargets,
            },
            _ => basic,
        };
    }

    private static BotTower Attack(BotTower basic, AttackTower tower) => basic with
    {
        Damage = tower.Damage,
        Interval = tower.FireInterval,
        ExtraTargets = tower.TargetCount - 1,
    };
}
