using System.Text;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;

namespace InstagramUploader;

/// <summary>
/// EXIF 情報から Instagram 投稿用キャプションを生成します。
/// </summary>
public sealed class ExifCaptionBuilder : ICaptionBuilder
{
    /// <inheritdoc />
    public string BuildCaption(string filePath)
    {
        var directories = ImageMetadataReader.ReadMetadata(filePath);
        var subIfd = directories.OfType<ExifSubIfdDirectory>().FirstOrDefault();
        var ifd0 = directories.OfType<ExifIfd0Directory>().FirstOrDefault();

        if (subIfd is null && ifd0 is null)
        {
            return string.Empty;
        }

        var lines = new List<string>();

        AppendDateTime(lines, subIfd);
        AppendCombinedLine(lines, "カメラ", ifd0?.GetString(ExifDirectoryBase.TagMake), ifd0?.GetString(ExifDirectoryBase.TagModel));
        AppendCombinedLine(lines, "レンズ", subIfd?.GetString(ExifDirectoryBase.TagLensMake), subIfd?.GetString(ExifDirectoryBase.TagLensModel));
        AppendValue(lines, "焦点距離", subIfd?.GetString(ExifDirectoryBase.TagFocalLength), "mm");
        AppendValue(lines, "絞り", subIfd?.GetString(ExifDirectoryBase.TagFNumber), prefix: "f/");
        AppendValue(lines, "シャッタースピード", subIfd?.GetString(ExifDirectoryBase.TagExposureTime), "秒");
        AppendValue(lines, "ISO感度", subIfd?.GetString(ExifDirectoryBase.TagIsoEquivalent));
        AppendValue(lines, "露出補正", subIfd?.GetString(ExifDirectoryBase.TagExposureBias), " EV");
        AppendValue(lines, "測光方式", subIfd?.GetDescription(ExifDirectoryBase.TagMeteringMode));

        return string.Join(Environment.NewLine, lines);
    }

    /// <summary>
    /// 撮影日時をキャプション行として追加します。
    /// </summary>
    /// <param name="lines">出力先の行コレクションです。</param>
    /// <param name="subIfd">EXIF サブ IFD 情報です。</param>
    private static void AppendDateTime(ICollection<string> lines, ExifSubIfdDirectory? subIfd)
    {
        var dateTime = subIfd?.GetString(ExifDirectoryBase.TagDateTimeOriginal);
        if (string.IsNullOrWhiteSpace(dateTime))
        {
            return;
        }

        if (dateTime.Length >= 10 && dateTime[4] == ':' && dateTime[7] == ':')
        {
            dateTime = $"{dateTime[..4]}/{dateTime.Substring(5, 2)}/{dateTime[8..]}";
        }

        lines.Add($"撮影日時: {dateTime}");
    }

    /// <summary>
    /// 複数値を結合した行を追加します。
    /// </summary>
    /// <param name="lines">出力先の行コレクションです。</param>
    /// <param name="label">項目ラベルです。</param>
    /// <param name="values">結合対象の値です。</param>
    private static void AppendCombinedLine(ICollection<string> lines, string label, params string?[] values)
    {
        var combined = string.Join(" ", values.Where(value => !string.IsNullOrWhiteSpace(value)));
        if (!string.IsNullOrWhiteSpace(combined))
        {
            lines.Add($"{label}: {combined}");
        }
    }

    /// <summary>
    /// 単一値の行を追加します。
    /// </summary>
    /// <param name="lines">出力先の行コレクションです。</param>
    /// <param name="label">項目ラベルです。</param>
    /// <param name="value">項目値です。</param>
    /// <param name="suffix">値の後ろに付ける文字列です。</param>
    /// <param name="prefix">値の前に付ける文字列です。</param>
    private static void AppendValue(ICollection<string> lines, string label, string? value, string suffix = "", string prefix = "")
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            lines.Add($"{label}: {prefix}{value}{suffix}");
        }
    }
}
