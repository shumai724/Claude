namespace TowerDefenceGame.Views.Audio;

/// <summary>各効果音の音作り（レシピ）。音声ファイルを使わず、起動時に波形を合成する。</summary>
public static class SoundLibrary
{
    // 音名と周波数（Hz）
    private const float C5 = 523.25f, E5 = 659.25f, G5 = 783.99f, B5 = 987.77f;
    private const float C6 = 1046.50f, D6 = 1174.66f, E6 = 1318.51f, G6 = 1567.98f, B6 = 1975.53f;
    private const float G4 = 392.00f, F4 = 349.23f, Eb4 = 311.13f, C4 = 261.63f, G3 = 196.00f;

    /// <summary>
    /// 効果音ごとのピーク音量。頻繁に鳴る命中音は控えめに、
    /// 操作の手応えや進行の節目になる音ははっきり聞こえるようにする。
    /// </summary>
    private static readonly Dictionary<SoundEffect, float> Loudness = new()
    {
        [SoundEffect.Select] = 0.35f,
        [SoundEffect.Cancel] = 0.3f,
        [SoundEffect.Rejected] = 0.4f,
        [SoundEffect.TowerPlaced] = 0.7f,
        [SoundEffect.TowerUpgraded] = 0.45f,
        [SoundEffect.TowerMastered] = 0.55f,
        [SoundEffect.TowerSold] = 0.45f,
        [SoundEffect.ArrowHit] = 0.22f,
        [SoundEffect.CannonHit] = 0.6f,
        [SoundEffect.FrostHit] = 0.28f,
        [SoundEffect.BreakerHit] = 0.3f,
        [SoundEffect.LightningHit] = 0.32f,
        [SoundEffect.GoldGenerated] = 0.3f,
        [SoundEffect.EnemyKilled] = 0.35f,
        [SoundEffect.EnemyReachedGoal] = 0.6f,
        [SoundEffect.WaveStarted] = 0.55f,
        [SoundEffect.WaveCompleted] = 0.5f,
        [SoundEffect.GameOver] = 0.55f,
        [SoundEffect.StageClear] = 0.6f,
    };

