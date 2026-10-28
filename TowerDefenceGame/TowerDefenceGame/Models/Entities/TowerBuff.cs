namespace TowerDefenceGame.Models.Entities;

/// <summary>司令塔から受けている強化。増加率は 0.25 なら +25%。</summary>
/// <param name="RangeBonus">射程の増加率（司令塔の強化量 Lv5「鼓舞」）。</param>
/// <param name="ExtraTargets">同時に攻撃できる敵の追加数（司令塔の効果範囲 Lv5「索敵網」）。</param>
public readonly record struct TowerBuff(float DamageBonus, float FireRateBonus, float RangeBonus = 0f, int ExtraTargets = 0)
{
    public static TowerBuff None => default;

    public bool IsActive => DamageBonus > 0f || FireRateBonus > 0f || RangeBonus > 0f || ExtraTargets > 0;
}
