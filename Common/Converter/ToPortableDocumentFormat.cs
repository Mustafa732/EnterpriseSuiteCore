using System.IO;
using Microsoft.AspNetCore.Http;

namespace Common.Converter
{
    public class ToPortableDocumentFormat
    {
        /// <summary>
        /// .NET 8 MVC version - Controller se HttpContext pass karna
        /// </summary>
        public static void ExportToPDF(Stream sourceStream, string fileName, HttpContext context)
        {
            byte[] pdfAsByteArray;
            using (var memoryStream = new MemoryStream())
            {
                sourceStream.CopyTo(memoryStream);
                pdfAsByteArray = memoryStream.ToArray();
            }

            context.Response.Clear();
            context.Response.ContentType = "application/pdf";
            context.Response.Headers.Add("Content-Disposition", $"attachment; filename={fileName}.pdf");
            context.Response.Body.WriteAsync(pdfAsByteArray, 0, pdfAsByteArray.Length);
        }

        /// <summary>
        /// Best for MVC - Controller me File() return karne ke liye
        /// </summary>
        public static byte[] GetPdfBytes(Stream sourceStream)
        {
            using (var memoryStream = new MemoryStream())
            {
                sourceStream.CopyTo(memoryStream);
                return memoryStream.ToArray();
            }
        }

        /// <summary>
        /// Controller helper - direct byte[] dega
        /// </summary>
        public static Microsoft.AspNetCore.Mvc.FileContentResult ExportToPDFResult(Stream sourceStream, string fileName)
        {
            var bytes = GetPdfBytes(sourceStream);
            return new Microsoft.AspNetCore.Mvc.FileContentResult(bytes, "application/pdf")
            {
                FileDownloadName = fileName + ".pdf"
            };
        }
    }
}