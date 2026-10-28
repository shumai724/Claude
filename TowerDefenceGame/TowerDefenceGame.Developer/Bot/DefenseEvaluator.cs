using System.Numerics;

namespace TowerDefenceGame.Developer.Bot;

/// <summary>
/// 守りの見積もり結果。
/// Total はウェーブ全体に与えられる見込みのダメージ、Toughest は一番硬い敵1体が倒れるまでに受ける見込みのダメージ。
/// </summary>
internal readonly record struct DefenseScore(float Total, float Toughest);

/// <summary>
/// タワーの並び（実際のもの、または「ここに建てたら」の仮のもの）が、あるウェーブにどれだけダメージを与えられるかを見積もる。
/// 厳密なシミュレーションではなく、人が頭の中でする計算に近い近似：
/// <list type="bullet">
/// <item>タワーが敵を攻撃できる時間 ＝ 射程に収めた道の長さ ÷ 敵の速さ（フロストと重なっていれば遅くなる分だけ長い）</item>
/// <item>1発のダメージ ＝ 攻撃力 − 防御力（ブレイカーと重なっていれば防御力が下がる）</item>
/// <item>キャノン・ライトニングは、敵が密集しているほど1回で多くの敵に当たる</item>
/// <item>同じ区間を多くのタワーが狙うほど、1基あたりの価値は少しずつ下がる（守りを散らす動機になる）</item>
/// </list>
/// </summary>
internal sealed class DefenseEvaluator
{
    /// <summary>この距離の中にいる敵の数を「密集度」とみなす（ワールド座標）。</summary>
    private const float CrowdWindow = 60f;

    /// <summary>フロスト・ブレイカーの効果を受けるとみなす、射程の重なりの最低割合。</summary>
    private const float OverlapThreshold = 0.25f;

    private readonly RouteSamples _samples;

    public DefenseEvaluator(RouteSamples samples)
    {
        _samples = samples;
    }

    /// <summary>道の各点が、いくつの攻撃タワーの射程に入っているか（司令塔の強化は含まない）。</summary>
    public int[] CoverCounts(IEnumerable<BotTower> layout)
    {
        var counts = new int[_samples.Count];
        foreach (var tower in layout.Where(t => t.IsAttack))
        {
            BitSet.AddTo(_samples.InRange(tower.Cell, tower.Position, tower.Range), counts);
        }

        return counts;
    }

    /// <summary>同じ点を狙うタワーが多いほど、1基あたりの価値を少しずつ下げる重み。</summary>
    public static float OverlapWeight(int coverCount) => coverCount <= 1 ? 1f : 1f / (1f + 0.12f * (coverCount - 1));

