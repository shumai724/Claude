using TowerDefenceGame.Models.Core;
using TowerDefenceGame.Models.Data;
using TowerDefenceGame.Models.Entities;
using TowerDefenceGame.Models.Map;
using TowerDefenceGame.Models.Waves;

namespace TowerDefenceGame.Developer.Bot;

/// <summary>
/// ゲームを自動でプレイする bot。プレイヤーと同じ操作（建設・強化・ウェーブ開始）だけを使う。
/// <para>
/// 考え方（上手な人のやり方をルールにしたもの）:
/// <list type="number">
/// <item>次のウェーブの敵の総HPに「余裕の倍率」を掛けたものを、必要な火力の目標にする</item>
/// <item>建設・強化の候補をすべて見積もり、1G あたりで一番守りが強くなる手を選ぶ（<see cref="DefenseEvaluator"/>）</item>
/// <item>守りに余裕があるうちは、金鉱を建てたり、大きな強化のために貯金したりする</item>
/// <item>敵がゴールに着いたら、すぐ補強し、次からは余裕の倍率を上げる。漏れずに凌げたら少しずつ戻す</item>
/// <item>最終ウェーブの前とその最中は、ゴールドを残さず使い切る</item>
/// </list>
/// </para>
/// 何をなぜしたかは <see cref="Log"/> に残す。
/// </summary>
internal sealed class AutoPlayer
{
    /// <summary>操作と操作の間隔（実時間の秒）。画面で見て追える速さにする。</summary>
    private const float ActionInterval = 0.35f;

    /// <summary>何もすることが無くなってから、次のウェーブを始めるまでの間（実時間の秒）。</summary>
    private const float WaitBeforeWave = 0.8f;

    private const float ThinkInterval = 0.5f;

    private const float BaseSafety = 1.6f;
    private const float SafetyStepOnLeak = 0.4f;
    private const float MaxSafety = 3f;
    private const float SafetyRecovery = 0.1f;

    /// <summary>買えない候補の価値が、買える候補のこの倍以上なら、貯金して待つ。</summary>
    private const float SaveValueRatio = 1.3f;

    /// <summary>貯金して待つのは、不足額が費用のこの割合以下のときだけ。</summary>
    private const float SaveShortfallRatio = 0.5f;

    private const int CandidateCellsPerType = 5;
    private const int MinWavesLeftForGoldMine = 6;
    private const int MaxLogLines = 300;

    private readonly List<string> _log = [];
    private Game? _game;
    private RouteSamples? _samples;
    private DefenseEvaluator? _evaluator;
    private float _cooldown;
    private float _waitTime;
    private bool _readyForWave;
    private float _safety = BaseSafety;
    private int _livesAtWaveStart;
    private int _lastLives;
    private bool _leaked;
    private string? _savingFor;
    private float _lastRatio;

    /// <summary>判断の記録（古い順）。</summary>
    public IReadOnlyList<string> Log => _log;

    /// <summary>ログが増えるたびに増える番号。画面側で追加分だけ表示するのに使う。</summary>
    public int LogVersion { get; private set; }

    /// <summary>今の見積もりの要約。</summary>
    public string Status { get; private set; } = "停止中";

    /// <summary>
    /// 自動プレイ中か。止めている間もウェーブの結果（漏れ）は見ているが、ログには残さない。
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>ゲームループから毎回呼ぶ。</summary>
    /// <param name="realDelta">前回からの実時間の経過秒数（倍速の影響を受けない）。</param>
    public void Update(Game game, float realDelta)
    {
        if (!ReferenceEquals(game, _game))
        {
            Attach(game);
        }

        if (!Enabled || game.State != GameState.Playing || _evaluator is null)
        {
            return;
        }

        var waves = game.WaveManager;
        NoticeLeak(game);

        if (_readyForWave && !waves.IsWaveInProgress)
        {
            _waitTime += realDelta;
            if (_waitTime >= WaitBeforeWave)
            {
                StartWave(game);
            }

            return;
        }

        _cooldown -= realDelta;
        if (_cooldown > 0f)
        {
            return;
        }

        if (TryAct(game))
        {
            _cooldown = ActionInterval;
            return;
        }

        _cooldown = ThinkInterval;
        if (!waves.IsWaveInProgress && waves.CanStartNextWave)
        {
            _readyForWave = true;
            _waitTime = 0f;
        }
    }

