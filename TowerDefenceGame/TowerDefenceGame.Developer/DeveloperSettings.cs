using System.Text.Json;

namespace TowerDefenceGame.Developer;

/// <summary>開発者ウィンドウの設定。</summary>
internal sealed class DeveloperSettings
{
    public const float MinEnemyCountMultiplier = 0.1f;
    public const float MaxEnemyCountMultiplier = 10f;

    /// <summary>ウェーブの敵の数に掛ける倍率。</summary>
    public float EnemyCountMultiplier { get; set; } = 1f;

    /// <summary>クリア状況に関係なく、すべてのステージ・レベルを選べるようにするか。</summary>
    public bool UnlockAllLevels { get; set; }

    /// <summary>ファイルを手で書き換えた場合などに備えて、範囲外の値を丸める。</summary>
    public void Normalize()
    {
        EnemyCountMultiplier = float.IsFinite(EnemyCountMultiplier)
            ? Math.Clamp(EnemyCountMultiplier, MinEnemyCountMultiplier, MaxEnemyCountMultiplier)
            : 1f;
    }
}

/// <summary>開発者ウィンドウの設定をファイルに保存・読み込みする。</summary>
internal sealed class DeveloperSettingsStore
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    private readonly string _filePath;

    public DeveloperSettingsStore(string filePath)
    {
        _filePath = filePath;
    }

    /// <summary>設定を読み込む。ファイルが無い・壊れている場合は初期設定を返す。</summary>
    public DeveloperSettings Load()
    {
        try
        {
            if (File.Exists(_filePath))
            {
                var settings = JsonSerializer.Deserialize<DeveloperSettings>(File.ReadAllText(_filePath), Options) ?? new DeveloperSettings();
                settings.Normalize();
                return settings;
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
        }

        return new DeveloperSettings();
    }

    /// <summary>設定を保存する。保存できなくても開発の邪魔はしない。</summary>
    public void Save(DeveloperSettings settings)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
            File.WriteAllText(_filePath, JsonSerializer.Serialize(settings, Options));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
        }
    }
}
