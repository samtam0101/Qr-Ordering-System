using Microsoft.AspNetCore.Mvc;

namespace AdminApp.Controllers;

[ApiController]
[Route("api/uploads")]
public class UploadController : ControllerBase
{
    [HttpPost("menu-image")]
    [DisableRequestSizeLimit]
    public async Task<IActionResult> UploadMenuImage(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file.");

        var uploadsRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
        Directory.CreateDirectory(uploadsRoot);

        var ext = Path.GetExtension(file.FileName);
        var name = $"menu_{Guid.NewGuid():N}{ext}";
        var savePath = Path.Combine(uploadsRoot, name);

        await using (var fs = System.IO.File.Create(savePath))
            await file.CopyToAsync(fs);

        var publicUrl = $"/uploads/{name}";
        return Ok(new { url = publicUrl });
    }
}
