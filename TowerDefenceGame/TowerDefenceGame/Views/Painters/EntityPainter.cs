using System.Diagnostics;
using System.Numerics;
using TowerDefenceGame.Models.Core;
using TowerDefenceGame.Models.Entities;
using TowerDefenceGame.Views.Rendering;

namespace TowerDefenceGame.Views.Painters;

/// <summary>建物・敵・弾を描画する。</summary>
public sealed class EntityPainter
{
    private const float HpBarWidth = 26f;
    private const float HpBarHeight = 4f;
    private const float GoldPopupRise = 18f;

    private readonly Random _jitter = new();

    /// <summary>Lv5 の建物のアニメーション（回転・明滅）に使う経過時間。</summary>
    private readonly Stopwatch _clock = Stopwatch.StartNew();

    public void Draw(IRenderer renderer, World world, float tileSize)
    {
        float time = (float)_clock.Elapsed.TotalSeconds;
        foreach (var tower in world.Towers)
        {
            DrawTower(renderer, tower, tileSize, time);
        }

        foreach (var enemy in world.Enemies)
        {
            if (enemy.IsActive)
            {
                DrawEnemy(renderer, enemy);
            }
        }

        foreach (var projectile in world.Projectiles)
        {
            if (projectile.IsActive)
            {
                DrawProjectile(renderer, projectile);
            }
        }

        // 「+○G」はほかの物に隠れないよう最後に描く
        foreach (var tower in world.Towers)
        {
            if (tower is GoldMine { PopupTime: > 0f } mine)
            {
                DrawGoldPopup(renderer, mine, tileSize);
            }
        }
    }

    public static float EnemyRadius(EnemyType type) => type switch
    {
        EnemyType.Fast => 7f,
        EnemyType.Tank => 14f,
        _ => 10f,
    };

    private static void DrawTower(IRenderer renderer, Tower tower, float tileSize, float time)
    {
        var p = tower.Position;
        float size = tileSize * 0.8f;
        float half = size / 2f;
        var color = Palette.ForTower(tower.Spec.Type);
        var mastered = tower.MasteredStat;

        if (mastered is null)
        {
            renderer.FillRect(p.X - half, p.Y - half, size, size, Palette.TowerBase);
            renderer.DrawRect(p.X - half, p.Y - half, size, size, Palette.TowerBaseEdge, 2f);
        }
        else
        {
            MasteryPainter.DrawBase(renderer, tower, p, size, time);
        }

        switch (tower)
        {
            case GoldMine mine:
                renderer.FillCircle(p, size * 0.3f, color);
                renderer.DrawText("G", p, Palette.TowerBaseEdge, size * 0.42f, TextAnchor.Center, bold: true);
                // 次の産出までのゲージ
                renderer.FillRect(p.X - half + 3, p.Y - half + 3, (size - 6) * mine.Progress, 3, color);
                if (mastered is { } mineStat)
                {
                    MasteryPainter.DrawGoldMine(renderer, mine, mineStat, p, size, time);
                }

                break;

            case SupportTower:
                renderer.FillCircle(p, size * 0.3f, color);
                renderer.DrawLine(p - new Vector2(size * 0.16f, 0), p + new Vector2(size * 0.16f, 0), Palette.White, 3f);
                renderer.DrawLine(p - new Vector2(0, size * 0.16f), p + new Vector2(0, size * 0.16f), Palette.White, 3f);
                if (mastered is { } supportStat)
                {
                    MasteryPainter.DrawSupportTower(renderer, supportStat, p, size, time);
                }

                break;

            case AttackTower attack:
                var aim = attack.Target is { } target && (target.Position - p).LengthSquared() > 0.01f
                    ? Vector2.Normalize(target.Position - p)
                    : (Vector2?)null;
                if (mastered is { } attackStat)
                {
                    // Lv5 の砲身は形そのものが特徴なので、標的がいないときも上向きに出しておく
                    MasteryPainter.DrawAttackTower(renderer, attack, attackStat, p, size, aim ?? -Vector2.UnitY, time);
                }
                else
                {
                    renderer.FillCircle(p, size * 0.3f, color);
                    if (aim is { } dir)
                    {
                        renderer.DrawLine(p, p + dir * half, Palette.Barrel, 5f);
                    }
                }

                if (attack.Buff.IsActive)
                {
                    renderer.DrawText("▲", new Vector2(p.X + half - 7, p.Y - half + 6), Palette.BuffMark, 11f, TextAnchor.Center, bold: true);
                }

                break;
        }

        // 項目ごとのレベルを小さなバーで表示する（左から強化項目の順）
        for (int s = 0; s < tower.UpgradeStats.Count; s++)
        {
            var stat = tower.UpgradeStats[s];
            float barY = p.Y + half - 4 - s * 4;
            for (int lv = 1; lv < tower.GetLevel(stat); lv++)
            {
                renderer.FillRect(p.X - half + 3 + (lv - 1) * 5, barY, 4, 2.5f, tower.IsMastered(stat) ? Palette.MasteryGlow : Palette.LevelPip);
            }
        }

        if (tower.MasteredStat is not null)
        {
            renderer.DrawRect(p.X - half - 1, p.Y - half - 1, size + 2, size + 2, Palette.MasteryGlow, 2f);
            renderer.DrawText("★", new Vector2(p.X - half + 7, p.Y - half + 7), Palette.MasteryGlow, 11f, TextAnchor.Center, bold: true);
        }
    }

