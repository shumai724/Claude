namespace TowerDefenceGame.Views.Audio;

/// <summary>
/// BGM の楽譜（レシピ）。効果音と同じく音声ファイルは使わず、起動時に波形を合成する。
/// 旋律は8分音符単位の記号列で書く（"." で伸ばす、"-" で休符、"|" は小節の区切り）。
/// </summary>
public static class MusicLibrary
{
    private const int Eighth = 2;

    /// <summary>
    /// 曲ごとのピーク音量。効果音（命中音など）が埋もれないよう、効果音より控えめにする。
    /// </summary>
    private static readonly Dictionary<MusicTrack, float> Loudness = new()
    {
        [MusicTrack.StageSelect] = 0.26f,
        [MusicTrack.Battle] = 0.3f,
    };

    /// <summary>曲の波形を合成する。<see cref="MusicTrack.None"/> は null。</summary>
    public static float[]? Create(MusicTrack track, int sampleRate) => track switch
    {
        MusicTrack.None => null,
        MusicTrack.StageSelect => StageSelect(sampleRate),
        MusicTrack.Battle => Battle(sampleRate),
        _ => throw new ArgumentOutOfRangeException(nameof(track), track, null),
    };

    /// <summary>ステージ選択：ハ長調・BPM 96・8小節。ゆったりしたアルペジオに柔らかい旋律を乗せる。</summary>
    private static float[] StageSelect(int sampleRate)
    {
        const int bars = 8;
        var b = new MusicBuilder(sampleRate, 96, bars);

        //               C           G           Am          Em          F           C           Dm          G
        string[] bass = ["C2", "G2", "A2", "E2", "F2", "C2", "D2", "G2"];
        int[][] chords =
        [
            [60, 64, 67, 72], [55, 59, 62, 67], [57, 60, 64, 69], [52, 55, 59, 64],
            [53, 57, 60, 65], [60, 64, 67, 72], [50, 53, 57, 62], [55, 59, 62, 67],
        ];
        int[] arpeggio = [0, 1, 2, 3, 2, 1, 0, 1];

        for (int bar = 0; bar < bars; bar++)
        {
            int start = bar * MusicBuilder.StepsPerBar;
            int root = MusicBuilder.ParseNote(bass[bar]);

            // ベース：根音と5度を2分音符で
            b.Note(start, 8, root, Waveform.Triangle, 0.5f, 1.2f)
             .Note(start + 8, 8, root + 7, Waveform.Triangle, 0.42f, 1.2f);

            // 和音の分散（8分音符）
            for (int i = 0; i < arpeggio.Length; i++)
            {
                b.Note(start + i * Eighth, Eighth, chords[bar][arpeggio[i]], Waveform.Triangle, 0.16f, 3f);
            }

            // 軽い拍取り
            b.Kick(start, 0.18f).Kick(start + 8, 0.12f);
            for (int step = 2; step < MusicBuilder.StepsPerBar; step += 4)
            {
                b.HiHat(start + step, 0.04f);
            }
        }

        const string melody =
            "E5 . . . G5 . E5 D5 | D5 . . . B4 . . . | C5 . . . E5 . A5 G5 | G5 . . . . . - - |" +
            "A5 . . . G5 . F5 E5 | E5 . . . C5 . . . | D5 . . . F5 . E5 D5 | D5 . . . G4 . B4 .";

        // 主旋律＋少し遅れて小さく重ねたやまびこ
        b.Melody(0, melody, Eighth, Waveform.Square, 0.1f, 0.9f)
         .Melody(3, melody, Eighth, Waveform.Square, 0.035f, 0.9f);

        return b.Build(Loudness[MusicTrack.StageSelect]);
    }

    /// <summary>戦闘：イ短調・BPM 140・16小節。8分のベースとドラムで前へ進む感じを出す。</summary>
    private static float[] Battle(int sampleRate)
    {
        const int bars = 16;
        var b = new MusicBuilder(sampleRate, 140, bars);

        int[] am = [57, 60, 64, 69], f = [53, 57, 60, 65], g = [55, 59, 62, 67];
        int[] em = [52, 55, 59, 64], e = [52, 56, 59, 64], dm = [50, 53, 57, 62];

        // 前半 8 小節（Am F G Em / Am F G E）と後半 8 小節（Dm Em F G / Dm Em F E）
        int[][] chords = [am, f, g, em, am, f, g, e, dm, em, f, g, dm, em, f, e];
        string[] bass = ["A2", "F2", "G2", "E2", "A2", "F2", "G2", "E2", "D2", "E2", "F2", "G2", "D2", "E2", "F2", "E2"];

        for (int bar = 0; bar < bars; bar++)
        {
            int start = bar * MusicBuilder.StepsPerBar;
            int root = MusicBuilder.ParseNote(bass[bar]);
            bool fillBar = bar % 8 == 7;

            // ベース：根音とオクターブ上を8分で刻む
            for (int i = 0; i < 8; i++)
            {
                b.Note(start + i * Eighth, Eighth, i % 2 == 0 ? root : root + 12, Waveform.Triangle, 0.5f, 2f);
            }

            // 和音の分散（16分音符・小さめ）
            for (int step = 0; step < MusicBuilder.StepsPerBar; step++)
            {
                b.Note(start + step, 1, chords[bar][step % 4] + 12, Waveform.Triangle, 0.06f, 4f);
            }

            // ドラム：4つ打ち寄りのキック、2・4拍のスネア、8分のハイハット。8小節ごとにフィルイン
            b.Kick(start, 0.42f).Kick(start + 8, 0.42f).Kick(start + 10, 0.3f);
            b.Snare(start + 4, 0.2f);
            if (fillBar)
            {
                b.Snare(start + 12, 0.2f).Snare(start + 13, 0.14f).Snare(start + 14, 0.17f).Snare(start + 15, 0.2f);
            }
            else
            {
                b.Snare(start + 12, 0.2f);
            }

            for (int step = 0; step < MusicBuilder.StepsPerBar; step += 2)
            {
                b.HiHat(start + step, step % 4 == 2 ? 0.07f : 0.035f);
            }
        }

        const string melody =
            // 前半
            "A4 . C5 . E5 . D5 C5 | C5 . A4 . F4 . A4 C5 | D5 . B4 . G4 . B4 D5 | E5 . . . B4 . . . |" +
            "A4 . C5 . E5 . A5 G5 | F5 . E5 . C5 . A4 C5 | B4 . D5 . G5 . F5 D5 | E5 . . . G#4 . B4 . |" +
            // 後半
            "F5 . . E5 D5 . A4 . | G5 . . E5 B4 . . . | A5 . G5 F5 E5 . C5 . | D5 . . . G5 . . . |" +
            "F5 . E5 D5 A4 . D5 . | G5 . E5 . B4 . E5 . | C5 . F5 . A5 . G5 F5 | E5 . . . G#5 . B5 .";

        b.Melody(0, melody, Eighth, Waveform.Square, 0.12f, 1.2f)
         .Melody(3, melody, Eighth, Waveform.Square, 0.04f, 1.2f);

        return b.Build(Loudness[MusicTrack.Battle]);
    }
}
