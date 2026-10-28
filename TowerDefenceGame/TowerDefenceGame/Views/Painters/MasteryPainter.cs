using System.Numerics;
using TowerDefenceGame.Models.Entities;
using TowerDefenceGame.Views.Rendering;

namespace TowerDefenceGame.Views.Painters;

/// <summary>
/// Lv5 にした建物の見た目。土台が銅色になって本体の色で脈打ち、
/// 本体は Lv5 効果ごとに形が変わる（砲身が3本になる、雪の結晶が付く など）。
/// </summary>
internal static class MasteryPainter
{
    private const float Tau = MathF.PI * 2f;

    private static readonly float[] FanAngles = [-0.4f, 0f, 0.4f];
    private static readonly float[] ForkSigns = [-1f, 1f];

    /// <summary>金塊を置く位置（本体の半径に対する比率）。</summary>
    private static readonly Vector2[] NuggetOffsets = [new(-0.95f, 0.75f), new(0.95f, 0.75f), new(0f, 1.1f)];

    /// <summary>土台。通常の灰色の代わりに銅色の地を敷き、本体の色の光を脈打たせる。</summary>
    public static void DrawBase(IRenderer renderer, Tower tower, Vector2 p, float size, float time)
    {
        float half = size / 2f;
        var color = Palette.ForTower(tower.Spec.Type);
        renderer.FillRect(p.X - half, p.Y - half, size, size, Palette.MasteredBase);
        renderer.DrawRect(p.X - half, p.Y - half, size, size, Palette.TowerBaseEdge, 2f);
        renderer.DrawCircle(p, half * 0.92f, color.WithAlpha(Alpha(40, 160, Pulse(time, 2.5f))), 3f);
    }

