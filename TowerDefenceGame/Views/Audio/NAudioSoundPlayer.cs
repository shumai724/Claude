using System.Diagnostics;
using NAudio;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using TowerDefenceGame.Models.Settings;

namespace TowerDefenceGame.Views.Audio;

/// <summary>
/// NAudio で効果音と BGM を再生する。すべての音を1本の出力にミキシングするので、
/// 命中音が連続しても前の音が途切れず、BGM とも同時に鳴る。
/// </summary>
public sealed class NAudioSoundPlayer : ISoundPlayer
{
    private const int SampleRate = 44100;
    private const int LatencyMs = 80;

    /// <summary>マスター音量 100% のときの出力倍率。複数の音が重なっても割れにくいよう余裕を持たせる。</summary>
    private const float OutputGain = 0.8f;

    /// <summary>
    /// 同じ効果音を鳴らす最短間隔（秒）。
    /// 命中音などは1フレームに何発も重なるので、間引かないと音割れする。
    /// </summary>
    private static readonly Dictionary<SoundEffect, double> MinIntervals = new()
    {
        [SoundEffect.ArrowHit] = 0.045,
        [SoundEffect.CannonHit] = 0.08,
        [SoundEffect.FrostHit] = 0.06,
        [SoundEffect.BreakerHit] = 0.06,
        [SoundEffect.LightningHit] = 0.06,
        [SoundEffect.GoldGenerated] = 0.08,
        [SoundEffect.EnemyKilled] = 0.04,
        [SoundEffect.EnemyReachedGoal] = 0.15,
    };

    private readonly WaveOutEvent _output;
    private readonly MixingSampleProvider _mixer;
    private readonly Dictionary<SoundEffect, float[]> _sounds;
    private readonly Dictionary<SoundEffect, double> _lastPlayed = [];
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private readonly Dictionary<SoundCategory, float> _gains = [];
    private readonly MusicChannel _music;

    /// <summary>BGM は合成に少し時間がかかるので、起動時に裏で作り始めておく。</summary>
    private readonly Dictionary<MusicTrack, Task<float[]?>> _musicTracks;

    private volatile MusicTrack _requestedMusic = MusicTrack.None;

    private NAudioSoundPlayer()
    {
        var format = WaveFormat.CreateIeeeFloatWaveFormat(SampleRate, 1);
        _sounds = Enum.GetValues<SoundEffect>().ToDictionary(e => e, e => SoundLibrary.Create(e, SampleRate));
        _mixer = new MixingSampleProvider(format) { ReadFully = true };
        _music = new MusicChannel(format);
        _mixer.AddMixerInput(_music);
        _musicTracks = Enum.GetValues<MusicTrack>()
            .ToDictionary(t => t, t => Task.Run(() => MusicLibrary.Create(t, SampleRate)));

        var volume = new VolumeSampleProvider(_mixer) { Volume = OutputGain };
        _output = new WaveOutEvent { DesiredLatency = LatencyMs };
        _output.Init(new SampleToWaveProvider16(volume));
        _output.Play();
        Apply(new AudioSettings());
    }

    public bool IsAvailable => true;

    /// <summary>分類ごとの最終的な音量（ON/OFF × マスター × 分類）を計算しておく。次に鳴らす音から反映される。</summary>
    public void Apply(AudioSettings settings)
    {
        foreach (var category in SoundCategories.All)
        {
            _gains[category] = settings.EffectiveVolume(category);
        }

        _music.Volume = _gains[SoundCategory.Music];
    }

    /// <summary>音声デバイスを開く。デバイスが無いなど開けない場合は null を返す。</summary>
    public static NAudioSoundPlayer? TryCreate()
    {
        try
        {
            return new NAudioSoundPlayer();
        }
        catch (MmException)
        {
            return null;
        }
    }

    public void Play(SoundEffect effect)
    {
        float gain = _gains.GetValueOrDefault(SoundCategories.Of(effect));
        if (gain <= 0f)
        {
            return;
        }

        double now = _clock.Elapsed.TotalSeconds;
        if (MinIntervals.TryGetValue(effect, out double interval)
            && _lastPlayed.TryGetValue(effect, out double last)
            && now - last < interval)
        {
            return;
        }

        _lastPlayed[effect] = now;
        _mixer.AddMixerInput(new CachedSoundSampleProvider(_sounds[effect], _mixer.WaveFormat, gain));
    }

    public void PlayMusic(MusicTrack track)
    {
        _requestedMusic = track;
        _musicTracks[track].ContinueWith(
            task =>
            {
                // 合成を待っている間に別の曲へ切り替えられていたら何もしない
                if (task.IsCompletedSuccessfully && _requestedMusic == track)
                {
                    _music.Change(task.Result);
                }
            },
            TaskScheduler.Default);
    }

    public void Dispose()
    {
        _output.Stop();
        _output.Dispose();
    }
}
