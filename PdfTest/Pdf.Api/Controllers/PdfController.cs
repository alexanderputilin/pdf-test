using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Pdf.Api.Services;
using Pdf.Models;

namespace Pdf.Api.Controllers;

public class PdfController : Controller
{
    private readonly ApiService _apiService;

    public PdfController(ApiService apiService)
    {
        _apiService = apiService;
    }

    [HttpPost("/api/upload")]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file.ContentType != "text/html")
        {
            return BadRequest();
        }

        using var stream = new MemoryStream();
        await file.CopyToAsync(stream);

        var result = await _apiService.SaveFile(Path.GetFileNameWithoutExtension(file.FileName), stream.ToArray());
        return Json(result);
    }

    [HttpDelete("/api/delete/{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _apiService.DeleteFile(id);
        return Ok();
    }

    [HttpGet("/api/download/{id}")]
    public async Task<IActionResult> Download(Guid id)
    {
        var fileTask = _apiService.GetFileTask(id);

        if (fileTask?.State != FileTask.EFileState.Done)
        {
            return NotFound();
        }

        var file = await _apiService.GetPdfFile(fileTask.Id);
        return File(file, "application/pdf", $"{fileTask.Name}.pdf");
    }
}