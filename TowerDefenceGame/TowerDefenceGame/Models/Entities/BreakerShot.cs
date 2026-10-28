using System.Numerics;
using TowerDefenceGame.Models.Core;

namespace TowerDefenceGame.Models.Entities;

/// <summary>命中した敵の防御力を下げる酸の弾。</summary>
public sealed class BreakerShot : Projectile
{
    /// <param name="maxArmorReduction">
    /// 重ねがけで到達できる防御力低下の上限（ブレイカー攻撃力 Lv5「腐食」）。armorReduction と同じなら重ならない。
    /// </param>
    /// <param name="splashRadius">周囲の敵の防御力も下げる範囲（ブレイカー射程 Lv5「酸の飛沫」）。0 なら標的だけ。</param>
    /// <param name="acidDamagePerSecond">防御力低下と同時に与える継続ダメージ（ブレイカー攻撃速度 Lv5「溶解」）。0 なら与えない。</param>
    public BreakerShot(Vector2 origin, Enemy target, int damage, float speed, int armorReduction, int maxArmorReduction, float duration,
        float splashRadius, float acidDamagePerSecond, HitModifiers modifiers)
        : base(origin, target, damage, speed, modifiers)
    {
        ArmorReduction = armorReduction;
        MaxArmorReduction = Math.Max(armorReduction, maxArmorReduction);
        Duration = duration;
        SplashRadius = splashRadius;
        AcidDamagePerSecond = acidDamagePerSecond;
    }

    public int ArmorReduction { get; }

    public int MaxArmorReduction { get; }

    public float Duration { get; }

    public float SplashRadius { get; }

    public float AcidDamagePerSecond { get; }

    protected override bool OnHit(World world)
    {
        if (!Target.IsActive)
        {
            return false;
        }

        // 先に防御力を下げるので、この弾自身のダメージから効果が乗る
        Corrode(Target);
        if (SplashRadius > 0f)
        {
            foreach (var enemy in world.FindEnemiesInRange(Position, SplashRadius))
            {
                if (enemy != Target)
                {
                    Corrode(enemy);
                }
            }
        }

        Strike(Target, Damage);
        return true;
    }

    private void Corrode(Enemy enemy)
    {
        // かかっている低下に上乗せする（上限まで）。すでにより強い低下がかかっていれば、それを弱めずに時間だけ延ばす
        int current = enemy.ArmorBreakReduction;
        int reduction = Math.Max(current, Math.Min(current + ArmorReduction, MaxArmorReduction));
        enemy.AddEffect(new ArmorBreakEffect(reduction, Duration));
        if (AcidDamagePerSecond > 0f)
        {
            enemy.AddEffect(new AcidEffect(AcidDamagePerSecond, Duration) { Source = Source });
        }
    }
}
