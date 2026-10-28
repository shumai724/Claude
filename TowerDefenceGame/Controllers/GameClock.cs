using System.Diagnostics;

namespace TowerDefenceGame.Controllers;

/// <summary>フレーム間の経過時間を計測する。</summary>
public sealed class GameClock
{
    private readonly Stopwatch _stopwatch = new();
    private TimeSpan _last;

    public void Reset()
    {
        _stopwatch.Restart();
        _last = TimeSpan.Zero;
    }

    /// <summary>前回呼び出しからの経過秒数を返す。</summary>
    public float Tick()
    {
        if (!_stopwatch.IsRunning)
        {
            Reset();
        }

        var now = _stopwatch.Elapsed;
        float deltaTime = (float)(now - _last).TotalSeconds;
        _last = now;
        return deltaTime;
    }
}
