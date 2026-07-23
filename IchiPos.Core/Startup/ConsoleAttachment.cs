namespace IchiPos.Startup;

/// <summary>
/// CLIモード起動時のコンソール確保(04書「モード判定」節・「実行ファイル構成」節)。
/// 実行ファイルはGUIサブシステムのため、既定ではコンソールが割り当てられていない。
/// 呼び出し元のコンソールへのアタッチを試み、失敗した場合(呼び出し元にコンソールがない場合)は
/// 新規コンソールを割り当てる。
/// </summary>
public static class ConsoleAttachment
{
    public static void Ensure(IConsoleAttacher attacher)
    {
        if (!attacher.AttachToParent())
        {
            attacher.AllocateNew();
        }
    }
}
