using TowerDefenceGame.Models.Settings;

namespace TowerDefenceGame.Views.Audio;

/// <summary>音声デバイスが使えない環境向けの、何も鳴らさない実装。</summary>
public sealed class NullSoundPlayer : ISoundPlayer
{
    public bool IsAvailable => false;

    public void Apply(AudioSettings settings)
    {
    }

    public void Play(SoundEffect effect)
    {
    }

    public void PlayMusic(MusicTrack track)
    {
    }

    public void Dispose()
    {
    }
}
