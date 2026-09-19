# DocExtract

Локальная библиотека извлечения текста из документов и операторское десктоп-приложение.

- **SDK (NuGet `DocExtract`)** — in-process API для .NET-программ: PDF, DOCX/XLSX, изображения; слова/строки с координатами; JSON (`schemaVersion: 1`).
- **Движок (`DocExtract.Engines.Onnx`)** — PP-OCR через ONNX Runtime, CPU по умолчанию, DirectML опционально. Модель распознавания ESLAV: русский + латиница/цифры.
- **Модели (`DocExtract.Models`)** — веса det/rec/cls, отдельный пакет, чтобы не раздувать ссылку на SDK.
- **Десктоп** — очередь файлов, превью с оверлеем, правка текста, зоны/шаблоны `.dex-template.json`, экспорт TXT/JSON, UI ru/en. Как пользоваться: [docs/DESKTOP.md](docs/DESKTOP.md).

Лицензия: [Apache-2.0](LICENSE).

## Быстрый старт SDK

```csharp
using DocExtract;
using DocExtract.Engines.Onnx;

var options = new ExtractorOptions
{
    Languages = ["ru", "en"],
    PreferEmbeddedText = true, // текстовый слой PDF, OCR только для сканов
    Gpu = GpuPreference.Auto,  // DirectML при наличии, иначе CPU
    RenderDpi = 200
};

using var extractor = OnnxDocumentExtractor.Create(options);
using var source = DocumentSource.FromFile(@"C:\docs\scan.pdf");
var result = await extractor.ExtractAsync(source);

Console.WriteLine(result.ToPlainText());
File.WriteAllText("out.json", result.ToJson());

var region = await extractor.ExtractRegionAsync(
    source,
    new PageRegion(0, Box.Normalized(0.1, 0.2, 0.4, 0.15)));
```

Без OCR-движка SDK всё равно извлекает текстовый слой цифровых PDF и текст Office.

`ExtractFieldsAsync` зарезервирован до этапа 1 (см. [docs/ROADMAP.md](docs/ROADMAP.md)).

## Требования

- .NET 10 SDK
- Windows 10/11 (Linux/macOS — этап 7)
- Для OCR: ONNX-модели в каталоге `models/` (см. ниже)

## Сборка

```powershell
dotnet restore DocExtract.slnx
dotnet build DocExtract.slnx
dotnet test DocExtract.slnx
```

Десктоп:

```powershell
dotnet run --project src/DocExtract.Desktop
```

Подробное руководство оператора (очередь, сегмент, зоны, настройки, экспорт): [docs/DESKTOP.md](docs/DESKTOP.md).

Модели OCR:

```powershell
pwsh -File tools/download-models.ps1
```

Ожидаемые файлы:

- `models/ch_PP-OCRv5_det_mobile.onnx`
- `models/eslav_PP-OCRv5_rec_mobile.onnx`
- `models/ch_PP-LCNet_x0_25_textline_ori_cls_mobile.onnx`

Каталог можно задать через `ExtractorOptions.ModelsDirectory` или переменную `DOCEXTRACT_MODELS`.

## Пакеты и установщик

```powershell
pwsh -File packaging/pack-nuget.ps1
pwsh -File packaging/pack-desktop.ps1
```

Установщик — self-contained win-x64 (Velopack), модели копируются из `models/`.

## Архитектура

Подробности: [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md). Кратко: десктоп только вызывает SDK; ingest (PDF/Office/растр) → OCR (`IOcrEngine`) → `DocumentResult`.

Публичные идентификаторы API — на английском, комментарии и этот README — на русском.