    private void Attach(Game game)
    {
        if (_game is not null)
        {
            _game.WaveManager.WaveStarted -= OnWaveStarted;
            _game.WaveManager.WaveCompleted -= OnWaveCompleted;
        }

        _game = game;
        _samples = new RouteSamples(game.Map.Route);
        _evaluator = new DefenseEvaluator(_samples);
        _cooldown = 0f;
        _waitTime = 0f;
        _readyForWave = false;
        _safety = BaseSafety;
        _leaked = false;
        _savingFor = null;
        _livesAtWaveStart = _lastLives = game.Player.Lives;
        game.WaveManager.WaveStarted += OnWaveStarted;
        game.WaveManager.WaveCompleted += OnWaveCompleted;
        Write($"―― {game.StageName}（Lv{game.Level}）を開始。初期資金 {game.Player.Gold}G・ライフ {game.Player.Lives}");
    }

    // ------------------------------------------------------------------
    // 1回分の判断
    // ------------------------------------------------------------------

    private bool TryAct(Game game)
    {
        var waves = game.WaveManager;
        bool inWave = waves.IsWaveInProgress;
        var wave = WaveProfile.From(game, inWave ? waves.CurrentWave : waves.NextWave);
        if (wave is null)
        {
            return false;
        }

        // 最終ウェーブに備えている（またはその最中）なら、貯金する理由はもう無い
        bool finalPush = inWave ? !waves.HasNextWave : waves.CurrentWaveNumber + 1 >= waves.TotalWaves;
        string phase = Phase(waves);

        var layout = CurrentLayout(game);
        var profiles = layout.Select(l => l.Profile).ToList();
        var now = _evaluator!.Evaluate(profiles, wave);
        var goal = new Goal(wave.TotalHp * _safety, wave.Toughest.Hp * _safety);
        bool comfortable = now.Total >= goal.Total && now.Toughest >= goal.Toughest;

        _lastRatio = now.Total / wave.TotalHp;
        Status = $"{phase}　推定火力 {_lastRatio:0.00}倍 / 目標 {_safety:0.00}倍"
            + $"　硬い敵（{wave.Toughest.Spec.Name}）{now.Toughest / wave.Toughest.Hp:0.00}倍"
            + (comfortable ? "　余裕あり" : "　不足");

        if (!inWave && !finalPush && comfortable && TryBuildGoldMine(game, phase))
        {
            return true;
        }

        // ウェーブ中は、足りていて漏れも無ければ慌てて買わない（強化のための貯金を優先）
        if (inWave && comfortable && !_leaked && !finalPush)
        {
            return false;
        }

        var candidates = Candidates(game, layout, profiles, wave, now, goal);
        if (candidates.Count == 0)
        {
            return false;
        }

        int gold = game.Player.Gold;
        var best = candidates.MaxBy(c => c.Value)!;
        var affordable = candidates.Where(c => c.Cost <= gold).MaxBy(c => c.Value);

        // 余裕があるときは、ずっと効率のいい手のために貯金する（あと少しで届く場合だけ）
        if (comfortable && !finalPush && best != affordable
            && (affordable is null || best.Value >= affordable.Value * SaveValueRatio)
            && best.Cost - gold <= best.Cost * SaveShortfallRatio)
        {
            if (_savingFor != best.Description)
            {
                _savingFor = best.Description;
                Write($"[{phase}] 「{best.Description}」（{best.Cost}G）のために貯金する");
            }

            return false;
        }

        if (affordable is null || affordable.Value <= 0f)
        {
            return false;
        }

        return Execute(game, affordable, wave, now, phase);
    }

    private bool Execute(Game game, Candidate action, WaveProfile wave, DefenseScore before, string phase)
    {
        bool done = action.Tower is { } tower
            ? game.TryUpgradeTower(tower, action.Stat!.Value)
            : game.TryPlaceTower(action.Type, action.Cell);
        if (!done)
        {
            return false;
        }

        _savingFor = null;
        Write($"[{phase}] {action.Description}（{action.Cost}G）{action.Reason}"
            + $"　火力 {before.Total / wave.TotalHp:0.00}→{action.After.Total / wave.TotalHp:0.00}倍");
        return true;
    }

