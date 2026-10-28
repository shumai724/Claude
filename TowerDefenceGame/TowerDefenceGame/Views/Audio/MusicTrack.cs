namespace TowerDefenceGame.Views.Audio;

/// <summary>ループ再生する BGM の種類。</summary>
public enum MusicTrack
{
    /// <summary>BGM を止める（勝敗のジングルを聞かせたいときなど）。</summary>
    None,

    /// <summary>ステージ選択画面。落ち着いた明るい曲。</summary>
    StageSelect,

    /// <summary>ゲーム中。テンポの速い戦闘曲。</summary>
    Battle,
}