    public DefenseScore Evaluate(IReadOnlyList<BotTower> layout, WaveProfile wave)
    {
        var supports = layout.Where(t => t.IsSupport).ToList();
        var attacks = new List<Engaged>();
        var counts = new int[_samples.Count];

        foreach (var tower in layout.Where(t => t.IsAttack))
        {
            // 司令塔の効果は重ならず、項目ごとに一番強いものが効く（World.GetBuffAt と同じ考え方）
            float damageBuff = 0f, fireRateBuff = 0f, rangeBuff = 0f;
            int extraTargets = 0;
            foreach (var support in supports)
            {
                if (Vector2.DistanceSquared(support.Position, tower.Position) <= support.Range * support.Range)
                {
                    damageBuff = MathF.Max(damageBuff, support.BuffDamage);
                    fireRateBuff = MathF.Max(fireRateBuff, support.BuffFireRate);
                    rangeBuff = MathF.Max(rangeBuff, support.BuffRange);
                    extraTargets = Math.Max(extraTargets, support.BuffExtraTargets);
                }
            }

            var bits = _samples.InRange(tower.Cell, tower.Position, tower.Range * (1f + rangeBuff));
            int covered = BitSet.Count(bits);
            if (covered == 0)
            {
                continue;
            }

            BitSet.AddTo(bits, counts);
            attacks.Add(new Engaged(tower, bits, covered, damageBuff, fireRateBuff, extraTargets));
        }

        var frosts = attacks.Where(a => a.Tower.SlowRate > 0f).ToList();
        var breakers = attacks.Where(a => a.Tower.ArmorReduction > 0).ToList();
        float crowd = Math.Clamp(wave.SpawnRate * CrowdWindow / wave.AverageSpeed, 0.3f, 3f);

        float total = 0f;
        float toughest = 0f;
        foreach (var a in attacks)
        {
            var t = a.Tower;
            float pathLength = BitSet.Sum(a.Bits, i => OverlapWeight(counts[i]) * _samples.GoalWeight(i)) * RouteSamples.Spacing;

            // 重なっているフロスト・ブレイカーの効果
            float slow = 0f;
            float damageTaken = 1f;
            int armorReduction = 0;
            foreach (var f in frosts)
            {
                float overlap = (float)BitSet.CountAnd(a.Bits, f.Bits) / a.Covered;
                slow = MathF.Max(slow, overlap * f.Tower.SlowRate);
                if (overlap >= OverlapThreshold)
                {
                    damageTaken = MathF.Max(damageTaken, 1f + f.Tower.DamageTakenBonus * overlap);
                }
            }

            foreach (var b in breakers)
            {
                if ((float)BitSet.CountAnd(a.Bits, b.Bits) / a.Covered >= OverlapThreshold)
                {
                    armorReduction = Math.Max(armorReduction, b.Tower.ArmorReduction);
                }
            }

            float damage = t.Damage * (1f + a.DamageBuff);
            float interval = t.Interval / (1f + a.FireRateBuff);
            float hits = HitsPerAttack(t, a.ExtraTargets, crowd);

            float damagePerSecond = 0f;
            float timeInRange = 0f;
            foreach (var g in wave.Groups)
            {
                float share = g.Count * g.Hp / wave.TotalHp;
                timeInRange += share * pathLength / (g.Spec.Speed * (1f - slow));
                damagePerSecond += share * DamagePerSecond(t, damage, interval, hits, g.Spec.Armor, armorReduction, damageTaken);
            }

            // 敵が途切れなく来れば出現している間ずっと、まばらなら敵がいる間だけ攻撃できる
            float busy = MathF.Min(wave.Duration, wave.TotalCount * timeInRange) + timeInRange;
            total += damagePerSecond * busy;

            // 一番硬い敵1体：同時に射程内にいる敵が多いほど、その敵に向く攻撃は減る
            var tough = wave.Toughest;
            float toughTime = pathLength / (tough.Spec.Speed * (1f - slow));
            float concurrency = MathF.Max(1f, wave.SpawnRate * timeInRange);
            float focus = MathF.Min(1f, hits / concurrency);
            toughest += DamagePerSecond(t, damage, interval, 1f, tough.Spec.Armor, armorReduction, damageTaken) * toughTime * focus;
        }

        return new DefenseScore(total, toughest);
    }

    /// <summary>1回の攻撃で何体分のダメージになるか。敵が密集しているほど範囲攻撃・連鎖が活きる。</summary>
    private static float HitsPerAttack(BotTower t, int buffExtraTargets, float crowd)
    {
        float hits = 1f + (t.ExtraTargets + buffExtraTargets) * MathF.Min(1f, crowd);

        if (t.Splash > 0f)
        {
            hits *= 1f + MathF.Min(2.5f, crowd * t.Splash / 45f * 0.8f);
        }

        if (t.ChainCount > 0)
        {
            float reach = MathF.Min(1f, crowd * t.ChainRange / 75f);
            float chained = 0f;
            float factor = 1f;
            for (int k = 0; k < t.ChainCount; k++)
            {
                factor *= t.Falloff;
                chained += factor * reach;
            }

            hits *= 1f + t.Branches * chained;
        }

        return hits;
    }

    private static float DamagePerSecond(BotTower t, float damage, float interval, float hits, int armor, int armorReduction, float damageTaken)
    {
        int effectiveArmor = armor - armorReduction;
        if (t.PierceArmor)
        {
            effectiveArmor = Math.Min(0, effectiveArmor);
        }

        float perHit = MathF.Max(1f, damage - effectiveArmor) * damageTaken;
        return perHit * hits / interval * t.DamageFactor + t.AcidDps;
    }

    private sealed record Engaged(BotTower Tower, ulong[] Bits, int Covered, float DamageBuff, float FireRateBuff, int ExtraTargets);
}
