namespace TowerDefenceGame.Models.Data;

/// <summary>ステージ選択画面に並べる1ステージ分の情報。</summary>
/// <param name="Id">ステージの識別子（ファイル名から拡張子を除いたもの）。クリア記録のキーに使う。</param>
/// <param name="FilePath">ステージファイルのパス。プレイ開始時はここから読み直す。</param>
/// <param name="Data">
/// 表示用に読み込んだデータ。マップのマスはタワー設置で書き換わるため、
/// プレイにはこのインスタンスを使わず <see cref="StageLoader.Load"/> で読み直すこと。
/// </param>
public sealed record StageSummary(string Id, string FilePath, StageData Data)
{
    public string Name => Data.Name;
}
