using System.Text;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;

namespace InstagramUploader;

public sealed class ExifCaptionBuilder : ICaptionBuilder
{
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

    private static void AppendCombinedLine(ICollection<string> lines, string label, params string?[] values)
    {
        var combined = string.Join(" ", values.Where(value => !string.IsNullOrWhiteSpace(value)));
        if (!string.IsNullOrWhiteSpace(combined))
        {
            lines.Add($"{label}: {combined}");
        }
    }

    private static void AppendValue(ICollection<string> lines, string label, string? value, string suffix = "", string prefix = "")
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            lines.Add($"{label}: {prefix}{value}{suffix}");
        }
    }
}
