namespace IchiPos.Startup;

public interface IConsoleAttacher
{
    /// <summary>呼び出し元のコンソールへのアタッチを試みる。成功した場合はtrueを返す。</summary>
    bool AttachToParent();

    /// <summary>新規コンソールを割り当てる。</summary>
    void AllocateNew();
}
