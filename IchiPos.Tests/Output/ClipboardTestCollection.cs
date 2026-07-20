using Xunit;

namespace IchiPos.Tests.Output;

/// <summary>
/// 実際のWindowsクリップボードを読み書きするテストのコレクション。
/// クリップボードはプロセス横断の共有資源のため、並行実行すると互いの内容を壊す。
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public class ClipboardTestCollection
{
    public const string Name = "Clipboard";
}
