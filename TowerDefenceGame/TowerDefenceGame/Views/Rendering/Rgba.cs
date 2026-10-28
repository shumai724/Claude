namespace TowerDefenceGame.Views.Rendering;

/// <summary>UIフレームワークに依存しない色。</summary>
public readonly record struct Rgba(byte R, byte G, byte B, byte A = 255)
{
    public Rgba WithAlpha(byte alpha) => this with { A = alpha };
}
