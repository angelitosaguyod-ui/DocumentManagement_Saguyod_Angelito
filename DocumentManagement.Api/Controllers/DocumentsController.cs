using DocumentManagement.Api.Data;
using Microsoft.AspNetCore.Mvc;
using DocumentManagement.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DocumentManagement.Api.Controllers
{
    [ApiController]
    [Route("api/documents")]
    public class DocumentsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly string _storagePath;
        private readonly ILogger<DocumentsController> _logger;

        private const long MaxFileSize = 10 * 1024 * 1024; // 10 MB
        private static readonly string[] AllowedExtensions = { ".pdf", ".doc", ".docx", ".txt", ".png", ".jpg", ".jpeg" };

        public DocumentsController(
            AppDbContext context,
            ILogger<DocumentsController> logger, 
            IConfiguration configuration, 
            IWebHostEnvironment env)
        {
            _context = context;
            _logger = logger;

            var folder = configuration["FileStorage:Path"] ?? "Uploads";
            _storagePath = Path.Combine(env.ContentRootPath, folder);
            Directory.CreateDirectory(_storagePath);
        }

        /* Upload Document */
        [HttpPost]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            /* 1. Validate the file */
            if (file == null || file.Length == 0)
                return BadRequest("Please upload a file.");

            if (file.Length > MaxFileSize)
                return BadRequest("File is too large. Maximum size is 10 MB.");

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(extension))
                return BadRequest("File type is not allowed.");

            /* 2. Save the file with a safe, unique name */
            var id = Guid.NewGuid();
            var storedFileName = id + extension;
            var filePath = Path.Combine(_storagePath, storedFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            /* 3. Save the metadata in the database */
            var document = new Document
            {
                Id = id,
                OriginalFileName = Path.GetFileName(file.FileName),
                StoredFileName = storedFileName,
                ContentType = file.ContentType,
                FileSize = file.Length,
                UploadedAt = DateTime.UtcNow
            };

            _context.Documents.Add(document);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch
            {
                /* If the database save fails, delete the file so nothing is left behind */
                System.IO.File.Delete(filePath);
                throw;
            }

            /* 4. Return 201 Created with the metadata */
            return Created($"/api/documents/{document.Id}", document);
        }


        /* List Documents */
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var documents = await _context.Documents
                .OrderByDescending(d => d.UploadedAt)
                .ToListAsync();

            return Ok(documents);
        }


        /* Retrieve Document Details */
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var document = await _context.Documents.FindAsync(id);

            if (document == null)
                return NotFound();

            return Ok(document);
        }


        /* Download Document */
        [HttpGet("{id}/download")]
        public async Task<IActionResult> Download(Guid id)
        {
            var document = await _context.Documents.FindAsync(id);
            if (document == null)
                return NotFound("Document not found.");

            var filePath = Path.Combine(_storagePath, document.StoredFileName);
            if (!System.IO.File.Exists(filePath))
                return NotFound("File not found in storage.");

            return PhysicalFile(filePath, document.ContentType, document.OriginalFileName);
        }


        /* Delete Document */
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var document = await _context.Documents.FindAsync(id);
            if (document == null)
                return NotFound();

            /* 1. Delete the database record first */
            _context.Documents.Remove(document);
            await _context.SaveChangesAsync();

            /* 2. Then delete the file */
            var filePath = Path.Combine(_storagePath, document.StoredFileName);
            try
            {
                if (System.IO.File.Exists(filePath))
                    System.IO.File.Delete(filePath);
            }
            catch (Exception ex)
            {
                /* The record is already gone, so log the leftover file for cleanup later */
                _logger.LogWarning(ex, "Could not delete file {FilePath}", filePath);
            }

            return NoContent();
        }
    }
}
