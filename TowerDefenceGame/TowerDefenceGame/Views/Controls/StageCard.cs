using TowerDefenceGame.Models.Data;
using TowerDefenceGame.Models.Progress;
using TowerDefenceGame.Views.Painters;
using TowerDefenceGame.Views.Rendering;

namespace TowerDefenceGame.Views.Controls;

/// <summary>
/// ステージ選択画面の1枚のカード。マップの縮小図・難易度・説明・クリア記録を表示し、
/// どの難易度レベルで遊ぶかを選ばせる。各レベルは1つ下のレベルをクリアすると解放される。
/// </summary>
public sealed class StageCard : Control
{
    // 幅 1040px のウィンドウに余白込みで4枚並ぶ大きさ
    public const int CardWidth = 236;
    public const int CardHeight = 344;

    private const int Inset = 10;
    private const int ThumbnailWidth = CardWidth - Inset * 2;
    private const int ButtonHeight = 30;
    private const int ButtonGap = 4;

    /// <summary>レベルの説明は長いので、ツールチップを読み切れるまで出しておく時間（ミリ秒）。</summary>
    private const int ToolTipDuration = 20000;

    private static readonly Color CardBack = Color.FromArgb(250, 250, 252);
    private static readonly Color HoverBack = Color.FromArgb(255, 246, 214);
    private static readonly Color Border = Color.FromArgb(70, 76, 88);
    private static readonly Color HoverBorder = Color.FromArgb(240, 180, 40);
    private static readonly Color TextMain = Color.FromArgb(30, 32, 38);
    private static readonly Color TextSub = Color.FromArgb(100, 106, 118);
    private static readonly Color StarOn = Color.FromArgb(235, 160, 20);
    private static readonly Color StarOff = Color.FromArgb(200, 204, 212);
    private static readonly Color ClearColor = Color.FromArgb(40, 150, 70);

    // ボタンの色は Lv1 の青から最高レベルの赤へ少しずつ変える
    private static readonly Color LowLevelButtonBack = Color.FromArgb(60, 120, 200);
    private static readonly Color HighLevelButtonBack = Color.FromArgb(200, 50, 45);
    private static readonly Color LockedButtonBack = Color.FromArgb(175, 178, 186);

    private readonly StageRecord _record;
    private readonly int _levelCount;
    private readonly Bitmap _thumbnail;
    private readonly Font _titleFont;
    private readonly Font _bodyFont;
    private readonly Font _smallFont;
    private readonly ToolTip _toolTip = new() { AutoPopDelay = ToolTipDuration };
    private bool _hover;

    /// <param name="levels">このステージで遊べる各レベルの補正（Lv1 から順）。</param>
    public StageCard(StageSummary stage, StageRecord? record, int number, IReadOnlyList<DifficultyRules> levels)
    {
        Stage = stage;
        _record = record ?? new StageRecord();
        _levelCount = levels.Count;
        Number = number;
        Size = new Size(CardWidth, CardHeight);
        Margin = new Padding(6);
        Cursor = Cursors.Hand;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);

        _titleFont = new Font("Meiryo UI", 15f, FontStyle.Bold, GraphicsUnit.Pixel);
        _bodyFont = new Font("Meiryo UI", 12f, FontStyle.Regular, GraphicsUnit.Pixel);
        _smallFont = new Font("Meiryo UI", 12f, FontStyle.Bold, GraphicsUnit.Pixel);
        _thumbnail = RenderThumbnail(stage);

