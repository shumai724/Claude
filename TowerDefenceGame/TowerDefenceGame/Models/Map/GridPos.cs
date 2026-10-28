namespace TowerDefenceGame.Models.Map;

/// <summary>マップ上のマス目の位置（列・行）。</summary>
public readonly record struct GridPos(int Col, int Row)
{
    public GridPos Offset(int deltaCol, int deltaRow) => new(Col + deltaCol, Row + deltaRow);

    public override string ToString() => $"({Col}, {Row})";
}
