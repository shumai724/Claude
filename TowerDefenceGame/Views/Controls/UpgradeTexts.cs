using TowerDefenceGame.Models.Entities;

namespace TowerDefenceGame.Views.Controls;

/// <summary>強化項目の表示名と、Lv5 で付く追加効果の説明。追加効果は建物の種類ごとに異なる。</summary>
public static class UpgradeTexts
{
    /// <summary>Lv5 効果の名前・詳細パネル用の短い説明・ツールチップ用の説明。</summary>
    private readonly record struct Mastery(string Name, string Short, string Description);

    public static string StatName(UpgradeStat stat) => stat switch
    {
        UpgradeStat.Damage => "攻撃力",
        UpgradeStat.Range => "射程",
        UpgradeStat.FireRate => "攻撃速度",
        UpgradeStat.GoldAmount => "産出量",
        UpgradeStat.GoldSpeed => "産出速度",
        UpgradeStat.BuffPower => "強化量",
        UpgradeStat.BuffRange => "効果範囲",
        _ => stat.ToString(),
    };

    public static string MasteryName(Tower tower, UpgradeStat stat) => GetMastery(tower, stat).Name;

    /// <summary>詳細パネルの1行に収まる短い説明。</summary>
    public static string MasteryShort(Tower tower, UpgradeStat stat) => GetMastery(tower, stat).Short;

    public static string MasteryDescription(Tower tower, UpgradeStat stat) => GetMastery(tower, stat).Description;

    /// <summary>その項目の今の値（詳細パネルの各行に出す）。</summary>
    public static string StatValue(Tower tower, UpgradeStat stat) => (tower, stat) switch
    {
        (AttackTower t, UpgradeStat.Damage) => $"{t.Damage}",
        (AttackTower t, UpgradeStat.Range) => $"{t.Range:F0}",
        (AttackTower t, UpgradeStat.FireRate) => $"{t.FireInterval:F2}秒",
        (GoldMine m, UpgradeStat.GoldAmount) => $"{m.GoldAmount}G",
        (GoldMine m, UpgradeStat.GoldSpeed) => $"{m.GoldInterval:F1}秒",
        (SupportTower s, UpgradeStat.BuffPower) => $"+{s.DamageBonus * 100:0}/{s.FireRateBonus * 100:0}%",
        (SupportTower s, UpgradeStat.BuffRange) => $"{s.Range:F0}",
        _ => "",
    };

