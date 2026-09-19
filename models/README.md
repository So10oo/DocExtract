# ONNX-модели PP-OCR

В этот каталог кладутся веса, которые **не хранятся в git**.

Скачивание:

```powershell
pwsh -File tools/download-models.ps1
```

Файлы MVP (печатный RU+EN / ESLAV):

| Файл | Назначение |
|------|------------|
| `ch_PP-OCRv5_det_mobile.onnx` | Детекция текстовых блоков |
| `eslav_PP-OCRv5_rec_mobile.onnx` | Распознавание (восточнославянские + латиница/цифры) |
| `ch_PP-LCNet_x0_25_textline_ori_cls_mobile.onnx` | Ориентация строки |

Источник: [RapidAI/RapidOCR на ModelScope](https://www.modelscope.cn/models/RapidAI/RapidOCR) (ONNX, тег v3.9.2). Лицензии моделей — Apache-2.0 (PaddleOCR / RapidOCR).

Установщик десктопа копирует `*.onnx` внутрь приложения. NuGet `DocExtract.Models` упаковывает те же файлы отдельно от SDK.
