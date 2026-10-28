namespace TowerDefenceGame.Models.Settings;

/// <summary>音量を個別に調整できる音の分類（BGM と効果音の種類）。</summary>
public enum SoundCategory
{
    /// <summary>選択・解除・操作できないときの音。</summary>
    Interface,

    /// <summary>建設・強化・売却。</summary>
    Building,

    /// <summary>弾や電撃の命中。</summary>
    Hit,

    /// <summary>敵の撃破とゴール到達。</summary>
    Enemy,

    /// <summary>金鉱の産出。</summary>
    Gold,

    /// <summary>ウェーブ開始・終了、ゲームオーバー、クリア。</summary>
    Progress,

    /// <summary>BGM。保存済みの設定と番号がずれないよう末尾に追加している。</summary>
    Music,
}
