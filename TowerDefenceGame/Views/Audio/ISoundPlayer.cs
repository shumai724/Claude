using TowerDefenceGame.Models.Settings;

namespace TowerDefenceGame.Views.Audio;

/// <summary>効果音と BGM の出力先。Controller はこのインターフェース経由で音を鳴らす。</summary>
public interface ISoundPlayer : IDisposable
{
    /// <summary>音声デバイスが使えるか。設定画面での案内に使う。</summary>
    bool IsAvailable { get; }

    /// <summary>ON/OFF・マスター音量・分類ごとの音量を反映する。</summary>
    void Apply(AudioSettings settings);

    void Play(SoundEffect effect);

    /// <summary>BGM を切り替える。同じ曲を指定した場合は途切れずに流し続ける。</summary>
    void PlayMusic(MusicTrack track);
}
