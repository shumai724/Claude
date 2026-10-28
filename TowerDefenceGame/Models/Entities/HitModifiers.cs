namespace TowerDefenceGame.Models.Entities;

/// <summary>弾に乗せる追加効果。攻撃タワーの Lv5 効果で付く。</summary>
/// <param name="PierceArmor">敵の防御力を無視する（アーチャー攻撃力 Lv5「徹甲矢」）。</param>
/// <param name="StunDuration">
/// ダメージを与えた敵を止める秒数。0 なら止めない
/// （キャノン「衝撃弾」・フロスト「氷結」・ライトニング「麻痺」）。
/// </param>
public readonly record struct HitModifiers(bool PierceArmor = false, float StunDuration = 0f)
{
    public static HitModifiers None => default;
}
