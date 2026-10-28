namespace TowerDefenceGame.Views.Audio;

/// <summary>
/// 拍と小節を単位に音符・ドラムを並べて、ループ再生用の BGM 波形を組み立てる。
/// 曲の長さは最初に決め、末尾からはみ出した余韻は先頭に回り込ませるので、つなぎ目で途切れない。
/// </summary>
public sealed class MusicBuilder
{
    /// <summary>1拍を何ステップに分けるか（4 = 16分音符単位）。</summary>
    public const int StepsPerBeat = 4;

    public const int StepsPerBar = StepsPerBeat * 4;

    private const float AttackSeconds = 0.005f;
    private const float ReleaseSeconds = 0.015f;

    private readonly int _sampleRate;
    private readonly double _samplesPerStep;
    private readonly float[] _buffer;
    private readonly Random _random = new(2468);

    /// <param name="bpm">テンポ（1分あたりの拍数）。</param>
    /// <param name="bars">ループ1周の小節数（4/4拍子）。</param>
    public MusicBuilder(int sampleRate, float bpm, int bars)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sampleRate);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(bpm);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(bars);

        _sampleRate = sampleRate;
        _samplesPerStep = sampleRate * 60.0 / bpm / StepsPerBeat;
        _buffer = new float[(int)Math.Round(_samplesPerStep * StepsPerBar * bars)];
    }

    /// <summary>MIDI ノート番号（A4 = 69）を周波数（Hz）に変換する。</summary>
    public static float Frequency(int midiNote) => 440f * MathF.Pow(2f, (midiNote - 69) / 12f);

    /// <summary>音符を1つ重ねる。</summary>
    /// <param name="step">開始位置（ステップ）。</param>
    /// <param name="steps">長さ（ステップ）。</param>
    /// <param name="decay">減衰の速さ。0 なら伸ばしっぱなし、大きいほど早く消える。</param>
    public MusicBuilder Note(int step, int steps, int midiNote, Waveform waveform, float volume, float decay = 1.5f)
    {
        int start = ToSamples(step);
        int length = ToSamples(step + steps) - start;
        float hz = Frequency(midiNote);
        int attack = Math.Max(1, Math.Min(length / 4, (int)(AttackSeconds * _sampleRate)));
        int release = Math.Max(1, Math.Min(length / 4, (int)(ReleaseSeconds * _sampleRate)));

        double phase = 0;
        for (int i = 0; i < length; i++)
        {
            phase += hz / _sampleRate;
            float t = (float)i / length;
            float env = MathF.Exp(-decay * t);
            if (i < attack)
            {
                env *= (float)i / attack;
            }
            else if (i >= length - release)
            {
                env *= (float)(length - i) / release;
            }

            Add(start + i, SoundBuilder.Oscillate(waveform, phase) * env * volume);
        }

        return this;
    }

    /// <summary>旋律を並べる。1つの記号が1ステップ分になる。</summary>
    /// <param name="startStep">旋律の開始位置（ステップ）。</param>
    /// <param name="notation">
    /// 空白区切りの記号列。"C5" "F#4" "Bb3" などの音名で発音、"." で直前の音を伸ばす、"-" で休符。
    /// 小節の区切りとして "|" を入れてもよい（空白として扱う）。
    /// </param>
    /// <param name="stepsPerSymbol">記号1つあたりのステップ数（2 なら8分音符単位で書ける）。</param>
    public MusicBuilder Melody(int startStep, string notation, int stepsPerSymbol, Waveform waveform, float volume, float decay = 1.5f)
    {
        int step = startStep;
        int? note = null;
        int noteStart = 0;
        int noteLength = 0;

        void Flush()
        {
            if (note is int n)
            {
                Note(noteStart, noteLength, n, waveform, volume, decay);
            }

            note = null;
        }

        // "|" は見やすさのための区切りなので、空白と同じ扱いにする（"|A4" のようにくっついていても読める）
        foreach (string symbol in notation.Replace('|', ' ').Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
        {
            switch (symbol)
            {
                case ".":
                    if (note is not null)
                    {
                        noteLength += stepsPerSymbol;
                    }

                    break;
                case "-":
                    Flush();
                    break;
                default:
                    Flush();
                    note = ParseNote(symbol);
                    noteStart = step;
                    noteLength = stepsPerSymbol;
                    break;
            }

            step += stepsPerSymbol;
        }

        Flush();
        return this;
    }

    /// <summary>バスドラム「ドッ」。</summary>
    public MusicBuilder Kick(int step, float volume)
    {
        int start = ToSamples(step);
        int length = (int)(0.16f * _sampleRate);
        double phase = 0;
        for (int i = 0; i < length; i++)
        {
            float t = (float)i / length;
            phase += (150 * MathF.Pow(45f / 150f, t)) / _sampleRate;
            float env = MathF.Exp(-5f * t) * (1f - t);
            Add(start + i, SoundBuilder.Oscillate(Waveform.Sine, phase) * env * volume);
        }

        return this;
    }

    /// <summary>スネア「タッ」。</summary>
    public MusicBuilder Snare(int step, float volume) => Noise(step, 0.12f, volume, 0.55f, 6f);

    /// <summary>ハイハット「チッ」。</summary>
    public MusicBuilder HiHat(int step, float volume) => Noise(step, 0.035f, volume, 0.95f, 8f);

    /// <summary>ピーク音量が targetPeak になるよう正規化して波形を返す。</summary>
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

    /// <summary>"C4" "F#3" "Bb5" のような音名を MIDI ノート番号に変換する。</summary>
    public static int ParseNote(string name)
    {
        if (name.Length < 2)
        {
            throw new FormatException($"音名として読めません: {name}");
        }

        int semitone = char.ToUpperInvariant(name[0]) switch
        {
            'C' => 0,
            'D' => 2,
            'E' => 4,
            'F' => 5,
            'G' => 7,
            'A' => 9,
            'B' => 11,
            _ => throw new FormatException($"音名として読めません: {name}"),
        };

        int index = 1;
        if (name[index] == '#')
        {
            semitone++;
            index++;
        }
        else if (name[index] == 'b')
        {
            semitone--;
            index++;
        }

        if (!int.TryParse(name.AsSpan(index), out int octave))
        {
            throw new FormatException($"音名として読めません: {name}");
        }

        return (octave + 1) * 12 + semitone;
    }

    private MusicBuilder Noise(int step, float seconds, float volume, float brightness, float decay)
    {
        int start = ToSamples(step);
        int length = (int)(seconds * _sampleRate);
        float filtered = 0f;
        float alpha = Math.Clamp(brightness, 0.01f, 1f);
        for (int i = 0; i < length; i++)
        {
            float white = (float)(_random.NextDouble() * 2 - 1);
            filtered += alpha * (white - filtered);
            float t = (float)i / length;
            Add(start + i, filtered * MathF.Exp(-decay * t) * (1f - t) * volume);
        }

        return this;
    }

    private int ToSamples(int step) => (int)Math.Round(step * _samplesPerStep);

    /// <summary>ループの末尾を越えた分は先頭に回り込ませる。</summary>
    private void Add(int index, float value) => _buffer[index % _buffer.Length] += value;
}
