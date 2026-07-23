using System.Reflection;

namespace IchiPos;

/// <summary>
/// アプリのバージョン番号。正典は <c>IchiPos.Core.csproj</c> の &lt;Version&gt; のみ（#52）。
/// ここでは、その値から生成される <see cref="AssemblyInformationalVersionAttribute"/> を
/// 実行時に読み取って導出する。バージョンを上げるときは csproj の1箇所を変えればよい。
/// </summary>
public static class AppVersion
{
    // SourceLink 等により InformationalVersion は "2.3.1+<commit hash>" 形式になるため、'+' 以降を除く。
    public static string Current { get; } =
        typeof(AppVersion).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()!
            .InformationalVersion
            .Split('+')[0];
}
