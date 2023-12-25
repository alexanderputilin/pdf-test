namespace Pdf.Processor.Services;

public interface IPdfConvertor
{
    public Task<byte[]> GetPdf(string content);
}