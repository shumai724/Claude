namespace TowerDefenceGame.Models.Settings;

/// <summary>BGM・効果音の設定。音量は 0（無音）〜1（最大）。</summary>
public sealed class AudioSettings
{
    public const float DefaultMasterVolume = 0.75f;
    public const float DefaultCategoryVolume = 1f;

    public bool Enabled { get; set; } = true;

    public float MasterVolume { get; set; } = DefaultMasterVolume;

    /// <summary>分類ごとの音量。記録の無い分類は <see cref="DefaultCategoryVolume"/> として扱う。</summary>
    public Dictionary<SoundCategory, float> CategoryVolumes { get; set; } = [];

    public float GetCategoryVolume(SoundCategory category) =>
        CategoryVolumes.TryGetValue(category, out float volume) ? Math.Clamp(volume, 0f, 1f) : DefaultCategoryVolume;

    public void SetCategoryVolume(SoundCategory category, float volume) =>
        CategoryVolumes[category] = Math.Clamp(volume, 0f, 1f);

    /// <summary>実際に鳴らすときの音量（マスター × 分類）。無効なら 0。</summary>
    public float EffectiveVolume(SoundCategory category) =>
        Enabled ? Math.Clamp(MasterVolume, 0f, 1f) * GetCategoryVolume(category) : 0f;

    public AudioSettings Clone() => new()
    {
        Enabled = Enabled,
        MasterVolume = MasterVolume,
        CategoryVolumes = new Dictionary<SoundCategory, float>(CategoryVolumes),
    };

    /// <summary>ファイルを手で書き換えた場合などに備えて、範囲外の値を丸める。</summary>
    public void Normalize()
    {
        MasterVolume = Math.Clamp(MasterVolume, 0f, 1f);
        foreach (var category in CategoryVolumes.Keys.ToList())
        {
            SetCategoryVolume(category, CategoryVolumes[category]);
        }
    }
}
