using Xunit;

namespace IchiPos.Tests.Gui;

/// <summary>
/// WPF の型初期化(BAML スキーマコンテキスト・ScrollViewer 等の静的コンストラクタ)はスレッドセーフでなく、
/// 複数スレッドから同時に初回初期化するとロック順の相違でデッドロックする(#93 で実測)。
/// XAML を読み込む/Window を生成するテスト(MainWindowLayoutTests・GuiCompositionRootTests)は、この共有
/// コレクションに入れて直列実行し、WPF 初回初期化が並行しないようにする。
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public class WpfUiCollection
{
    public const string Name = "WPF UI";
}
