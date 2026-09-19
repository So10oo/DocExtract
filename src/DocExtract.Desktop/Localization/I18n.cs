using CommunityToolkit.Mvvm.ComponentModel;

namespace DocExtract.Desktop.Localization;

public sealed partial class I18n : ObservableObject
{
    public static I18n Current { get; } = new();

    private string _language = "ru";

    public string Language
    {
        get => _language;
        set
        {
            if (SetProperty(ref _language, value))
            {
                OnPropertyChanged(string.Empty);
            }
        }
    }

    public string Title => T("DocExtract — оператор");
    public string Queue => T("Очередь");
    public string AddFiles => T("Файлы…");
    public string AddFolder => T("Папка…");
    public string Process => T("Обработать");
    public string Retry => T("Повтор");
    public string Remove => T("Удалить");
    public string Export => T("Экспорт");
    public string Preview => T("Просмотр");
    public string Text => T("Текст");
    public string Zones => T("Зоны");
    public string Settings => T("Настройки");
    public string Page => T("Страница");
    public string Status => T("Статус");
    public string Pending => T("ожидает");
    public string Running => T("в работе");
    public string Done => T("готово");
    public string Error => T("ошибка");
    public string SelectRegion => T("Сегмент");
    public string DrawZone => T("Рисовать зону");
    public string RecognizeRegion => T("Распознать сегмент");
    public string SaveTemplate => T("Сохранить шаблон");
    public string LoadTemplate => T("Загрузить шаблон");
    public string ZoneName => T("Имя зоны");
    public string Languages => T("Языки OCR");
    public string Gpu => T("GPU");
    public string GpuAuto => T("Auto (DirectML)");
    public string GpuOff => T("Только CPU");
    public string Dpi => T("DPI растра");
    public string PreferText => T("Текстовый слой PDF сначала");
    public string ExportFolder => T("Папка экспорта");
    public string UiLanguage => T("Язык интерфейса");
    public string SaveSettings => T("Сохранить настройки");
    public string Ready => T("Готово.");
    public string Processing => T("Обработка…");
    public string NoFile => T("Выберите файл в очереди.");
    public string ModelsMissing => T("ONNX-модели не найдены. Запустите tools/download-models.ps1 — PDF с текстовым слоем всё равно можно извлекать.");
    public string Edited => T("Исправленный текст");
    public string Overlay => T("Оверлей слов");
    public string Russian => T("Русский");
    public string English => T("English");

    private string T(string ru) => _language == "en" ? En(ru) : ru;

    private static string En(string ru) => ru switch
    {
        "DocExtract — оператор" => "DocExtract — operator",
        "Очередь" => "Queue",
        "Файлы…" => "Files…",
        "Папка…" => "Folder…",
        "Обработать" => "Process",
        "Повтор" => "Retry",
        "Удалить" => "Remove",
        "Экспорт" => "Export",
        "Просмотр" => "Preview",
        "Текст" => "Text",
        "Зоны" => "Zones",
        "Настройки" => "Settings",
        "Страница" => "Page",
        "Статус" => "Status",
        "ожидает" => "pending",
        "в работе" => "running",
        "готово" => "done",
        "ошибка" => "error",
        "Сегмент" => "Segment",
        "Рисовать зону" => "Draw zone",
        "Распознать сегмент" => "Recognize segment",
        "Сохранить шаблон" => "Save template",
        "Загрузить шаблон" => "Load template",
        "Имя зоны" => "Zone name",
        "Языки OCR" => "OCR languages",
        "GPU" => "GPU",
        "Auto (DirectML)" => "Auto (DirectML)",
        "Только CPU" => "CPU only",
        "DPI растра" => "Render DPI",
        "Текстовый слой PDF сначала" => "Prefer PDF text layer",
        "Папка экспорта" => "Export folder",
        "Язык интерфейса" => "UI language",
        "Сохранить настройки" => "Save settings",
        "Готово." => "Ready.",
        "Обработка…" => "Processing…",
        "Выберите файл в очереди." => "Select a file in the queue.",
        "ONNX-модели не найдены. Запустите tools/download-models.ps1 — PDF с текстовым слоем всё равно можно извлекать." =>
            "ONNX models not found. Run tools/download-models.ps1 — digital PDF text can still be extracted.",
        "Исправленный текст" => "Corrected text",
        "Оверлей слов" => "Word overlay",
        "Русский" => "Russian",
        "English" => "English",
        _ => ru
    };
}