    private static Mastery GetMastery(Tower tower, UpgradeStat stat) => (tower, stat) switch
    {
        (ArcherTower, UpgradeStat.Damage) => new(
            "徹甲矢", "防御力を無視",
            "矢が敵の防御力を無視する"),
        (ArcherTower, UpgradeStat.Range) => new(
            "三本矢", $"{1 + ArcherTower.VolleyExtraTargets}体同時に攻撃",
            $"射程内の別の敵{ArcherTower.VolleyExtraTargets}体にも同時に矢を撃つ"),
        (ArcherTower, UpgradeStat.FireRate) => new(
            "速射", $"連続攻撃で最大+{ArcherTower.RapidFireBonusPerShot * ArcherTower.RapidFireMaxStacks:P0}速",
            $"同じ敵を撃ち続けると1発ごとに攻撃速度が+{ArcherTower.RapidFireBonusPerShot:P0}"
            + $"（最大+{ArcherTower.RapidFireBonusPerShot * ArcherTower.RapidFireMaxStacks:P0}）。標的が変わると元に戻る"),

        (CannonTower, UpgradeStat.Damage) => new(
            "衝撃弾", $"爆風の敵を{CannonTower.ShockStunDuration:0.#}秒停止",
            $"爆風に巻き込んだ敵をすべて{CannonTower.ShockStunDuration:0.#}秒止める"),
        (CannonTower, UpgradeStat.Range) => new(
            "クラスター弾", $"子弾{CannonTower.ClusterFragments}発が飛び散る",
            $"着弾すると近くの別の敵{CannonTower.ClusterFragments}体へ子弾が飛ぶ（威力{CannonBall.FragmentDamageRate:P0}）"),
        (CannonTower, UpgradeStat.FireRate) => new(
            "連装砲", $"{CannonTower.FollowUpDelay:0.#}秒後に追撃",
            $"攻撃のたびに{CannonTower.FollowUpDelay:0.#}秒遅れてもう1発撃つ（威力{CannonTower.FollowUpDamageRate:P0}）"),

        (FrostTower, UpgradeStat.Damage) => new(
            "氷結", $"{FrostTower.FreezeEveryShots}発ごとに凍結",
            $"{FrostTower.FreezeEveryShots}回に1回、命中した敵を{FrostTower.FreezeDuration:0.#}秒凍らせて止める"),
        (FrostTower, UpgradeStat.Range) => new(
            "ブリザード", "周囲の敵もまとめて減速",
            $"命中した敵の周り（半径{FrostTower.BlizzardRadius:0}）の敵もまとめて減速させる"),
        (FrostTower, UpgradeStat.FireRate) => new(
            "凍傷", $"減速中の被ダメージ+{FrostTower.FrostbiteDamageBonus:P0}",
            $"減速させた敵は、どのタワーから受けるダメージも+{FrostTower.FrostbiteDamageBonus:P0}"),

        (BreakerTower, UpgradeStat.Damage) => new(
            "腐食", $"防御低下が{BreakerTower.CorrosionMaxStacks}回まで重なる",
            $"命中するたびに防御力低下が重なる（最大{BreakerTower.CorrosionMaxStacks}回分）"),
        (BreakerTower, UpgradeStat.Range) => new(
            "酸の飛沫", "周囲の敵も防御低下",
            $"命中した敵の周り（半径{BreakerTower.AcidSplashRadius:0}）の敵の防御力もまとめて下げる"),
        (BreakerTower, UpgradeStat.FireRate) => new(
            "溶解", "防御低下中に継続ダメージ",
            $"防御力を下げた敵に、効果中は毎秒 攻撃力の{BreakerTower.AcidDamageRate:P0} のダメージを与える（防御力無視）"),

        (TeslaTower, UpgradeStat.Damage) => new(
            "過電流", "連鎖で威力が落ちない",
            "連鎖してもダメージが減らない"),
        (TeslaTower, UpgradeStat.Range) => new(
            "分岐雷", $"{TeslaTower.ForkBranches}方向に分かれて連鎖",
            $"電撃が最初の標的から{TeslaTower.ForkBranches}方向に分かれ、それぞれ連鎖する"),
        (TeslaTower, UpgradeStat.FireRate) => new(
            "麻痺", $"{TeslaTower.ParalyzeEveryShots}発ごとに感電停止",
            $"{TeslaTower.ParalyzeEveryShots}回に1回、電撃を受けた敵すべてを{TeslaTower.ParalyzeDuration:0.#}秒しびれさせて止める"),

        (GoldMine, UpgradeStat.GoldAmount) => new(
            "大鉱脈", $"ウェーブ後に{GoldMine.MotherLodeMultiplier}倍産出",
            $"ウェーブを凌ぐたびに産出量の{GoldMine.MotherLodeMultiplier}倍を追加で得る"),
        (GoldMine, UpgradeStat.GoldSpeed) => new(
            "採掘ラッシュ", $"開始{GoldMine.RushDuration:0}秒は2倍速",
            $"ウェーブ開始から{GoldMine.RushDuration:0}秒間、産出間隔が半分になる"),

        (SupportTower, UpgradeStat.BuffPower) => new(
            "鼓舞", $"範囲内の射程+{SupportTower.InspireRangeBonus:P0}",
            $"範囲内のタワーの射程も +{SupportTower.InspireRangeBonus:P0}"),
        (SupportTower, UpgradeStat.BuffRange) => new(
            "索敵網", "範囲内が同時攻撃+1",
            "範囲内のタワーが同時にもう1体を攻撃できる"),

        _ => new("", "", ""),
    };
}
