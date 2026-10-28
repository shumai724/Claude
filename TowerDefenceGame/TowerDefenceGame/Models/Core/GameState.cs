namespace TowerDefenceGame.Models.Core;

/// <summary>ゲームの進行状態。</summary>
public enum GameState
{
    Ready,
    Playing,
    Paused,
    GameOver,
    StageClear,
}
