using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace IchiPos.Config;

/// <summary>
/// 定型文（04書 G-016 第2節、issue #111）のYAML読み取り。1件は次のいずれかで書ける。
///
/// <list type="bullet">
///   <item>文字列 … Misskey・X とも同じ本文。</item>
///   <item><c>misskey</c> / <c>x</c> を持つマップ … それぞれ Misskey用・X用の本文。</item>
/// </list>
///
/// マップで片方を省略（または空）にした場合は、もう片方の本文を流用する。両方が空の場合は
/// 両本文とも空の <see cref="TemplateEntry"/> を返し、除外は設定読み込み側（<see cref="ConfigLoader"/>）で行う。
/// </summary>
public sealed class TemplateEntryYamlConverter : IYamlTypeConverter
{
    public bool Accepts(Type type) => type == typeof(TemplateEntry);

    public object? ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
    {
        // 文字列形式: Misskey・X とも同じ本文。
        if (parser.TryConsume<Scalar>(out var scalar))
        {
            var text = scalar.Value ?? string.Empty;
            return new TemplateEntry(text, text);
        }

        // マップ形式: misskey / x を読み取る。
        if (parser.TryConsume<MappingStart>(out _))
        {
            string? misskey = null;
            string? x = null;

            while (!parser.TryConsume<MappingEnd>(out _))
            {
                var key = parser.Consume<Scalar>().Value;
                // 値はスカラー（複数行の | / > を含む）のみ対応する。
                var value = parser.Consume<Scalar>().Value;

                if (string.Equals(key, "misskey", StringComparison.OrdinalIgnoreCase))
                    misskey = value;
                else if (string.Equals(key, "x", StringComparison.OrdinalIgnoreCase))
                    x = value;
                // 未知キーは無視する。
            }

            // 片方が空（未記載・空文字・空白のみ）なら、もう片方の本文を流用する（第2節第3項）。
            var misskeyText = !string.IsNullOrWhiteSpace(misskey) ? misskey! : (x ?? string.Empty);
            var xText = !string.IsNullOrWhiteSpace(x) ? x! : (misskey ?? string.Empty);
            return new TemplateEntry(misskeyText, xText);
        }

        throw new YamlException("templates の要素は、文字列または misskey / x を持つマップである必要があります");
    }

    public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer)
        => throw new NotSupportedException("TemplateEntry の書き出しはサポートしていません");
}
