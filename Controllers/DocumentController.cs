using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SEN_T_PAZAR.Models;
using System.IO;

namespace SEN_T_PAZAR.Controllers;

[Authorize]
public class DocumentController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _environment;

    public DocumentController(ApplicationDbContext context, IWebHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
    }

    // GET: Document
    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var documents = await _context.Documents
            .Where(d => d.UserId == userId)
            .OrderByDescending(d => d.UploadDate)
            .ToListAsync();
        return View(documents);
    }

    // POST: Document/Upload
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(IFormFile file, DocumentType documentType)
    {
        if (file == null || file.Length == 0)
        {
            ModelState.AddModelError("file", "Please select a file to upload.");
            return RedirectToAction(nameof(Index));
        }

        // Validate file type (optional)
        var allowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png", ".doc", ".docx" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(extension))
        {
            ModelState.AddModelError("file", "Invalid file type. Allowed types: PDF, JPG, JPEG, PNG, DOC, DOCX.");
            return RedirectToAction(nameof(Index));
        }

        // Limit file size (e.g., 10MB)
        if (file.Length > 10 * 1024 * 1024)
        {
            ModelState.AddModelError("file", "File size cannot exceed 10 MB.");
            return RedirectToAction(nameof(Index));
        }

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userId == null)
        {
            return Unauthorized();
        }

        // Create uploads folder if it doesn't exist
        var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "documents");
        if (!Directory.Exists(uploadsFolder))
        {
            Directory.CreateDirectory(uploadsFolder);
        }

        // Generate a unique file name
        var fileName = $"{Guid.NewGuid()}{extension}";
        var filePath = Path.Combine(uploadsFolder, fileName);

        // Save the file
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        // Save document record to database
        var document = new Document
        {
            UserId = userId,
            DocumentType = documentType,
            FileName = file.FileName,
            FilePath = $"/uploads/documents/{fileName}"
        };

        _context.Documents.Add(document);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Document uploaded successfully.";
        return RedirectToAction(nameof(Index));
    }

    // GET: Document/Download/5
    public async Task<IActionResult> Download(int id)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var document = await _context.Documents
            .FirstOrDefaultAsync(d => d.Id == id && d.UserId == userId);

        if (document == null)
        {
            return NotFound();
        }

        var filePath = Path.Combine(_environment.WebRootPath, document.FilePath.TrimStart('/'));
        if (!System.IO.File.Exists(filePath))
        {
            return NotFound();
        }

        var memory = new MemoryStream();
        using (var stream = new FileStream(filePath, FileMode.Open))
        {
            await stream.CopyToAsync(memory);
        }
        memory.Position = 0;

        return File(memory, GetContentType(document.FilePath), document.FileName);
    }

    private string GetContentType(string path)
    {
        var extension = Path.GetExtension(path).ToLowerInvariant();
        return extension switch
        {
            ".pdf" => "application/pdf",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            _ => "application/octet-stream"
        };
    }
}