    public static float[] Create(SoundEffect effect, int sampleRate)
    {
        var b = new SoundBuilder(sampleRate);
        switch (effect)
        {
            case SoundEffect.Select:
                // 短い「ピッ」
                b.Tone(0, 0.04f, 1200, 1500, Waveform.Triangle, 0.25f, 3);
                break;

            case SoundEffect.Cancel:
                // 下がる「ポッ」
                b.Tone(0, 0.06f, 900, 550, Waveform.Triangle, 0.22f, 3);
                break;

            case SoundEffect.Rejected:
                // 低い「ブブッ」
                b.Tone(0, 0.08f, 150, 140, Waveform.Square, 0.22f, 1)
                 .Tone(0.1f, 0.1f, 150, 130, Waveform.Square, 0.22f, 1);
                break;

            case SoundEffect.TowerPlaced:
                // 「ドンッ」と置いた衝撃＋「キン」と完成した金属音
                b.Tone(0, 0.16f, 220, 70, Waveform.Sine, 0.75f, 5)
                 .Noise(0, 0.06f, 0.25f, 0.3f, 8)
                 .Tone(0.04f, 0.14f, E6, E6, Waveform.Triangle, 0.16f, 6)
                 .Tone(0.04f, 0.14f, B6, B6, Waveform.Triangle, 0.1f, 6);
                break;

            case SoundEffect.TowerUpgraded:
                // 上昇するアルペジオ
                b.Arpeggio(0, 0.06f, [C5, E5, G5, C6], Waveform.Square, 0.17f, 0.3f);
                break;

            case SoundEffect.TowerMastered:
                // 上昇アルペジオの後にきらめく和音（Lv5 到達）
                b.Arpeggio(0, 0.05f, [C5, E5, G5, C6, E6], Waveform.Square, 0.16f, 0.2f)
                 .Tone(0.25f, 0.6f, C6, C6, Waveform.Triangle, 0.2f, 2.5f)
                 .Tone(0.25f, 0.6f, G6, G6, Waveform.Triangle, 0.15f, 2.5f)
                 .Tone(0.3f, 0.4f, 2600, 3400, Waveform.Sine, 0.08f, 4);
                break;

            case SoundEffect.TowerSold:
                // コインの「チャリーン」
                b.Tone(0, 0.06f, B5, B5, Waveform.Square, 0.2f, 1)
                 .Tone(0.06f, 0.32f, E6, E6, Waveform.Square, 0.2f, 5);
                break;

            case SoundEffect.ArrowHit:
                // 軽い「トッ」
                b.Tone(0, 0.05f, 900, 380, Waveform.Triangle, 0.28f, 6)
                 .Noise(0, 0.03f, 0.12f, 0.6f, 10);
                break;

            case SoundEffect.CannonHit:
                // 低い爆発音「ドーン」
                b.Noise(0, 0.42f, 0.6f, 0.07f, 5)
                 .Tone(0, 0.3f, 110, 40, Waveform.Sine, 0.6f, 4);
                break;

            case SoundEffect.FrostHit:
                // 氷の「キラン」
                b.Tone(0, 0.13f, 2400, 3200, Waveform.Sine, 0.16f, 5)
                 .Tone(0.02f, 0.15f, 3600, 4300, Waveform.Sine, 0.09f, 6)
                 .Noise(0, 0.05f, 0.06f, 0.9f, 10);
                break;

            case SoundEffect.BreakerHit:
                // 装甲が砕ける「ガキッ」
                b.Tone(0, 0.08f, 320, 140, Waveform.Square, 0.25f, 5)
                 .Noise(0, 0.07f, 0.2f, 0.75f, 7);
                break;

            case SoundEffect.LightningHit:
                // 電撃の「バチッ」
                b.Tone(0, 0.13f, 1900, 280, Waveform.Saw, 0.25f, 5)
                 .Noise(0, 0.11f, 0.25f, 0.95f, 6);
                break;

            case SoundEffect.GoldGenerated:
                // 小さなコインの「チャリン」
                b.Tone(0, 0.04f, E6, E6, Waveform.Square, 0.2f, 1)
                 .Tone(0.04f, 0.14f, B6, B6, Waveform.Square, 0.2f, 5);
                break;

            case SoundEffect.EnemyKilled:
                // はじける「ポンッ」
                b.Tone(0, 0.1f, 480, 1150, Waveform.Square, 0.17f, 4)
                 .Noise(0, 0.07f, 0.16f, 0.35f, 8);
                break;

            case SoundEffect.EnemyReachedGoal:
                // 被ダメージの警告音（わずかにずらした2音でうなりを出す）
                b.Tone(0, 0.28f, 330, 165, Waveform.Saw, 0.32f, 2)
                 .Tone(0, 0.28f, 334, 167, Waveform.Saw, 0.22f, 2);
                break;

            case SoundEffect.WaveStarted:
                // 開戦のホルン「パッパー」
                b.Tone(0, 0.12f, G4, G4, Waveform.Saw, 0.25f, 1.5f)
                 .Tone(0, 0.12f, G4 / 2, G4 / 2, Waveform.Square, 0.1f, 1.5f)
                 .Tone(0.14f, 0.34f, C5, C5, Waveform.Saw, 0.28f, 2)
                 .Tone(0.14f, 0.34f, C5 / 2, C5 / 2, Waveform.Square, 0.12f, 2);
                break;

            case SoundEffect.WaveCompleted:
                // 凌いだときの明るいアルペジオ
                b.Arpeggio(0, 0.07f, [G5, B5, D6, G6], Waveform.Triangle, 0.3f, 0.4f);
                break;

            case SoundEffect.GameOver:
                // 下降する重い旋律
                b.Arpeggio(0, 0.22f, [G4, F4, Eb4, C4, G3], Waveform.Saw, 0.28f, 0.9f);
                break;

            case SoundEffect.StageClear:
                // ファンファーレ＋和音
                b.Arpeggio(0, 0.12f, [C5, E5, G5], Waveform.Square, 0.2f, 0.16f)
                 .Tone(0.38f, 1.0f, C6, C6, Waveform.Triangle, 0.22f, 2)
                 .Tone(0.38f, 1.0f, E6, E6, Waveform.Triangle, 0.18f, 2)
                 .Tone(0.38f, 1.0f, G6, G6, Waveform.Triangle, 0.15f, 2);
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(effect), effect, null);
        }

        return b.Build(Loudness[effect]);
    }
}
