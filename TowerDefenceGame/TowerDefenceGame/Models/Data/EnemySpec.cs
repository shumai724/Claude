using TowerDefenceGame.Models.Entities;

namespace TowerDefenceGame.Models.Data;

/// <summary>敵の種類ごとの基本ステータス。</summary>
public sealed record EnemySpec
{
    public required EnemyType Type { get; init; }

    public required string Name { get; init; }

    public required int MaxHp { get; init; }

    /// <summary>移動速度（ワールド座標/秒）。</summary>
    public required float Speed { get; init; }

    /// <summary>倒したときに得られる所持金。</summary>
    public required int Reward { get; init; }

    /// <summary>ゴールに到達したときに減るライフ。</summary>
    public int DamageToBase { get; init; } = 1;

    /// <summary>防御力。1回の攻撃で受けるダメージをこの値だけ減らす。</summary>
    public int Armor { get; init; }

    public void Validate()
    {
        if (MaxHp <= 0 || Speed <= 0 || Reward < 0 || DamageToBase < 0 || Armor < 0)
        {
            throw new InvalidDataException($"敵 {Type} のステータスに不正な値があります。");
        }
    }
}
