namespace TowerDefenceGame.Models.Core;

/// <summary>プレイヤーの所持金・ライフ・スコアを管理する。</summary>
public sealed class Player
{
    public Player(int initialGold, int initialLives)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(initialGold);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(initialLives);
        Gold = initialGold;
        Lives = initialLives;
        InitialLives = initialLives;
    }

    public int Gold { get; private set; }

    public int Lives { get; private set; }

    /// <summary>ステージ開始時のライフ。</summary>
    public int InitialLives { get; }

    public int Score { get; private set; }

    public bool IsDead => Lives <= 0;

    /// <summary>所持金・ライフ・スコアのいずれかが変化したときに発生する。</summary>
    public event Action? Changed;

    public bool CanAfford(int cost) => Gold >= cost;

    public bool TrySpend(int amount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(amount);
        if (!CanAfford(amount))
        {
            return false;
        }

        Gold -= amount;
        Changed?.Invoke();
        return true;
    }

    public void Earn(int amount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(amount);
        if (amount == 0)
        {
            return;
        }

        Gold += amount;
        Changed?.Invoke();
    }

    public void AddScore(int points)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(points);
        if (points == 0)
        {
            return;
        }

        Score += points;
        Changed?.Invoke();
    }

    public void LoseLife(int amount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(amount);
        if (amount == 0 || IsDead)
        {
            return;
        }

        Lives = Math.Max(0, Lives - amount);
        Changed?.Invoke();
    }
}