    /// <summary>攻撃タワーの本体と砲身。aim は砲身の向き（単位ベクトル）。</summary>
    public static void DrawAttackTower(IRenderer renderer, AttackTower tower, UpgradeStat stat, Vector2 p, float size, Vector2 aim, float time)
    {
        float half = size / 2f;
        float r = size * 0.32f;
        var color = Palette.ForTower(tower.Spec.Type);
        var side = new Vector2(-aim.Y, aim.X);

        renderer.FillCircle(p, r, color);
        renderer.DrawCircle(p, r, Palette.MasteryGlow, 2f);

        switch (tower, stat)
        {
            // ---- アーチャー ----
            case (ArcherTower, UpgradeStat.Damage):
                // 徹甲矢: 砲身の先に鋼の矢じり
                Barrel(renderer, p, aim, half * 0.8f, 5f);
                var tip = p + aim * (half + 3f);
                var neck = p + aim * (half - 3f);
                renderer.DrawLine(neck, tip, Palette.SteelTip, 2.5f);
                renderer.DrawLine(tip, neck + side * 4f, Palette.SteelTip, 2.5f);
                renderer.DrawLine(tip, neck - side * 4f, Palette.SteelTip, 2.5f);
                break;

            case (ArcherTower, UpgradeStat.Range):
                // 三本矢: 扇状に3本の砲身
                foreach (float angle in FanAngles)
                {
                    Barrel(renderer, p, Rotate(aim, angle), half, 3.5f);
                }

                break;

            case (ArcherTower archer, UpgradeStat.FireRate):
                // 速射: 回転する弾倉。加速しているほど速く回る
                float spin = time * (3f + 12f * archer.RapidFireBonus);
                for (int k = 0; k < 3; k++)
                {
                    renderer.FillCircle(p + Polar(spin + k * Tau / 3f, r * 0.55f), 2.2f, Palette.Barrel);
                }

                Barrel(renderer, p, aim, half, 5f);
                break;

            // ---- キャノン ----
            case (CannonTower, UpgradeStat.Damage):
                // 衝撃弾: 太い砲身と、砲口から広がる衝撃波
                var muzzle = p + aim * half;
                float wave = Frac(time * 1.2f);
                Barrel(renderer, p, aim, half, 8f);
                renderer.DrawCircle(muzzle, 3f + 7f * wave, color.WithAlpha(Alpha(0, 220, 1f - wave)), 2f);
                renderer.FillCircle(muzzle, 4.5f, Palette.Barrel);
                break;

            case (CannonTower, UpgradeStat.Range):
                // クラスター弾: 本体の四隅に子弾を抱える
                for (int k = 0; k < 4; k++)
                {
                    var bomb = p + Polar(Tau / 8f + k * Tau / 4f, r * 1.2f);
                    renderer.FillCircle(bomb, 2.8f, Palette.CannonBallColor);
                    renderer.FillCircle(bomb - new Vector2(0.8f), 0.9f, Palette.White.WithAlpha(180));
                }

                Barrel(renderer, p, aim, half, 6f);
                break;

            case (CannonTower, UpgradeStat.FireRate):
                // 連装砲: 平行な2本の砲身
                Barrel(renderer, p + side * 3.5f, aim, half, 4.5f);
                Barrel(renderer, p - side * 3.5f, aim, half, 4.5f);
                break;

            // ---- フロスト ----
            case (FrostTower, UpgradeStat.Damage):
                // 氷結: 本体に雪の結晶
                Barrel(renderer, p, aim, half, 5f);
                for (int k = 0; k < 6; k++)
                {
                    renderer.DrawLine(p, p + Polar(Tau / 12f + k * Tau / 6f, r * 0.8f), Palette.IceCrystal, 1.5f);
                }

                renderer.FillCircle(p, 2.5f, Palette.IceCrystal);
                break;

            case (FrostTower, UpgradeStat.Range):
                // ブリザード: 周りを舞う雪
                Barrel(renderer, p, aim, half, 5f);
                for (int k = 0; k < 6; k++)
                {
                    renderer.FillCircle(p + Polar(time * 1.5f + k * Tau / 6f, half * 0.85f), 1.8f, Palette.IceCrystal);
                }

                break;

            case (FrostTower, UpgradeStat.FireRate):
                // 凍傷: 本体から突き出す氷柱
                for (int k = 0; k < 8; k++)
                {
                    var dir = Polar(k * Tau / 8f, 1f);
                    renderer.DrawLine(p + dir * r, p + dir * (r + 4.5f), Palette.IceCrystal, 2f);
                }

                Barrel(renderer, p, aim, half, 5f);
                break;

            // ---- ブレイカー ----
            case (BreakerTower, UpgradeStat.Damage):
                // 腐食: 重なっていく防御低下を表す同心円
                for (int k = 1; k <= 3; k++)
                {
                    renderer.DrawCircle(p, r * 0.25f * k, Palette.CorrosionRing, 1.5f);
                }

                Barrel(renderer, p, aim, half, 5f);
                break;

            case (BreakerTower, UpgradeStat.Range):
                // 酸の飛沫: 周りを回る酸の滴
                for (int k = 0; k < 4; k++)
                {
                    renderer.FillCircle(p + Polar(time * 0.8f + k * Tau / 4f, r + 3.5f), 2.2f, Palette.AcidDroplet);
                }

                Barrel(renderer, p, aim, half, 5f);
                break;

            case (BreakerTower, UpgradeStat.FireRate):
                // 溶解: 本体から湧き上がる泡
                Barrel(renderer, p, aim, half, 5f);
                for (int k = 0; k < 3; k++)
                {
                    float phase = Frac(time * 0.7f + k / 3f);
                    var bubble = p + new Vector2((k - 1) * r * 0.5f, r * 0.5f - phase * r);
                    renderer.DrawCircle(bubble, 1.2f + phase * 1.8f, Palette.AcidMark, 1.5f);
                }

                break;

            // ---- ライトニング ----
            case (TeslaTower, UpgradeStat.Damage):
                // 過電流: 明滅するコイルと白熱した芯
                renderer.DrawCircle(p, r + 3f, Palette.LightningGlow.WithAlpha(Alpha(80, 255, Pulse(time, 8f))), 2f);
                Barrel(renderer, p, aim, half, 5f);
                renderer.FillCircle(p, r * 0.35f, Palette.White);
                break;

            case (TeslaTower, UpgradeStat.Range):
                // 分岐雷: 先が二股に分かれた砲身
                var fork = p + aim * (half * 0.55f);
                Barrel(renderer, p, aim, half * 0.55f, 5f);
                foreach (float sign in ForkSigns)
                {
                    var prong = fork + Rotate(aim, sign * 0.6f) * (half * 0.55f);
                    renderer.DrawLine(fork, prong, Palette.Barrel, 3.5f);
                    renderer.FillCircle(prong, 2f, Palette.LightningGlow);
                }

                break;

            case (TeslaTower, UpgradeStat.FireRate):
                // 麻痺: 本体の周りに走る火花（毎フレーム場所が変わってちらつく）
                Barrel(renderer, p, aim, half, 5f);
                for (int k = 0; k < 2; k++)
                {
                    float a = Random.Shared.NextSingle() * Tau;
                    var s0 = p + Polar(a, r);
                    var s1 = p + Polar(a + 0.35f, r + 3.5f);
                    var s2 = p + Polar(a - 0.1f, r + 6.5f);
                    renderer.DrawLine(s0, s1, Palette.StunMark, 2f);
                    renderer.DrawLine(s1, s2, Palette.StunMark, 2f);
                }

                break;

            default:
                Barrel(renderer, p, aim, half, 5f);
                break;
        }
    }

