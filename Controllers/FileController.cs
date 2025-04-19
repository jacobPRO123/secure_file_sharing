using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SecureFileSharingApp.Helpers;
using SecureFileSharingApp.Models;
using System;
using System.IO;
using System.Threading.Tasks;
using SecureFileSharingApp.Services;
using Microsoft.AspNetCore.Hosting;
using System.Net.Sockets;
using System.Net;


namespace SecureFileSharingApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FileController : ControllerBase
    {
        private readonly ILogger<FileController> _logger;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _configuration;
        private readonly MailService _mailService;  // Inject MailService
        private static readonly string[] allowedExtensions = { ".pdf", ".docx", ".xlsx", ".csv", ".txt" };

        public FileController(ILogger<FileController> logger,
                              IWebHostEnvironment env,
                              IConfiguration configuration,
                              MailService mailService)  // Add MailService to constructor
        {
            _logger = logger;
            _env = env;
            _configuration = configuration;
            _mailService = mailService;  // Assign MailService instance
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile([FromForm] FileUploadRequest request, [FromForm] string folder = "default")
        {
            var file = request.File;

            if (file == null || file.Length == 0)
                return BadRequest("No file selected.");

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension))
                return BadRequest("Unsupported file type.");

            var rootPath = Path.Combine(_env.ContentRootPath, "EncryptedFiles", folder);
            Directory.CreateDirectory(rootPath);

            // File versioning
            string fileName = Path.GetFileNameWithoutExtension(file.FileName);
            string ext = Path.GetExtension(file.FileName);
            string fullPath = Path.Combine(rootPath, file.FileName);
            int version = 1;

            while (System.IO.File.Exists(fullPath))
            {
                fullPath = Path.Combine(rootPath, $"{fileName}_v{version}{ext}");
                version++;
            }

            try
            {
                using (var stream = file.OpenReadStream())
                {
                    await FileEncryptionHelper.EncryptAndSaveFileAsync(stream, fullPath);
                }

                // Antivirus scan (if needed)
                //var isClean = await ScanWithClamAV(fullPath);
                //if (!isClean)
                //{
                //    System.IO.File.Delete(fullPath);
                //    return BadRequest("File contains a virus.");
                //}

                // Use MailService to send email
                //await _mailService.SendEmailAsync("File Uploaded",
                                                  //$"File '{Path.GetFileName(fullPath)}' was uploaded to folder '{folder}'.");

                _logger.LogInformation("File uploaded: {FileName}", file.FileName);
                return Ok("File uploaded, encrypted, and scanned successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Upload failed for file: {FileName}", file.FileName);
                return StatusCode(500, "File upload failed.");
            }
        }

        [HttpGet("download")]
        public async Task<IActionResult> DownloadFile([FromQuery] string fileName, [FromQuery] string folder = "default")
        {
            string filePath = Path.Combine(_env.ContentRootPath, "EncryptedFiles", folder, fileName);

            if (!System.IO.File.Exists(filePath))
            {
                _logger.LogWarning("File not found: {FileName}", fileName);
                return NotFound("File not found.");
            }

            try
            {
                var memoryStream = new MemoryStream();
                await FileEncryptionHelper.DecryptFileAsync(filePath, memoryStream);
                memoryStream.Position = 0;

                // Use MailService to send email on download
                //await _mailService.SendEmailAsync("File Downloaded",
                //                                  $"File '{fileName}' was downloaded from folder '{folder}'.");

                _logger.LogInformation("File downloaded: {FileName}", fileName);
                return File(memoryStream, "application/octet-stream", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Download failed for file: {FileName}", fileName);
                return StatusCode(500, "File download failed.");
            }
        }

        // Optional antivirus scan (if needed)
        private async Task<bool> ScanWithClamAV(string filePath)
        {
            try
            {
                using (var client = new TcpClient("localhost", 3310))
                using (var stream = client.GetStream())
                {
                    var writer = new StreamWriter(stream);
                    var reader = new StreamReader(stream);
                    writer.AutoFlush = true;

                    await writer.WriteLineAsync("zINSTREAM");

                    using var fileStream = System.IO.File.OpenRead(filePath);
                    var buffer = new byte[2048];
                    int bytesRead;

                    while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        var size = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(bytesRead));
                        await stream.WriteAsync(size);
                        await stream.WriteAsync(buffer, 0, bytesRead);
                    }

                    await stream.WriteAsync(BitConverter.GetBytes(0));
                    var response = await reader.ReadLineAsync();

                    return response != null && response.Contains("OK");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ClamAV scan error.");
                return false;
            }
        }
    }
}
