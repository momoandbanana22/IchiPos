using MessageBox = System.Windows.MessageBox;
using MessageBoxButton = System.Windows.MessageBoxButton;
using MessageBoxImage = System.Windows.MessageBoxImage;
using MessageBoxResult = System.Windows.MessageBoxResult;

namespace IchiPos.Gui;

/// <summary>
/// 再投稿確認をメッセージボックスで行うIRepostConfirmation実装(04書 G-015第3節)。
/// 誤ってEnterキーを押した場合に投稿されないよう、既定の選択は「いいえ」とする。
/// </summary>
public class MessageBoxRepostConfirmation : IRepostConfirmation
{
    public bool ConfirmRepost()
        => MessageBox.Show(
            "前回と同じ内容です。再投稿しますか？",
            "IchiPos",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question,
            MessageBoxResult.No) == MessageBoxResult.Yes;
}
