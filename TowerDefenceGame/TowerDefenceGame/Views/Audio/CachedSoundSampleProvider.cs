using NAudio.Wave;

namespace TowerDefenceGame.Views.Audio;

/// <summary>合成済みの波形を、指定した音量で先頭から1回だけ再生する。</summary>
internal sealed class CachedSoundSampleProvider : ISampleProvider
{
    private readonly float[] _samples;
    private readonly float _gain;
    private int _position;

    public CachedSoundSampleProvider(float[] samples, WaveFormat waveFormat, float gain)
    {
        _samples = samples;
        _gain = gain;
        WaveFormat = waveFormat;
    }

    public WaveFormat WaveFormat { get; }

    public int Read(float[] buffer, int offset, int count)
    {
        // 要求より少なく返すと、ミキサーはこの入力を終了したものとして取り除く
        int read = Math.Min(count, _samples.Length - _position);
        for (int i = 0; i < read; i++)
        {
            buffer[offset + i] = _samples[_position + i] * _gain;
        }

        _position += read;
        return read;
    }
}
