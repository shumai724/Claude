using System.Text.Json;
using TowerDefenceGame.Models.Data;

namespace TowerDefenceGame.Models.Settings;

/// <summary>システム設定をファイルに保存・読み込みする。</summary>
public sealed class SettingsStore
{
    private readonly string _filePath;

    public SettingsStore(string filePath)
    {
        _filePath = filePath;
    }

    public static string DefaultPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TowerDefenceGame", "settings.json");

    /// <summary>設定を読み込む。ファイルが無い・壊れている場合は初期設定を返す。</summary>
    public GameSettings Load()
    {
        if (!File.Exists(_filePath))
        {
            return new GameSettings();
        }

        try
        {
            var settings = JsonFile.Read<GameSettings>(_filePath);
            settings.Audio ??= new AudioSettings();
            settings.Audio.CategoryVolumes ??= [];
            settings.Audio.Normalize();
            return settings;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or InvalidDataException)
        {
            return new GameSettings();
        }
    }

    /// <summary>設定を保存する。保存できなくてもゲームは続けられるので、失敗したら false を返すだけにする。</summary>
    public bool Save(GameSettings settings)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
            JsonFile.Write(_filePath, settings);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }
}