    private void StartWave(Game game)
    {
        _readyForWave = false;
        _waitTime = 0f;
        var waves = game.WaveManager;
        int number = waves.CurrentWaveNumber + 1;
        if (game.StartNextWave())
        {
            Write($"[W{number}前] 準備完了 → ウェーブ {number}/{waves.TotalWaves} を開始（推定火力 {_lastRatio:0.00}倍 / 目標 {_safety:0.00}倍、残り {game.Player.Gold}G）");
        }
    }

    // ------------------------------------------------------------------
    // 候補の列挙と見積もり
    // ------------------------------------------------------------------

    private List<Candidate> Candidates(Game game, List<Placed> layout, List<BotTower> profiles, WaveProfile wave, DefenseScore now, Goal goal)
    {
        var list = new List<Candidate>();

        // 強化
        for (int i = 0; i < layout.Count; i++)
        {
            var tower = layout[i].Tower;
            if (tower is GoldMine)
            {
                continue;
            }

            foreach (var stat in tower.UpgradeStats)
            {
                if (!tower.CanUpgrade(stat))
                {
                    continue;
                }

                int cost = tower.GetUpgradeCost(stat);
                var preview = Copy(game, tower);
                preview.Upgrade(stat);
                var trial = new List<BotTower>(profiles) { [i] = BotTower.From(preview, layout[i].Profile.Value + cost) };
                var after = _evaluator!.Evaluate(trial, wave);
                int level = tower.GetLevel(stat);
                string description = $"{tower.Spec.Name}{tower.Cell}の{StatName(stat)}を強化（Lv{level}→{level + 1}{(level + 1 == Tower.MaxLevel ? "★" : "")}）";
                list.Add(new Candidate(description, UpgradeReason(tower, stat, wave), tower.Spec.Type, tower.Cell, tower, stat, cost,
                    Worth(now, after, goal) / cost, after));
            }
        }

        // 建設
        var counts = _evaluator!.CoverCounts(profiles);
        int attackCount = profiles.Count(p => p.IsAttack);
        foreach (var spec in game.TowerSpecs)
        {
            if (spec.Type == TowerType.GoldMine || (spec.Type == TowerType.Support && attackCount < 2))
            {
                continue;
            }

            foreach (var (cell, coveredTiles) in PromisingCells(game, spec, profiles, counts))
            {
                var preview = BotTower.From(game.CreatePreview(spec.Type, cell), spec.Cost);
                var trial = new List<BotTower>(profiles) { preview };
                var after = _evaluator.Evaluate(trial, wave);
                list.Add(new Candidate($"{spec.Name}を{cell}に建設", BuildReason(spec.Type, coveredTiles, profiles, preview, wave),
                    spec.Type, cell, null, null, spec.Cost, Worth(now, after, goal) / spec.Cost, after));
            }
        }

        return list;
    }

    /// <summary>
    /// 手の価値。目標に届いていない分を埋める効果を重く見る。
    /// すでに目標を超えている場合は、上積みの価値を小さめに見る。
    /// </summary>
    private static float Worth(DefenseScore before, DefenseScore after, Goal goal)
    {
        float filled = (MathF.Min(after.Total, goal.Total) - MathF.Min(before.Total, goal.Total)) / goal.Total
            + (MathF.Min(after.Toughest, goal.Toughest) - MathF.Min(before.Toughest, goal.Toughest)) / goal.Toughest;
        if (filled > 1e-4f)
        {
            return filled;
        }

        return 0.25f * ((after.Total - before.Total) / goal.Total + (after.Toughest - before.Toughest) / goal.Toughest);
    }

