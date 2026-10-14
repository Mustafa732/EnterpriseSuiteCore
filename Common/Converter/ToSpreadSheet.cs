using System.Data;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Common.Converter
{
    public class ToSpreadSheet
    {
        /// <summary>
        ///.NET 8 MVC - Controller me FileResult return karne ke liye (BEST)
        /// </summary>
        public static FileContentResult ExportToSpreadsheetResult(DataTable table, string name)
        {
            var csv = BuildCsv(table);
            var bytes = Encoding.UTF8.GetBytes(csv);
            return new FileContentResult(bytes, "text/csv")
            {
                FileDownloadName = name + ".csv"
            };
        }

        /// <summary>
        /// Agar HttpContext chahiye to ye wala
        /// </summary>
        public static async void ExportToSpreadsheet(DataTable table, string name, HttpContext context)
        {
            var csv = BuildCsv(table);
            var bytes = Encoding.UTF8.GetBytes(csv);

            context.Response.Clear();
            context.Response.ContentType = "text/csv";
            context.Response.Headers.Add("Content-Disposition", $"attachment; filename={name}.csv");
            await context.Response.Body.WriteAsync(bytes, 0, bytes.Length);
        }

        private static string BuildCsv(DataTable table)
        {
            var sb = new StringBuilder();

            // Header
            for (int i = 0; i < table.Columns.Count; i++)
            {
                sb.Append(table.Columns[i].ColumnName);
                if (i < table.Columns.Count - 1) sb.Append(",");
            }
            sb.AppendLine();

            // Rows
            foreach (DataRow row in table.Rows)
            {
                for (int i = 0; i < table.Columns.Count; i++)
                {
                    if (row[i] != null && row[i] != DBNull.Value)
                        sb.Append(row[i].ToString().Replace(",", string.Empty));
                    if (i < table.Columns.Count - 1) sb.Append(",");
                }
                sb.AppendLine();
            }

            return sb.ToString();
        }
    }
}