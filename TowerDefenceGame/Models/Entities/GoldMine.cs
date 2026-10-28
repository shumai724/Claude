using System.Numerics;
using TowerDefenceGame.Models.Core;
using TowerDefenceGame.Models.Data;
using TowerDefenceGame.Models.Map;

namespace TowerDefenceGame.Models.Entities;

/// <summary>
/// 一定時間ごとにゴールドを生み出す建物。
/// ウェーブを始めずに待って稼げないよう、ウェーブ進行中だけ時間が進む。
/// <list type="bullet">
/// <item>産出量 Lv5「大鉱脈」: ウェーブを凌ぐたびに、産出量の3倍を追加で得る</item>
/// <item>産出速度 Lv5「採掘ラッシュ」: ウェーブ開始から一定時間、産出間隔が半分になる</item>
/// </list>
/// </summary>
public sealed class GoldMine : Tower
{
    public const int MotherLodeMultiplier = 3;
    public const float RushDuration = 15f;

    private static readonly UpgradeStat[] Stats = [UpgradeStat.GoldAmount, UpgradeStat.GoldSpeed];

    private const float AmountGrowthPerLevel = 0.4f;
    private const float IntervalFactorPerLevel = 0.88f;
    private const float PopupDuration = 1.0f;

    private float _elapsed;
    private float _waveElapsed;
    private bool _wasWaveInProgress;

    public GoldMine(TowerSpec spec, GridPos cell, Vector2 position)
        : base(spec, cell, position, Stats)
    {
    }

    public int GoldAmount => (int)MathF.Round(Spec.GoldAmount * StatScale(UpgradeStat.GoldAmount, AmountGrowthPerLevel));

    public float GoldInterval => Spec.GoldInterval * StatPower(UpgradeStat.GoldSpeed, IntervalFactorPerLevel);

    /// <summary>今の産出間隔。採掘ラッシュ中は半分になる。</summary>
    public float CurrentInterval => IsRushing ? GoldInterval / 2f : GoldInterval;

    public bool IsRushing => IsMastered(UpgradeStat.GoldSpeed) && _wasWaveInProgress && _waveElapsed < RushDuration;

    /// <summary>次の産出までの進み具合（0〜1）。</summary>
    public float Progress => Math.Clamp(_elapsed / CurrentInterval, 0f, 1f);

    public int LastPayout { get; private set; }

    /// <summary>「+○G」の表示を出しておく残り時間（秒）。</summary>
    public float PopupTime { get; private set; }

    public float PopupRatio => PopupTime / PopupDuration;

    public override void Update(float deltaTime, World world)
    {
        PopupTime = MathF.Max(0f, PopupTime - deltaTime);

        bool inWave = world.IsWaveInProgress;
        if (inWave && !_wasWaveInProgress)
        {
            _waveElapsed = 0f;
        }
        else if (!inWave && _wasWaveInProgress && IsMastered(UpgradeStat.GoldAmount))
        {
            Payout(world, GoldAmount * MotherLodeMultiplier);
        }

        _wasWaveInProgress = inWave;
        if (!inWave)
        {
            return;
        }

        _waveElapsed += deltaTime;
        _elapsed += deltaTime;
        if (_elapsed >= CurrentInterval)
        {
            _elapsed -= CurrentInterval;
            Payout(world, GoldAmount);
        }
    }

    private void Payout(World world, int amount)
    {
        LastPayout = amount;
        PopupTime = PopupDuration;
        world.NotifyGoldGenerated(this, amount);
    }
}