    /// <summary>種類ごとに、見積もる価値のありそうなマスを少数に絞る（全マスを見積もると重いため）。</summary>
    private IEnumerable<(GridPos Cell, float CoveredTiles)> PromisingCells(Game game, TowerSpec spec, List<BotTower> profiles, int[] counts)
    {
        var samples = _samples!;
        float tileSize = game.Map.TileSize;
        var scored = new List<(GridPos Cell, float Score, float CoveredTiles)>();
        foreach (var tile in game.Map.Tiles)
        {
            if (!tile.IsBuildable)
            {
                continue;
            }

            var center = game.Map.CellToWorld(tile.Cell);
            float score;
            float coveredTiles = 0f;
            if (spec.Type == TowerType.Support)
            {
                // 司令塔：範囲に入る攻撃タワーにかけた金額の合計（2基以上入る場所だけ）
                var inRange = profiles.Where(p => p.IsAttack && System.Numerics.Vector2.Distance(p.Position, center) <= spec.Range).ToList();
                score = inRange.Count >= 2 ? inRange.Sum(p => p.Value) : 0f;
            }
            else
            {
                var bits = samples.InRange(tile.Cell, center, spec.Range);
                coveredTiles = BitSet.Count(bits) * RouteSamples.Spacing / tileSize;
                score = spec.Type is TowerType.Frost or TowerType.Breaker
                    // 足止め・防御低下は、主力の射程と重なる場所ほど活きる
                    ? BitSet.Sum(bits, i => (counts[i] > 0 ? 1f + 0.4f * Math.Min(counts[i], 4) : 0.3f) * samples.GoalWeight(i))
                    // 攻撃タワーは、まだ手薄な道を長く射程に収める場所
                    : BitSet.Sum(bits, i => DefenseEvaluator.OverlapWeight(counts[i] + 1) * samples.GoalWeight(i));
            }

            if (score > 0f)
            {
                scored.Add((tile.Cell, score, coveredTiles));
            }
        }

        return scored
            .OrderByDescending(s => s.Score)
            .Take(CandidateCellsPerType)
            .Select(s => (s.Cell, s.CoveredTiles));
    }

    /// <summary>守りに余裕があり、残りウェーブが多いうちに金鉱を建てる。道から遠いマスを使い、好立地はつぶさない。</summary>
    private bool TryBuildGoldMine(Game game, string phase)
    {
        var spec = game.TowerSpecs.FirstOrDefault(s => s.Type == TowerType.GoldMine);
        var waves = game.WaveManager;
        if (spec is null || !game.Player.CanAfford(spec.Cost) || waves.CurrentWaveNumber < 1)
        {
            return false;
        }

        int wavesLeft = waves.TotalWaves - waves.CurrentWaveNumber;
        int limit = waves.TotalWaves >= 12 ? 2 : 1;
        int mines = game.World.Towers.Count(t => t is GoldMine);
        if (wavesLeft < MinWavesLeftForGoldMine || mines >= limit)
        {
            return false;
        }

        var samples = _samples!;
        var cell = game.Map.Tiles
            .Where(t => t.IsBuildable)
            .Select(t => t.Cell)
            .OrderBy(c => BitSet.Count(samples.InRange(c, game.Map.CellToWorld(c), 110f)))
            .Select(c => (GridPos?)c)
            .FirstOrDefault();
        if (cell is not { } target || !game.TryPlaceTower(TowerType.GoldMine, target))
        {
            return false;
        }

        Write($"[{phase}] {spec.Name}を{target}に建設（{spec.Cost}G）守りに余裕があり、残り {wavesLeft} ウェーブで元が取れる見込み。道から遠い場所に置いて好立地は空けておく");
        return true;
    }

    // ------------------------------------------------------------------
    // 漏れへの対応
    // ------------------------------------------------------------------

    private void NoticeLeak(Game game)
    {
        int lives = game.Player.Lives;
        if (lives < _lastLives && game.WaveManager.IsWaveInProgress && !_leaked)
        {
            _leaked = true;
            Write($"[{Phase(game.WaveManager)}] 敵がゴールに到達（ライフ {_lastLives}→{lives}）→ 手持ちで急いで補強する");
        }

        _lastLives = lives;
    }

    private void OnWaveStarted(Wave wave)
    {
        _livesAtWaveStart = _lastLives = _game?.Player.Lives ?? 0;
        _leaked = false;
        _readyForWave = false;
    }

    private void OnWaveCompleted(Wave wave)
    {
        if (_game is null)
        {
            return;
        }

        int lost = _livesAtWaveStart - _game.Player.Lives;
        float previous = _safety;
        if (lost > 0)
        {
            _safety = MathF.Min(MaxSafety, _safety + SafetyStepOnLeak);
            if (Enabled)
            {
                Write($"[W{wave.Number}] ライフを {lost} 失って凌いだ → 次からは目標火力を {previous:0.0}→{_safety:0.0}倍に上げる");
            }
        }
        else
        {
            _safety = MathF.Max(BaseSafety, _safety - SafetyRecovery);
            if (Enabled)
            {
                Write($"[W{wave.Number}] 漏れなしで凌いだ（所持金 {_game.Player.Gold}G）");
            }
        }
    }

