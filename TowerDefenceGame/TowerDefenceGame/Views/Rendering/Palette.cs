using TowerDefenceGame.Models.Entities;
using TowerDefenceGame.Models.Map;

namespace TowerDefenceGame.Views.Rendering;

/// <summary>画面で使う色の定義。</summary>
public static class Palette
{
    public static readonly Rgba Background = new(24, 28, 32);
    public static readonly Rgba White = new(255, 255, 255);
    public static readonly Rgba GridLine = new(0, 0, 0, 28);

    public static readonly Rgba Grass = new(104, 159, 86);
    public static readonly Rgba GrassAlt = new(97, 150, 80);
    public static readonly Rgba Road = new(198, 170, 122);
    public static readonly Rgba Obstacle = new(104, 150, 86);
    public static readonly Rgba Rock = new(118, 116, 110);
    public static readonly Rgba RockShade = new(88, 86, 82);
    public static readonly Rgba Spawn = new(176, 72, 64);
    public static readonly Rgba Goal = new(64, 92, 176);
    public static readonly Rgba Wasteland = new(128, 112, 92);
    public static readonly Rgba WastelandCrack = new(80, 66, 52);
    public static readonly Rgba LevelBadge = new(235, 70, 60);

    public static readonly Rgba TowerBase = new(66, 68, 74);
    public static readonly Rgba TowerBaseEdge = new(40, 42, 46);
    public static readonly Rgba Barrel = new(28, 28, 30);
    public static readonly Rgba LevelPip = new(255, 214, 64);
    public static readonly Rgba MasteryGlow = new(255, 150, 30);
    public static readonly Rgba StunMark = new(255, 240, 120);

    // Lv5 にした建物の見た目
    public static readonly Rgba MasteredBase = new(96, 80, 56);
    public static readonly Rgba SteelTip = new(215, 222, 232);
    public static readonly Rgba IceCrystal = new(235, 250, 255);
    public static readonly Rgba CorrosionRing = new(90, 30, 130);
    public static readonly Rgba AcidDroplet = new(225, 170, 255);
    public static readonly Rgba MineGear = new(190, 140, 20);

    public static readonly Rgba EnemyOutline = new(20, 20, 20, 160);
    public static readonly Rgba SlowOutline = new(150, 220, 255);
    public static readonly Rgba HpBack = new(90, 20, 20);
    public static readonly Rgba HpFill = new(84, 220, 84);

    public static readonly Rgba ArmorBreakOutline = new(190, 90, 230);
    public static readonly Rgba FrostbiteMark = new(225, 245, 255);
    public static readonly Rgba AcidMark = new(150, 230, 60);
    public static readonly Rgba ArmorMark = new(60, 60, 70, 200);
    public static readonly Rgba BuffMark = new(255, 150, 40);

    public static readonly Rgba ArrowColor = new(92, 60, 28);
    public static readonly Rgba CannonBallColor = new(30, 30, 30);
    public static readonly Rgba FrostShotColor = new(170, 230, 255);
    public static readonly Rgba BreakerShotColor = new(180, 90, 230);
    public static readonly Rgba LightningGlow = new(120, 170, 255);

    public static readonly Rgba RangeFill = new(255, 255, 255, 36);
    public static readonly Rgba RangeOutline = new(255, 255, 255, 170);
    public static readonly Rgba Selection = new(255, 230, 80);
    public static readonly Rgba PlaceOk = new(80, 255, 80, 90);
    public static readonly Rgba PlaceNg = new(255, 60, 60, 90);

    public static readonly Rgba HudBack = new(32, 36, 44);
    public static readonly Rgba HudText = new(235, 235, 235);
    public static readonly Rgba HudGold = new(255, 214, 64);
    public static readonly Rgba HudLife = new(255, 110, 110);
    public static readonly Rgba HudSub = new(160, 170, 185);
    public static readonly Rgba Overlay = new(0, 0, 0, 150);

    public static Rgba ForTile(TileType type) => type switch
    {
        TileType.Road => Road,
        TileType.Obstacle => Obstacle,
        TileType.Spawn => Spawn,
        TileType.Goal => Goal,
        TileType.Wasteland => Wasteland,
        _ => Grass,
    };

    public static Rgba ForTower(TowerType type) => type switch
    {
        TowerType.Archer => new Rgba(96, 168, 76),
        TowerType.Cannon => new Rgba(224, 130, 50),
        TowerType.Frost => new Rgba(110, 190, 245),
        TowerType.Breaker => new Rgba(160, 80, 210),
        TowerType.Tesla => new Rgba(70, 110, 255),
        TowerType.GoldMine => new Rgba(240, 195, 40),
        TowerType.Support => new Rgba(230, 100, 160),
        _ => White,
    };

    public static Rgba ForEnemy(EnemyType type) => type switch
    {
        EnemyType.Normal => new Rgba(220, 70, 70),
        EnemyType.Fast => new Rgba(240, 210, 60),
        EnemyType.Tank => new Rgba(140, 90, 190),
        _ => White,
    };
}