    private static void DrawEnemy(IRenderer renderer, Enemy enemy)
    {
        var p = enemy.Position;
        float radius = EnemyRadius(enemy.Spec.Type);

        renderer.FillCircle(p, radius, Palette.ForEnemy(enemy.Spec.Type));
        if (enemy.IsArmorBroken)
        {
            renderer.DrawCircle(p, radius + 3f, Palette.ArmorBreakOutline, 2f);
        }

        renderer.DrawCircle(p, radius, enemy.IsSlowed ? Palette.SlowOutline : Palette.EnemyOutline, enemy.IsSlowed ? 3f : 1.5f);
        if (enemy.Armor > 0)
        {
            // 防御力のある敵は内側に装甲の輪を描く
            renderer.DrawCircle(p, radius * 0.55f, Palette.ArmorMark, 2f);
        }

        if (enemy.IsStunned)
        {
            renderer.DrawText("✦", p - new Vector2(0, radius + 4), Palette.StunMark, 12f, TextAnchor.Center, bold: true);
        }

        // 凍傷（被ダメージ増加）は左上、溶解（継続ダメージ）は右上の小さな点で示す
        float markOffset = radius * 0.75f;
        if (enemy.IsFrostbitten)
        {
            renderer.FillCircle(p + new Vector2(-markOffset, -markOffset), 3f, Palette.FrostbiteMark);
        }

        if (enemy.IsMelting)
        {
            renderer.FillCircle(p + new Vector2(markOffset, -markOffset), 3f, Palette.AcidMark);
        }

        if (enemy.Hp < enemy.MaxHp)
        {
            float x = p.X - HpBarWidth / 2f;
            float y = p.Y - radius - 8f;
            renderer.FillRect(x, y, HpBarWidth, HpBarHeight, Palette.HpBack);
            renderer.FillRect(x, y, HpBarWidth * enemy.HpRatio, HpBarHeight, Palette.HpFill);
        }
    }

    private void DrawProjectile(IRenderer renderer, Projectile projectile)
    {
        switch (projectile)
        {
            case LightningBolt bolt:
                DrawLightning(renderer, bolt);
                break;
            case CannonBall ball:
                renderer.FillCircle(projectile.Position, ball.IsFragment ? 3f : 5f, Palette.CannonBallColor);
                break;
            case FrostShot:
                renderer.FillCircle(projectile.Position, 4f, Palette.FrostShotColor);
                break;
            case BreakerShot:
                renderer.FillCircle(projectile.Position, 4.5f, Palette.BreakerShotColor);
                break;
            default:
                renderer.FillCircle(projectile.Position, 3f, Palette.ArrowColor);
                break;
        }
    }

    /// <summary>連鎖した経路を、少しジグザグさせた稲妻として描く。時間とともに薄くなる。</summary>
    private void DrawLightning(IRenderer renderer, LightningBolt bolt)
    {
        byte alpha = (byte)(255 * bolt.Fade);
        var glow = Palette.LightningGlow.WithAlpha((byte)(alpha / 2));
        var core = Palette.White.WithAlpha(alpha);
        foreach (var (from, to) in bolt.Segments)
        {
            var normal = Vector2.Normalize(new Vector2(-(to.Y - from.Y), to.X - from.X) + new Vector2(0.0001f));
            var mid = Vector2.Lerp(from, to, 0.5f) + normal * (float)(_jitter.NextDouble() * 16 - 8);
            renderer.DrawLine(from, mid, glow, 5f);
            renderer.DrawLine(mid, to, glow, 5f);
            renderer.DrawLine(from, mid, core, 1.5f);
            renderer.DrawLine(mid, to, core, 1.5f);
        }
    }

    private static void DrawGoldPopup(IRenderer renderer, GoldMine mine, float tileSize)
    {
        float rise = (1f - mine.PopupRatio) * GoldPopupRise;
        var position = mine.Position - new Vector2(0, tileSize * 0.55f + rise);
        byte alpha = (byte)(255 * Math.Clamp(mine.PopupRatio * 1.5f, 0f, 1f));
        renderer.DrawText($"+{mine.LastPayout}G", position + new Vector2(1, 1), Palette.TowerBaseEdge.WithAlpha(alpha), 14f, TextAnchor.Center, bold: true);
        renderer.DrawText($"+{mine.LastPayout}G", position, Palette.HudGold.WithAlpha(alpha), 14f, TextAnchor.Center, bold: true);
    }
}