        AddLevelButtons(levels);
    }

    public StageSummary Stage { get; }

    public int Number { get; }

    /// <summary>遊ぶレベルが選ばれたとき。カードのボタン以外の場所をクリックした場合は Lv1。</summary>
    public event Action<int>? LevelSelected;

    protected override void OnClick(EventArgs e)
    {
        base.OnClick(e);
        LevelSelected?.Invoke(DifficultyRules.NormalLevel);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
        g.Clear(_hover ? HoverBack : CardBack);

        using (var pen = new Pen(_hover ? HoverBorder : Border, _hover ? 3f : 1f))
        {
            g.DrawRectangle(pen, 1, 1, Width - 3, Height - 3);
        }

        g.DrawImage(_thumbnail, Inset, Inset);
        int y = Inset + _thumbnail.Height + 8;

        using var main = new SolidBrush(TextMain);
        using var sub = new SolidBrush(TextSub);
        g.DrawString($"{Number}. {Stage.Name}", _titleFont, main, Inset, y);
        y += 24;

        DrawStars(g, Inset, y, Stage.Data.Difficulty);
        g.DrawString($"ウェーブ {Stage.Data.Waves.Count}", _bodyFont, sub, Inset + 92, y);
        y += 20;
        g.DrawString($"初期資金 {Stage.Data.InitialGold}G　ライフ {Stage.Data.InitialLives}", _bodyFont, sub, Inset, y);
        y += 22;

        g.DrawString(Stage.Data.Description, _bodyFont, main, new RectangleF(Inset, y, ThumbnailWidth, 34));

        DrawRecord(g, Height - Inset - ButtonHeight - 42, sub);
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        base.OnMouseEnter(e);
        _hover = true;
        Invalidate();
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        // ボタンの上に移ったときもカードから出たことになるので、本当に外に出たときだけ解除する
        _hover = ClientRectangle.Contains(PointToClient(Cursor.Position));
        Invalidate();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _thumbnail.Dispose();
            _titleFont.Dispose();
            _bodyFont.Dispose();
            _smallFont.Dispose();
            _toolTip.Dispose();
        }

        base.Dispose(disposing);
    }

    /// <summary>ゲーム画面と同じ MapPainter で描いたマップを縮小して画像にする。</summary>
    private static Bitmap RenderThumbnail(StageSummary stage)
    {
        var map = stage.Data.Map;
        float scale = ThumbnailWidth / map.Width;
        var bitmap = new Bitmap(ThumbnailWidth, (int)MathF.Ceiling(map.Height * scale));
        using var graphics = Graphics.FromImage(bitmap);
        using var renderer = new GdiRenderer();
        graphics.ScaleTransform(scale, scale);
        renderer.Begin(graphics);
        new MapPainter().Draw(renderer, map);
        return bitmap;
    }

    private static Button CreateButton(string text, Rectangle bounds, Color back)
    {
        var button = new Button
        {
            Text = text,
            Bounds = bounds,
            FlatStyle = FlatStyle.Flat,
            BackColor = back,
            ForeColor = Color.White,
            Font = new Font("Meiryo UI", 9f, FontStyle.Bold),
            Cursor = Cursors.Hand,
            TabStop = false,
        };
        button.FlatAppearance.BorderSize = 0;
        return button;
    }

    private static Color LevelColor(int index, int count)
    {
        float t = count <= 1 ? 0f : (float)index / (count - 1);
        return Color.FromArgb(
            (int)(LowLevelButtonBack.R + (HighLevelButtonBack.R - LowLevelButtonBack.R) * t),
            (int)(LowLevelButtonBack.G + (HighLevelButtonBack.G - LowLevelButtonBack.G) * t),
            (int)(LowLevelButtonBack.B + (HighLevelButtonBack.B - LowLevelButtonBack.B) * t));
    }

    /// <summary>ツールチップに出す補正の項目。値は表示用の文字列で、前のレベルとの比較にも使う。</summary>
    private static IReadOnlyList<(string Label, string Value)> DescribeItems(DifficultyRules rules) =>
    [
        ("敵の HP", $"×{rules.EnemyHpMultiplier:0.##}"),
        ("敵の移動速度", $"×{rules.EnemySpeedMultiplier:0.##}"),
        ("敵の防御力", $"{rules.EnemyArmorBonus:+0;-0;+0}"),
        ("撃破報酬", $"×{rules.RewardMultiplier:0.##}"),
        ("ウェーブボーナス", $"×{rules.WaveBonusMultiplier:0.##}"),
        ("建設費", $"×{rules.TowerCostMultiplier:0.##}"),
        ("強化費", $"×{rules.UpgradeCostMultiplier:0.##}"),
        ("ライフ", $"×{rules.LivesMultiplier:0.##}"),
        ("道の近くの荒れ地", $"{rules.BlockedTileRatio:P0}"),
    ];

    /// <summary>レベルの補正を、前のレベルから変わる点と、基本のルールからの補正の一覧にする。</summary>
    private static string DescribeLevel(DifficultyRules rules, DifficultyRules? previous)
    {
        if (previous is null)
        {
            return "補正なしの基本のルール";
        }

        var now = DescribeItems(rules);
        var changes = now.Zip(DescribeItems(previous))
            .Where(p => p.First.Value != p.Second.Value)
            .Select(p => $"{p.First.Label} {p.Second.Value} → {p.First.Value}")
            .DefaultIfEmpty("なし");
        var total = now.Zip(DescribeItems(DifficultyRules.Normal))
            .Where(p => p.First.Value != p.Second.Value)
            .Select(p => $"{p.First.Label} {p.First.Value}");
        return string.Join("\n", ["【このレベルで変わる点】", .. changes, "", "【基本のルールからの補正】", .. total]);
    }

    private void AddLevelButtons(IReadOnlyList<DifficultyRules> levels)
    {
        int buttonWidth = (ThumbnailWidth - ButtonGap * (levels.Count - 1)) / levels.Count;
        int buttonY = CardHeight - Inset - ButtonHeight;
        for (int i = 0; i < levels.Count; i++)
        {
            int level = levels[i].Level;
            bool unlocked = _record.IsUnlocked(level);
            var best = _record.Get(level);
            var bounds = new Rectangle(Inset + i * (buttonWidth + ButtonGap), buttonY, buttonWidth, ButtonHeight);
            var button = CreateButton(best is null ? $"{level}" : $"{level}✔", bounds, unlocked ? LevelColor(i, levels.Count) : LockedButtonBack);

            // ロック中も押せるようにしておく（ツールチップを出すため。押したときは Controller が拒否音を鳴らす）
            if (!unlocked)
            {
                button.Cursor = Cursors.No;
            }

            string status = !unlocked ? $"Lv{level - 1} をクリアすると解放されます。"
                : best is null ? "未クリア"
                : $"✔ クリア済み　ベスト {best.BestScore}（ライフ {best.BestLives}）";
            _toolTip.SetToolTip(button, $"【Lv{level}】{status}\n\n{DescribeLevel(levels[i], i > 0 ? levels[i - 1] : null)}");
            button.Click += (_, _) => LevelSelected?.Invoke(level);
            Controls.Add(button);
        }
    }

    private void DrawRecord(Graphics g, int y, Brush sub)
    {
        int highest = _record.HighestClearedLevel();
        if (_record.Get(highest) is not { } best)
        {
            g.DrawString($"未クリア（全 {_levelCount} レベル）", _smallFont, sub, Inset, y);
            return;
        }

        using var brush = new SolidBrush(ClearColor);
        g.DrawString($"✔ Lv{highest} までクリア（全 {_levelCount} レベル）", _smallFont, brush, Inset, y);
        g.DrawString($"Lv{highest} ベスト {best.BestScore}（ライフ {best.BestLives}）", _smallFont, sub, Inset, y + 18);
    }

    private void DrawStars(Graphics g, int x, int y, int difficulty)
    {
        using var on = new SolidBrush(StarOn);
        using var off = new SolidBrush(StarOff);
        for (int i = 0; i < StageData.MaxDifficulty; i++)
        {
            g.DrawString("★", _bodyFont, i < difficulty ? on : off, x + i * 16, y);
        }
    }
}
