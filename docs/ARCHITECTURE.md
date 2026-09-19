# Архитектура DocExtract

Каноническое ядро — in-process SDK на .NET 10. Десктоп на Avalonia только вызывает публичный API, пайплайн не дублируется.

## Слои

```
DocExtract.Desktop  -->  DocExtract (API)
                         ├─ Ingest (PDF / Office / растр)
                         ├─ Pipeline
                         ├─ Geometry (пиксели + 0..1)
                         └─ Export (TXT / JSON schemaVersion=1)
                                    │
                                    ▼
                         IOcrEngine
                                    │
                         DocExtract.Engines.Onnx  (PP-OCR, ONNX Runtime, CPU/DirectML)
```

## Координаты

На странице один канонический мир:

- пиксели растра при известном DPI, начало — левый верх;
- нормализованный прямоугольник 0..1 для шаблонов зон.

Текстовый слой PDF переводится из user space (пункты, начало снизу) в этот мир.

## PDF

Постранично, не одним режимом на файл:

1. Если `PreferEmbeddedText` и на странице достаточно символов текстового слоя — PdfPig, без OCR.
2. Иначе страница растеризуется (Pdfium / PDFtoImage) и уходит в `IOcrEngine`.

## Расширения

- `IOcrEngine` — Tesseract и облако (Azure/Yandex) без ломки контракта.
- `ExtractFieldsAsync` / `FieldSchema` / `ZoneTemplate` — этап 1; в MVP шаблон зон уже сериализуется.

## Python позже

JSON-контракт стабилен (`schemaVersion`). In-process Python — NativeAOT C-ABI на этапе 7, не pythonnet.
