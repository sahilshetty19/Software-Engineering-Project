using Tesseract;

namespace Bank.Web.Services;

public sealed class OcrService
{
    private readonly IWebHostEnvironment _env;

    public OcrService(IWebHostEnvironment env)
    {
        _env = env;
    }

    public (string text, float confidence) ReadText(byte[] imageBytes)
    {
        var tessDataPath = Path.Combine(_env.ContentRootPath, "tessdata");
        using var engine = new TesseractEngine(tessDataPath, "eng", EngineMode.Default);

        using var pix = Pix.LoadFromMemory(imageBytes);
        using var page = engine.Process(pix);

        return (page.GetText() ?? "", page.GetMeanConfidence());
    }
}