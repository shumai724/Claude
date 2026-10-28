using TowerDefenceGame.Models.Settings;
using TowerDefenceGame.Views.Audio;
using TowerDefenceGame.Views.Forms;

namespace TowerDefenceGame.Controllers;

/// <summary>システム設定の読み込み・保存と、設定画面の表示を担当する。</summary>
public sealed class SettingsController
{
    private readonly ISoundPlayer _player;
    private readonly SettingsStore _store;
    private readonly GameSettings _settings;

    public SettingsController(ISoundPlayer player, SettingsStore store)
    {
        _player = player;
        _store = store;
        _settings = store.Load();
        _player.Apply(_settings.Audio);
    }

    public bool SoundEnabled => _settings.Audio.Enabled;

    /// <summary>設定が確定して変わったときに発生する。</summary>
    public event Action? Changed;

    /// <summary>設定画面を開く。編集中の変更はすぐ音に反映し、キャンセルなら元に戻す。</summary>
    public void ShowDialog(IWin32Window owner)
    {
        using var dialog = new SettingsDialog(_settings.Audio, _player.IsAvailable);
        dialog.PreviewChanged += _player.Apply;
        dialog.SampleRequested += _player.Play;

        if (dialog.ShowDialog(owner) == DialogResult.OK)
        {
            _settings.Audio = dialog.Settings;
            _store.Save(_settings);
        }

        _player.Apply(_settings.Audio);
        Changed?.Invoke();
    }

    /// <summary>BGM・効果音の ON/OFF を切り替えて保存する（M キー）。</summary>
    public void ToggleSound()
    {
        _settings.Audio.Enabled = !_settings.Audio.Enabled;
        _player.Apply(_settings.Audio);
        _store.Save(_settings);
        if (_settings.Audio.Enabled)
        {
            _player.Play(SoundEffect.Select);
        }

        Changed?.Invoke();
    }
}
