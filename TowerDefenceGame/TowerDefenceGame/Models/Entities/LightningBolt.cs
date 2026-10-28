using System.Numerics;
using TowerDefenceGame.Models.Core;

namespace TowerDefenceGame.Models.Entities;

/// <summary>
/// 標的に即座に命中し、近くの敵へ連鎖する電撃。
/// 命中後もしばらく残り、連鎖した経路（<see cref="Segments"/>）を画面に表示できるようにする。
/// </summary>
public sealed class LightningBolt : Projectile
{
    private const float DisplayDuration = 0.2f;

    private readonly List<(Vector2 From, Vector2 To)> _segments = [];
    private bool _struck;
    private float _elapsed;

    /// <param name="branches">最初の標的から何本に分かれて連鎖するか（ライトニング射程 Lv5「分岐雷」で 2）。</param>
    public LightningBolt(Vector2 origin, Enemy target, int damage, int chainCount, int branches, float chainRange, float falloff, HitModifiers modifiers)
        : base(origin, target, damage, speed: 1f, modifiers)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(branches);
        ChainCount = chainCount;
        Branches = branches;
        ChainRange = chainRange;
        Falloff = falloff;
    }

    public int ChainCount { get; }

    public int Branches { get; }

    public float ChainRange { get; }

    public float Falloff { get; }

    /// <summary>電撃が通った区間（タワー → 1体目、1体目 → 2体目 …）。分岐すると枝分かれする。</summary>
    public IReadOnlyList<(Vector2 From, Vector2 To)> Segments => _segments;

    /// <summary>表示の残り具合（1 → 0）。</summary>
    public float Fade => 1f - Math.Clamp(_elapsed / DisplayDuration, 0f, 1f);

    public override void Update(float deltaTime, World world)
    {
        if (!_struck)
        {
            _struck = true;
            Impact(world);
            return;
        }

        _elapsed += deltaTime;
        if (_elapsed >= DisplayDuration)
        {
            Deactivate();
        }
    }

    protected override bool OnHit(World world)
    {
        if (!Target.IsActive)
        {
            Deactivate();
            return false;
        }

        _segments.Add((Position, Target.Position));
        var struck = new HashSet<Enemy> { Target };
        Strike(Target, Damage);

        // 分岐するときは各枝を1歩ずつ交互に伸ばし、片方の枝が近くの敵を取り尽くさないようにする
        var heads = Enumerable.Repeat(Target.Position, Branches).ToArray();
        var alive = Enumerable.Repeat(true, Branches).ToArray();
        float damage = Damage;
        for (int i = 0; i < ChainCount; i++)
        {
            damage *= Falloff;
            for (int b = 0; b < Branches; b++)
            {
                if (!alive[b])
                {
                    continue;
                }

                var from = heads[b];
                var next = world.FindEnemiesInRange(from, ChainRange)
                    .Where(e => !struck.Contains(e))
                    .MinBy(e => Vector2.DistanceSquared(from, e.Position));
                if (next is null)
                {
                    alive[b] = false;
                    continue;
                }

                heads[b] = next.Position;
                _segments.Add((from, next.Position));
                struck.Add(next);
                Strike(next, Math.Max(1, (int)MathF.Round(damage)));
            }
        }

        return true;
    }
}
