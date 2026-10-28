using System.Numerics;
using TowerDefenceGame.Models.Core;

namespace TowerDefenceGame.Models.Entities;

/// <summary>フィールド上に存在するオブジェクトの基底クラス。</summary>
public abstract class Entity
{
    protected Entity(Vector2 position)
    {
        Position = position;
    }

    public Vector2 Position { get; protected set; }

    public bool IsActive { get; private set; } = true;

    public abstract void Update(float deltaTime, World world);

    public void Deactivate() => IsActive = false;
}
