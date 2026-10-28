namespace TowerDefenceGame.Views.Audio;

/// <summary>音色（トーン・ノイズ）を時間軸上に重ねてモノラルの波形を組み立てる。</summary>
public sealed class SoundBuilder
{
    private const float AttackSeconds = 0.005f;

    private readonly int _sampleRate;
    private readonly Random _random = new(12345);
    private float[] _buffer = [];

    public SoundBuilder(int sampleRate)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sampleRate);
        _sampleRate = sampleRate;
    }

    /// <summary>周波数を fromHz から toHz へ指数的に変化させるトーンを重ねる。</summary>
    /// <param name="start">開始時刻（秒）。</param>
    /// <param name="duration">長さ（秒）。</param>
    /// <param name="decay">減衰の速さ。大きいほど早く消える。</param>
    public SoundBuilder Tone(float start, float duration, float fromHz, float toHz, Waveform waveform, float volume, float decay = 4f)
    {
        int offset = ToSamples(start);
        int length = ToSamples(duration);
        EnsureLength(offset + length);

        double phase = 0;
        double ratio = toHz / fromHz;
        for (int i = 0; i < length; i++)
        {
            double t = (double)i / length;
            phase += fromHz * Math.Pow(ratio, t) / _sampleRate;
            _buffer[offset + i] += Oscillate(waveform, phase) * Envelope(i, length, decay) * volume;
        }

        return this;
    }

    /// <summary>ノイズを重ねる。</summary>
    /// <param name="brightness">0〜1。小さいほど高音を削った「ドーン」に、大きいほど「シャッ」になる。</param>
    public SoundBuilder Noise(float start, float duration, float volume, float brightness, float decay = 4f)
    {
        int offset = ToSamples(start);
        int length = ToSamples(duration);
        EnsureLength(offset + length);

        float filtered = 0f;
        float alpha = Math.Clamp(brightness, 0.01f, 1f);
        for (int i = 0; i < length; i++)
        {
            float white = (float)(_random.NextDouble() * 2 - 1);
            filtered += alpha * (white - filtered);
            _buffer[offset + i] += filtered * Envelope(i, length, decay) * volume;
        }

        return this;
    }

    /// <summary>音程を順番に鳴らす（アルペジオ）。最後の音だけ長く伸ばす。</summary>
    public SoundBuilder Arpeggio(float start, float step, float[] frequencies, Waveform waveform, float volume, float lastDuration)
    {
        for (int i = 0; i < frequencies.Length; i++)
        {
            bool isLast = i == frequencies.Length - 1;
            Tone(start + step * i, isLast ? lastDuration : step * 1.5f, frequencies[i], frequencies[i], waveform, volume, isLast ? 2.5f : 3f);
        }

        return this;
    }

    /// <summary>ピーク音量が targetPeak になるよう正規化して波形を返す。</summary>
    /// <param name="targetPeak">0〜1。効果音どうしの音量バランスはこの値で決める。</param>
    public float[] Build(float targetPeak)
    {
        float peak = 0f;
        foreach (float sample in _buffer)
        {
            peak = MathF.Max(peak, MathF.Abs(sample));
        }

        float gain = peak > 0f ? Math.Clamp(targetPeak, 0f, 1f) / peak : 0f;
        var result = new float[_buffer.Length];
        for (int i = 0; i < result.Length; i++)
        {
            result[i] = _buffer[i] * gain;
        }

        return result;
    }

    /// <summary>位相（周期数）から波形の値（-1〜1 付近）を返す。BGM の合成でも使う。</summary>
    internal static float Oscillate(Waveform waveform, double phase)
    {
        float frac = (float)(phase - Math.Floor(phase));
        return waveform switch
        {
            Waveform.Square => frac < 0.5f ? 0.6f : -0.6f,
            Waveform.Triangle => 4f * MathF.Abs(frac - 0.5f) - 1f,
            Waveform.Saw => (2f * frac - 1f) * 0.7f,
            _ => MathF.Sin(2f * MathF.PI * frac),
        };
    }

    /// <summary>立ち上がりのプチノイズを防ぐ短いアタックの後、指数的に減衰して最後に 0 になる包絡線。</summary>
    private float Envelope(int index, int length, float decay)
    {
        int attack = Math.Max(1, Math.Min(length / 4, ToSamples(AttackSeconds)));
        float attackGain = index < attack ? (float)index / attack : 1f;
        float t = (float)index / length;
        return attackGain * MathF.Exp(-decay * t) * (1f - t);
    }

    private int ToSamples(float seconds) => (int)(seconds * _sampleRate);

    private void EnsureLength(int length)
    {
        if (_buffer.Length < length)
        {
            Array.Resize(ref _buffer, length);
        }
    }
}
