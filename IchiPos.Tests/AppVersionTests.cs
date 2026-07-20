using System.Reflection;
using Xunit;

namespace IchiPos.Tests;

/// <summary>
/// バージョン番号は <c>AppVersion.Current</c>（--version の出力・GUIのP-11）と
/// <c>IchiPos.Core.csproj</c> の &lt;Version&gt;（配布exeのファイルバージョン）の2箇所にあり、
/// リリース手順で人手により同期させている。片方の更新漏れを検出するためのテスト。
/// </summary>
public class AppVersionTests
{
    /// <summary>
    /// csproj の &lt;Version&gt; から生成される AssemblyInformationalVersion を取得する。
    /// SourceLink 等により "2.3.0+&lt;commit hash&gt;" 形式になるため、'+' 以降を除く。
    /// </summary>
    private static string CsprojVersion()
    {
        var informational = typeof(AppVersion).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()!
            .InformationalVersion;
        return informational.Split('+')[0];
    }

    [Fact]
    public void AppVersionのCurrentはcsprojのVersionと一致する()
    {
        // 不一致の場合、--version の出力と配布exeのファイルバージョンが食い違う（#45）。
        Assert.Equal(AppVersion.Current, CsprojVersion());
    }

    [Fact]
    public void AppVersionのCurrentはセマンティックバージョン形式である()
    {
        Assert.Matches(@"^\d+\.\d+\.\d+$", AppVersion.Current);
    }
}
