using System.IO;
using Api.Models;

namespace Api.Services
{
    public interface IForm1040PdfService
    {
        /// <summary>
        /// Generate a filled PDF for the given submission. The implementation must load the official
        /// bundled PDF and populate fields using server-side calculated values. Returns a MemoryStream
        /// containing the generated PDF.
        /// </summary>
        Task<Stream> GeneratePdfAsync(Form1040Submission submission);
    }
}
