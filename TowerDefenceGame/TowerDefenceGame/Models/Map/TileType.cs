namespace TowerDefenceGame.Models.Map;

/// <summary>マスの種類。</summary>
public enum TileType
{
    /// <summary>草地。タワーを建設できる。</summary>
    Grass,

    /// <summary>敵が通る道。</summary>
    Road,

    /// <summary>岩などの障害物。建設も通行もできない。</summary>
    Obstacle,

    /// <summary>敵の出現地点。</summary>
    Spawn,

    /// <summary>敵の目的地。</summary>
    Goal,

    /// <summary>荒れ地。ハードモードで建設できなくなった草地。通行もできない。</summary>
    Wasteland,
}
