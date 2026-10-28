namespace TowerDefenceGame.Models.Settings;

/// <summary>システム設定のルート。今後、音以外の設定もここに追加する。</summary>
public sealed class GameSettings
{
    public AudioSettings Audio { get; set; } = new();
}
