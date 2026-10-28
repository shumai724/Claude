namespace TowerDefenceGame.Controllers;

/// <summary>プレイヤーの操作に対する結果。効果音などの手応えを返すのに使う。</summary>
public enum InputFeedback
{
    /// <summary>タワーの種類やマップ上のタワーを選んだ。</summary>
    Selected,

    /// <summary>選択を解除した。</summary>
    Cancelled,

    /// <summary>建設できない・お金が足りないなどで操作が受け付けられなかった。</summary>
    Rejected,
}
