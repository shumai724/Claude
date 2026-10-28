using System.Numerics;
using TowerDefenceGame.Controllers;
using TowerDefenceGame.Models.Core;
using TowerDefenceGame.Models.Entities;
using TowerDefenceGame.Views.Rendering;

namespace TowerDefenceGame.Views.Painters;

/// <summary>選択中タワーの射程や、建設予定地のプレビューを描画する。</summary>
public sealed class SelectionPainter
{
    public void Draw(IRenderer renderer, Game game, InputController input)
    {
        var map = game.Map;
        float size = map.TileSize;

        if (input.SelectedTower is { IsActive: true } tower)
        {
            DrawRange(renderer, tower.Position, tower.Range);
            renderer.DrawRect(tower.Cell.Col * size, tower.Cell.Row * size, size, size, Palette.Selection, 2f);

            // 司令塔を選んだら、強化している攻撃タワーを示す
            if (tower is SupportTower support)
            {
                foreach (var buffed in game.World.Towers.OfType<AttackTower>().Where(t => support.Covers(t.Position)))
                {
                    renderer.DrawRect(buffed.Cell.Col * size + 2, buffed.Cell.Row * size + 2, size - 4, size - 4, Palette.BuffMark, 2f);
                }
            }
        }

        if (input.SelectedTowerType is { } type && input.HoverCell is { } cell && map.Contains(cell))
        {
            bool canPlace = game.CanPlaceTower(type, cell);
            var center = map.CellToWorld(cell);
            float range = game.GetTowerSpec(type).Range;

            renderer.FillRect(cell.Col * size, cell.Row * size, size, size, canPlace ? Palette.PlaceOk : Palette.PlaceNg);
            DrawRange(renderer, center, range);
        }
    }

    /// <summary>射程・効果範囲の円。範囲を持たない建物（金鉱）では何も描かない。</summary>
    private static void DrawRange(IRenderer renderer, Vector2 center, float range)
    {
        if (range <= 0f)
        {
            return;
        }

        renderer.FillCircle(center, range, Palette.RangeFill);
        renderer.DrawCircle(center, range, Palette.RangeOutline, 1.5f);
    }
}