    /// <summary>金鉱の飾り。通常の本体の上に重ねて描く。</summary>
    public static void DrawGoldMine(IRenderer renderer, GoldMine mine, UpgradeStat stat, Vector2 p, float size, float time)
    {
        float r = size * 0.3f;
        switch (stat)
        {
            case UpgradeStat.GoldAmount:
                // 大鉱脈: 足元に積まれた金塊
                foreach (var offset in NuggetOffsets)
                {
                    var nugget = p + offset * r;
                    renderer.FillCircle(nugget, 3f, Palette.HudGold);
                    renderer.DrawCircle(nugget, 3f, Palette.TowerBaseEdge, 1f);
                }

                break;

            case UpgradeStat.GoldSpeed:
                // 採掘ラッシュ: 回る歯車。ラッシュ中は速く回る
                float spin = time * (mine.IsRushing ? 6f : 1.2f);
                for (int k = 0; k < 8; k++)
                {
                    var dir = Polar(spin + k * Tau / 8f, 1f);
                    renderer.DrawLine(p + dir * (r + 1f), p + dir * (r + 4f), Palette.MineGear, 3f);
                }

                break;
        }
    }

    /// <summary>司令塔の飾り。通常の本体の上に重ねて描く。</summary>
    public static void DrawSupportTower(IRenderer renderer, UpgradeStat stat, Vector2 p, float size, float time)
    {
        float r = size * 0.3f;
        switch (stat)
        {
            case UpgradeStat.BuffPower:
                // 鼓舞: 脈打つ光の筋
                float length = 2f + 3f * Pulse(time, 3f);
                for (int k = 0; k < 8; k++)
                {
                    var dir = Polar(k * Tau / 8f, 1f);
                    renderer.DrawLine(p + dir * (r + 1.5f), p + dir * (r + 1.5f + length), Palette.BuffMark, 2f);
                }

                break;

            case UpgradeStat.BuffRange:
                // 索敵網: 回るレーダー
                renderer.DrawCircle(p, r + 5f, Palette.White.WithAlpha(90), 1f);
                renderer.DrawLine(p, p + Polar(time * 2.5f, r + 5f), Palette.White.WithAlpha(220), 2f);
                break;
        }
    }

    private static void Barrel(IRenderer renderer, Vector2 from, Vector2 dir, float length, float thickness) =>
        renderer.DrawLine(from, from + dir * length, Palette.Barrel, thickness);

    private static Vector2 Polar(float angle, float length) => new(MathF.Cos(angle) * length, MathF.Sin(angle) * length);

    private static Vector2 Rotate(Vector2 v, float angle)
    {
        float cos = MathF.Cos(angle);
        float sin = MathF.Sin(angle);
        return new Vector2(v.X * cos - v.Y * sin, v.X * sin + v.Y * cos);
    }

    /// <summary>0 → 1 → 0 を周期的に繰り返す。</summary>
    private static float Pulse(float time, float speed) => 0.5f + 0.5f * MathF.Sin(time * speed);

    /// <summary>小数部分（0 以上 1 未満）。</summary>
    private static float Frac(float value) => value - MathF.Floor(value);

    /// <summary>
    /// min〜max の間で t に応じた透明度。描画側はペンとブラシを色ごとにキャッシュするので、
    /// 色の種類が増えすぎないよう 16 段刻みに丸める。
    /// </summary>
    private static byte Alpha(float min, float max, float t) =>
        (byte)Math.Clamp(MathF.Round((min + (max - min) * t) / 16f) * 16f, 0f, 255f);
}
