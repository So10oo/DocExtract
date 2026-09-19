using DocExtract.Ocr;

namespace DocExtract.Engines.Onnx;

/// <summary>
/// Поиск каталога ONNX-моделей: опции, DOCEXTRACT_MODELS, models/ рядом с приложением, подъём к корню репозитория.
/// </summary>
public static class ModelLocator
{
    public const string DetFileName = "ch_PP-OCRv5_det_mobile.onnx";
    public const string RecFileName = "eslav_PP-OCRv5_rec_mobile.onnx";
    public const string ClsFileName = "ch_PP-LCNet_x0_25_textline_ori_cls_mobile.onnx";

    public static string ResolveDirectory(string? explicitDirectory = null)
    {
        foreach (var candidate in EnumerateCandidates(explicitDirectory))
        {
            if (HasRequiredModels(candidate))
            {
                return candidate;
            }
        }

        throw new FileNotFoundException(
            "Не найдены ONNX-модели PP-OCR. Запустите tools/download-models.ps1 " +
            $"или укажите ExtractorOptions.ModelsDirectory. Ожидаются файлы {DetFileName}, {RecFileName}, {ClsFileName}.");
    }

    public static bool HasRequiredModels(string directory)
    {
        if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
        {
            return false;
        }

        return File.Exists(Path.Combine(directory, DetFileName))
               && File.Exists(Path.Combine(directory, RecFileName))
               && File.Exists(Path.Combine(directory, ClsFileName));
    }

    public static string DetPath(string directory) => Path.Combine(directory, DetFileName);

    public static string RecPath(string directory) => Path.Combine(directory, RecFileName);

    public static string ClsPath(string directory) => Path.Combine(directory, ClsFileName);

    private static IEnumerable<string> EnumerateCandidates(string? explicitDirectory)
    {
        if (!string.IsNullOrWhiteSpace(explicitDirectory))
        {
            yield return Path.GetFullPath(explicitDirectory);
        }

        var env = Environment.GetEnvironmentVariable("DOCEXTRACT_MODELS");
        if (!string.IsNullOrWhiteSpace(env))
        {
            yield return Path.GetFullPath(env);
        }

        foreach (var root in new[] { AppContext.BaseDirectory, Directory.GetCurrentDirectory() })
        {
            var current = new DirectoryInfo(root);
            for (var i = 0; i < 8 && current is not null; i++)
            {
                yield return Path.Combine(current.FullName, "models");
                current = current.Parent;
            }
        }
    }
}
