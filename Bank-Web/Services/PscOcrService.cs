using System.Text;
using System.Text.RegularExpressions;
using Tesseract;

namespace Bank.Web.Services;

public sealed class PscOcrService
{
    private readonly string _tessdataPath;

    public PscOcrService(IWebHostEnvironment env)
    {
        _tessdataPath = Path.Combine(env.ContentRootPath, "tessdata");
    }

    public OcrResult ReadText(byte[] imageBytes)
    {
        if (!Directory.Exists(_tessdataPath))
            return new OcrResult(false, "tessdata folder not found.", "", 0, null, null);

        using var engine = new TesseractEngine(_tessdataPath, "eng", EngineMode.LstmOnly);

        using var pix = Pix.LoadFromMemory(imageBytes);
        using var page = engine.Process(pix, PageSegMode.Auto);

        var text = page.GetText() ?? "";
        var conf = page.GetMeanConfidence();

        if (string.IsNullOrWhiteSpace(text))
            return new OcrResult(false, "OCR returned empty text.", "", conf, null, null);

        var extractedPpsn = ExtractPpsn(text);
        var extractedName = ExtractNameLine(text);

        return new OcrResult(true, "OCR OK.", text, conf, extractedName, extractedPpsn);
    }

    public ValidationResult ValidateFrontName(string expectedFirst, string expectedMiddle, string expectedLast, OcrResult ocr)
    {
        if (!ocr.Ok) return new ValidationResult(false, ocr.Message, null, ocr.Confidence, ocr.Text);

        var expectedTokens = BuildNameTokens(expectedFirst, expectedMiddle, expectedLast);
        var hay = NormalizeForMatch(ocr.Text);

        var missing = expectedTokens.Where(t => !hay.Contains(t)).ToList();
        if (missing.Count > 0)
            return new ValidationResult(false, $"Name mismatch. Missing token(s): {string.Join(", ", missing)}", null, ocr.Confidence, ocr.Text);

        return new ValidationResult(true, "PSC Front validated: name matched.", null, ocr.Confidence, ocr.Text);
    }

    public ValidationResult ValidateBackPpsn(string expectedPpsn, OcrResult ocr)
    {
        if (!ocr.Ok) return new ValidationResult(false, ocr.Message, null, ocr.Confidence, ocr.Text);

        var expected = NormalizePpsn(expectedPpsn);
        var extracted = ocr.ExtractedPpsn == null ? null : NormalizePpsn(ocr.ExtractedPpsn);

        if (string.IsNullOrWhiteSpace(expected))
            return new ValidationResult(false, "Expected PPSN is empty.", null, ocr.Confidence, ocr.Text);

        if (string.IsNullOrWhiteSpace(extracted))
            return new ValidationResult(false, "PPSN not detected on PSC Back.", null, ocr.Confidence, ocr.Text);

        if (!string.Equals(expected, extracted, StringComparison.OrdinalIgnoreCase))
            return new ValidationResult(false, $"PPSN mismatch. Expected {expected}, OCR found {extracted}.", extracted, ocr.Confidence, ocr.Text);

        return new ValidationResult(true, "PSC Back validated: PPSN matched.", extracted, ocr.Confidence, ocr.Text);
    }

    private static string? ExtractPpsn(string text)
    {
        var m = Regex.Match(text.ToUpperInvariant(), @"\b\d{7}[A-Z]{1,2}\b");
        return m.Success ? m.Value : null;
    }

    private static string? ExtractNameLine(string text)
    {
        var lines = (text ?? "").Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.Trim())
            .Where(l => l.Length >= 4)
            .ToList();

        if (lines.Count == 0) return null;

        string? best = null;
        var bestScore = -1;

        foreach (var l in lines)
        {
            var s = Regex.Replace(l, @"[^A-Za-z\s'\-]", " ").Trim();
            var words = s.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (words.Length < 2 || words.Length > 6) continue;

            var score = words.Count(w => w.Length >= 2);
            if (score > bestScore)
            {
                bestScore = score;
                best = string.Join(' ', words);
            }
        }

        return best;
    }

    private static List<string> BuildNameTokens(string first, string middle, string last)
    {
        var tokens = new List<string>();

        void add(string s)
        {
            s = (s ?? "").Trim();
            if (s.Length == 0) return;
            tokens.Add(NormalizeForMatch(s));
        }

        add(first);
        add(middle);
        add(last);

        return tokens.Where(t => t.Length > 0).Distinct().ToList();
    }

    private static string NormalizeForMatch(string s)
    {
        s = (s ?? "").ToUpperInvariant();
        s = Regex.Replace(s, @"[^A-Z0-9]+", " ");
        s = Regex.Replace(s, @"\s+", " ").Trim();
        return s;
    }

    private static string NormalizePpsn(string s)
    {
        s = (s ?? "").ToUpperInvariant();
        s = Regex.Replace(s, @"[^A-Z0-9]", "");
        return s.Trim();
    }
}

public sealed record OcrResult(
    bool Ok,
    string Message,
    string Text,
    float Confidence,
    string? ExtractedName,
    string? ExtractedPpsn
);

public sealed record ValidationResult(
    bool Valid,
    string Message,
    string? ExtractedValue,
    float Confidence,
    string OcrText
);