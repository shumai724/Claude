using System.Numerics;
using TowerDefenceGame.Models.Map;
using TowerDefenceGame.Views.Rendering;

namespace TowerDefenceGame.Views.Painters;

/// <summary>マップのマス目を描画する。</summary>
public sealed class MapPainter
{
    public void Draw(IRenderer renderer, GameMap map)
    {
        float size = map.TileSize;
        foreach (var tile in map.Tiles)
        {
            float x = tile.Cell.Col * size;
            float y = tile.Cell.Row * size;
            var color = tile.Type == TileType.Grass && (tile.Cell.Col + tile.Cell.Row) % 2 == 1
                ? Palette.GrassAlt
                : Palette.ForTile(tile.Type);
            renderer.FillRect(x, y, size, size, color);

            var center = map.CellToWorld(tile.Cell);
            switch (tile.Type)
            {
                case TileType.Obstacle:
                    DrawRock(renderer, center, size);
                    break;
                case TileType.Wasteland:
                    DrawCracks(renderer, x, y, size);
                    break;
                case TileType.Spawn:
                    renderer.DrawText("S", center, Palette.White, size * 0.5f, TextAnchor.Center, bold: true);
                    break;
                case TileType.Goal:
                    renderer.DrawText("G", center, Palette.White, size * 0.5f, TextAnchor.Center, bold: true);
                    break;
            }
        }

        for (int col = 1; col < map.Columns; col++)
        {
            renderer.DrawLine(new Vector2(col * size, 0), new Vector2(col * size, map.Height), Palette.GridLine);
        }

        for (int row = 1; row < map.Rows; row++)
        {
            renderer.DrawLine(new Vector2(0, row * size), new Vector2(map.Width, row * size), Palette.GridLine);
        }
    }

    /// <summary>荒れ地のひび割れ。建設できないことが一目で分かるようにする。</summary>
    private static void DrawCracks(IRenderer renderer, float x, float y, float size)
    {
        var crack = Palette.WastelandCrack;
        renderer.DrawLine(new Vector2(x + size * 0.2f, y + size * 0.25f), new Vector2(x + size * 0.5f, y + size * 0.5f), crack, 2f);
        renderer.DrawLine(new Vector2(x + size * 0.5f, y + size * 0.5f), new Vector2(x + size * 0.8f, y + size * 0.35f), crack, 2f);
        renderer.DrawLine(new Vector2(x + size * 0.5f, y + size * 0.5f), new Vector2(x + size * 0.45f, y + size * 0.8f), crack, 2f);
    }

    private static void DrawRock(IRenderer renderer, Vector2 center, float tileSize)
    {
        renderer.FillCircle(center + new Vector2(2, 3), tileSize * 0.34f, Palette.RockShade);
        renderer.FillCircle(center, tileSize * 0.34f, Palette.Rock);
        renderer.FillCircle(center + new Vector2(-tileSize * 0.12f, -tileSize * 0.1f), tileSize * 0.1f, Palette.White.WithAlpha(50));
    }
}