    // ------------------------------------------------------------------
    // 補助
    // ------------------------------------------------------------------

    /// <summary>今あるタワーの一覧。見積もりには、司令塔の強化を受けていない仮のタワーの性能を使う。</summary>
    private static List<Placed> CurrentLayout(Game game) =>
        game.World.Towers
            .Where(t => t.IsActive)
            .Select(t => new Placed(t, BotTower.From(Copy(game, t), Invested(t))))
            .ToList();

    /// <summary>同じ種類・同じ強化レベルの仮のタワーを作る（マップには置かない）。</summary>
    private static Tower Copy(Game game, Tower tower)
    {
        var copy = game.CreatePreview(tower.Spec.Type, tower.Cell);

        // Lv5 の項目を先に上げると、他の項目の上限（Lv4）に引っかからない
        foreach (var stat in tower.UpgradeStats.OrderByDescending(tower.GetLevel))
        {
            for (int level = 1; level < tower.GetLevel(stat); level++)
            {
                copy.Upgrade(stat);
            }
        }

        return copy;
    }

    /// <summary>売却額から、かけた金額をおおよそ逆算する（売却額は投資額の7割）。</summary>
    private static int Invested(Tower tower) => (int)(tower.SellPrice / 0.7f);

    private static string Phase(WaveManager waves) =>
        waves.IsWaveInProgress ? $"W{waves.CurrentWaveNumber}" : $"W{waves.CurrentWaveNumber + 1}前";

    private static string StatName(UpgradeStat stat) => stat switch
    {
        UpgradeStat.Damage => "攻撃力",
        UpgradeStat.Range => "射程",
        UpgradeStat.FireRate => "攻撃速度",
        UpgradeStat.BuffPower => "強化量",
        UpgradeStat.BuffRange => "効果範囲",
        UpgradeStat.GoldAmount => "産出量",
        UpgradeStat.GoldSpeed => "産出速度",
        _ => stat.ToString(),
    };

    private static string BuildReason(TowerType type, float coveredTiles, List<BotTower> profiles, BotTower preview, WaveProfile wave) => type switch
    {
        TowerType.Support =>
            $"周りの攻撃タワー {profiles.Count(p => p.IsAttack && System.Numerics.Vector2.Distance(p.Position, preview.Position) <= preview.Range)} 基をまとめて強化できる",
        TowerType.Frost => $"主力の射程と重なる道 {coveredTiles:0.0} マス分で敵を足止めできる",
        TowerType.Breaker => wave.HasArmored
            ? $"防御力のある{wave.Groups.Where(g => g.Spec.Armor > 0).Select(g => g.Spec.Name).First()}に主力のダメージを通すため"
            : $"主力の射程と重なる道 {coveredTiles:0.0} マス分で防御力を下げられる",
        TowerType.Cannon or TowerType.Tesla => $"道 {coveredTiles:0.0} マス分を射程に収め、固まった敵をまとめて削れる",
        _ => $"道 {coveredTiles:0.0} マス分を射程に収める位置",
    };

    private static string UpgradeReason(Tower tower, UpgradeStat stat, WaveProfile wave)
    {
        if (tower.GetLevel(stat) + 1 == Tower.MaxLevel)
        {
            return "Lv5 の追加効果が付く";
        }

        return stat switch
        {
            UpgradeStat.Damage when wave.HasArmored => "攻撃力を上げて防御力の高い敵にも通す",
            UpgradeStat.Range => "射程を伸ばして攻撃できる時間を増やす",
            UpgradeStat.BuffPower or UpgradeStat.BuffRange => "周りのタワーへの強化を厚くする",
            _ => "",
        };
    }

    private void Write(string line)
    {
        _log.Add(line);
        if (_log.Count > MaxLogLines)
        {
            _log.RemoveRange(0, _log.Count - MaxLogLines);
        }

        LogVersion++;
    }

    private sealed record Placed(Tower Tower, BotTower Profile);

    private readonly record struct Goal(float Total, float Toughest);

    private sealed record Candidate(
        string Description,
        string Reason,
        TowerType Type,
        GridPos Cell,
        Tower? Tower,
        UpgradeStat? Stat,
        int Cost,
        float Value,
        DefenseScore After);
}
