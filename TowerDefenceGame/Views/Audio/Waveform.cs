namespace TowerDefenceGame.Views.Audio;

/// <summary>合成に使う基本波形。</summary>
public enum Waveform
{
    /// <summary>柔らかい音。低音の衝撃やきらめきに使う。</summary>
    Sine,

    /// <summary>レトロゲーム風のはっきりした音。</summary>
    Square,

    /// <summary>サインと矩形の中間。軽いクリックや電子音に使う。</summary>
    Triangle,

    /// <summary>ざらついた音。警告やファンファーレに使う。</summary>
    Saw,
}
