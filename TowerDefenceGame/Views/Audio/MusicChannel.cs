using NAudio.Wave;

namespace TowerDefenceGame.Views.Audio;

/// <summary>
/// BGM を1曲ずつループ再生するミキサー入力。曲の切り替えはフェードアウト→フェードインで行う。
/// ミキサーから外れないよう、曲が無いときも無音を返し続ける。
/// </summary>
internal sealed class MusicChannel : ISampleProvider
{
    private const float FadeSeconds = 0.6f;

    /// <summary>音量変更を1サンプルごとに少しずつ追従させる係数（約 50ms）。急に変えるとプチノイズが出る。</summary>
    private const float VolumeSmoothing = 0.0005f;

    private readonly object _lock = new();
    private readonly float _fadeStep;
    private float[]? _current;
    private float[]? _next;
    private int _position;
    private float _fade;
    private bool _switching;
    private float _targetVolume;
    private float _volume;

    public MusicChannel(WaveFormat waveFormat)
    {
        WaveFormat = waveFormat;
        _fadeStep = 1f / (FadeSeconds * waveFormat.SampleRate);
    }

    public WaveFormat WaveFormat { get; }

    /// <summary>BGM の音量（ON/OFF × マスター × BGM）。</summary>
    public float Volume
    {
        set
        {
            lock (_lock)
            {
                _targetVolume = Math.Clamp(value, 0f, 1f);
            }
        }
    }

    /// <summary>次に流す曲を指定する。null なら止める。同じ曲を指定した場合は流し続ける。</summary>
    public void Change(float[]? samples)
    {
        lock (_lock)
        {
            if (samples == _current)
            {
                // 切り替え中に元の曲へ戻された場合は、フェードアウトを取りやめる
                _switching = false;
                _next = null;
                return;
            }

            if (_current is null)
            {
                _current = samples;
                _position = 0;
                _fade = 0f;
                return;
            }

            _next = samples;
            _switching = true;
        }
    }

    public int Read(float[] buffer, int offset, int count)
    {
        lock (_lock)
        {
            for (int i = 0; i < count; i++)
            {
                _volume += (_targetVolume - _volume) * VolumeSmoothing;

                if (_switching)
                {
                    _fade -= _fadeStep;
                    if (_fade <= 0f)
                    {
                        _fade = 0f;
                        _current = _next;
                        _next = null;
                        _position = 0;
                        _switching = false;
                    }
                }
                else if (_fade < 1f)
                {
                    _fade = MathF.Min(1f, _fade + _fadeStep);
                }

                if (_current is null || _current.Length == 0)
                {
                    buffer[offset + i] = 0f;
                    continue;
                }

                buffer[offset + i] = _current[_position] * _fade * _volume;
                _position = (_position + 1) % _current.Length;
            }
        }

        return count;
    }
}
