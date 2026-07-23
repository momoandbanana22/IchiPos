using System.Xml.Linq;

namespace IchiPos.Tests;

/// <summary>
/// バージョン番号の正典は <c>IchiPos.Core.csproj</c> の &lt;Version&gt; のみ（#52）。
/// <c>AppVersion.Current</c>（--version の出力・GUIのP-11）は、csproj の &lt;Version&gt; から
/// 生成される AssemblyInformationalVersion を実行時に導出するため、二重管理はしていない。
/// 本テストは、その導出が csproj の宣言値どおりに機能していること（ビルドの配線が壊れていないこと）と、
/// 形式がセマンティックバージョンであることを検証する。
/// </summary>
public class AppVersionTests
{
    /// <summary>
    /// 正典である csproj の &lt;Version&gt; をソースツリーから直接読む。
    /// AppVersion.Current の導出元（アセンブリ属性）とは独立した経路で取得することで、
    /// 導出が宣言値どおりに機能していることを検証できる。
    /// </summary>
    private static string CsprojDeclaredVersion()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var csproj = Path.Combine(dir.FullName, "IchiPos.Core", "IchiPos.Core.csproj");
            if (File.Exists(csproj))
            {
                var version = XDocument.Load(csproj)
                    .Descendants("Version")
                    .FirstOrDefault()?.Value;
                return version
                    ?? throw new InvalidOperationException("csproj に <Version> が見つかりません。");
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("IchiPos.Core.csproj が見つかりません。");
    }

    [Fact]
    public void AppVersionのCurrentはcsprojの宣言値と一致する()
    {
        // csproj が唯一の正典。AppVersion.Current はそこから導出されるため、
        // 一致しなければ導出（AssemblyInformationalVersion の生成・解析）が壊れている（#52）。
        Assert.Equal(CsprojDeclaredVersion(), AppVersion.Current);
    }

    [Fact]
    public void AppVersionのCurrentはセマンティックバージョン形式である()
    {
        Assert.Matches(@"^\d+\.\d+\.\d+$", AppVersion.Current);
    }
}
