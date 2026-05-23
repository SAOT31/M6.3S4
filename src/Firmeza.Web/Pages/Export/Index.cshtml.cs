using Firmeza.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SysColor = System.Drawing.Color;

namespace Firmeza.Web.Pages.Export;

[Authorize(Roles = "Admin")]
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public IndexModel(ApplicationDbContext context) => _context = context;

    public int ProductCount { get; set; }
    public int ClientCount  { get; set; }
    public int SaleCount    { get; set; }

    public async Task OnGetAsync()
    {
        ProductCount = await _context.Products.CountAsync();
        ClientCount  = await _context.Clients.CountAsync();
        SaleCount    = await _context.Sales.CountAsync();
    }

    // Excel

    public async Task<IActionResult> OnGetProductsExcelAsync()
    {
        ExcelPackage.License.SetNonCommercialOrganization("Firmeza");
        var products = await _context.Products.OrderBy(p => p.Name).ToListAsync();

        using var pkg   = new ExcelPackage();
        var sheet = pkg.Workbook.Worksheets.Add("Products");

        string[] headers = ["ID", "Name", "Category", "Unit", "Price", "Stock", "Description", "Created At"];
        StyleHeader(sheet, headers);

        for (int i = 0; i < products.Count; i++)
        {
            var p = products[i]; int r = i + 2;
            sheet.Cells[r, 1].Value = p.Id;
            sheet.Cells[r, 2].Value = p.Name;
            sheet.Cells[r, 3].Value = p.Category;
            sheet.Cells[r, 4].Value = p.Unit;
            sheet.Cells[r, 5].Value = (double)p.Price;
            sheet.Cells[r, 6].Value = p.Stock;
            sheet.Cells[r, 7].Value = p.Description;
            sheet.Cells[r, 8].Value = p.CreatedAt.ToString("yyyy-MM-dd");
        }
        sheet.Cells.AutoFitColumns();
        return File(pkg.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "products.xlsx");
    }

    public async Task<IActionResult> OnGetClientsExcelAsync()
    {
        ExcelPackage.License.SetNonCommercialOrganization("Firmeza");
        var clients = await _context.Clients.OrderBy(c => c.LastName).ToListAsync();

        using var pkg   = new ExcelPackage();
        var sheet = pkg.Workbook.Worksheets.Add("Clients");

        string[] headers = ["ID", "First Name", "Last Name", "Doc.Type", "Document No.", "Email", "Phone", "Address", "Age"];
        StyleHeader(sheet, headers);

        for (int i = 0; i < clients.Count; i++)
        {
            var c = clients[i]; int r = i + 2;
            sheet.Cells[r, 1].Value = c.Id;
            sheet.Cells[r, 2].Value = c.FirstName;
            sheet.Cells[r, 3].Value = c.LastName;
            sheet.Cells[r, 4].Value = c.DocumentType;
            sheet.Cells[r, 5].Value = c.DocumentNumber;
            sheet.Cells[r, 6].Value = c.Email;
            sheet.Cells[r, 7].Value = c.Phone;
            sheet.Cells[r, 8].Value = c.Address;
            sheet.Cells[r, 9].Value = c.Age;
        }
        sheet.Cells.AutoFitColumns();
        return File(pkg.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "clients.xlsx");
    }

    public async Task<IActionResult> OnGetSalesExcelAsync()
    {
        ExcelPackage.License.SetNonCommercialOrganization("Firmeza");
        var sales = await _context.Sales
            .Include(s => s.Client)
            .OrderByDescending(s => s.SaleDate).ToListAsync();

        using var pkg   = new ExcelPackage();
        var sheet = pkg.Workbook.Worksheets.Add("Sales");

        string[] headers = ["ID", "Client", "Document", "Date", "Total", "Status"];
        StyleHeader(sheet, headers);

        for (int i = 0; i < sales.Count; i++)
        {
            var s = sales[i]; int r = i + 2;
            sheet.Cells[r, 1].Value = s.Id;
            sheet.Cells[r, 2].Value = s.Client.FullName;
            sheet.Cells[r, 3].Value = s.Client.DocumentNumber;
            sheet.Cells[r, 4].Value = s.SaleDate.ToString("yyyy-MM-dd");
            sheet.Cells[r, 5].Value = (double)s.Total;
            sheet.Cells[r, 6].Value = s.Status;
        }
        sheet.Cells.AutoFitColumns();
        return File(pkg.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "sales.xlsx");
    }

    // PDF

    public async Task<IActionResult> OnGetProductsPdfAsync()
    {
        QuestPDF.Settings.License = LicenseType.Community;
        var products = await _context.Products.OrderBy(p => p.Name).ToListAsync();

        var pdf = Document.Create(c => c.Page(page =>
        {
            page.Size(PageSizes.A4.Landscape());
            page.Margin(1.5f, Unit.Centimetre);
            page.DefaultTextStyle(t => t.FontSize(10));

            page.Header().Column(col =>
            {
                col.Item().Text("FIRMEZA — Products Report").Bold().FontSize(16).FontColor(Color.FromHex("#ca8a04"));
                col.Item().Text($"Generated: {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(9).FontColor(Color.FromHex("#64748b"));
                col.Item().PaddingTop(6).LineHorizontal(1).LineColor(Color.FromHex("#ca8a04"));
            });

            page.Content().PaddingTop(12).Table(table =>
            {
                table.ColumnsDefinition(cols =>
                {
                    cols.ConstantColumn(40);
                    cols.RelativeColumn(3);
                    cols.RelativeColumn(2);
                    cols.RelativeColumn(1);
                    cols.ConstantColumn(70);
                    cols.ConstantColumn(60);
                });

                static IContainer Th(IContainer c) =>
                    c.Background(Color.FromHex("#1e293b")).Padding(6)
                     .DefaultTextStyle(t => t.Bold().FontColor(Colors.White).FontSize(9));

                table.Header(h =>
                {
                    h.Cell().Element(Th).Text("ID");
                    h.Cell().Element(Th).Text("Name");
                    h.Cell().Element(Th).Text("Category");
                    h.Cell().Element(Th).Text("Unit");
                    h.Cell().Element(Th).AlignRight().Text("Price");
                    h.Cell().Element(Th).AlignRight().Text("Stock");
                });

                bool odd = true;
                foreach (var p in products)
                {
                    var bg = odd ? Color.FromHex("#ffffff") : Color.FromHex("#f8fafc");
                    odd = !odd;
                    static IContainer Td(IContainer c, QuestPDF.Infrastructure.Color bg) => c.Background(bg).Padding(6);
                    table.Cell().Element(c => Td(c, bg)).Text(p.Id.ToString());
                    table.Cell().Element(c => Td(c, bg)).Text(p.Name);
                    table.Cell().Element(c => Td(c, bg)).Text(p.Category);
                    table.Cell().Element(c => Td(c, bg)).Text(p.Unit);
                    table.Cell().Element(c => Td(c, bg)).AlignRight().Text($"${p.Price:N2}");
                    table.Cell().Element(c => Td(c, bg)).AlignRight().Text(p.Stock.ToString());
                }
            });

            page.Footer().AlignCenter().Text(t =>
            {
                t.Span($"Firmeza © {DateTime.Now.Year} | Page ").FontSize(8).FontColor(Color.FromHex("#94a3b8"));
                t.CurrentPageNumber().FontSize(8);
            });
        }));

        return File(pdf.GeneratePdf(), "application/pdf", "products.pdf");
    }

    public async Task<IActionResult> OnGetClientsPdfAsync()
    {
        QuestPDF.Settings.License = LicenseType.Community;
        var clients = await _context.Clients.OrderBy(c => c.LastName).ToListAsync();

        var pdf = Document.Create(c => c.Page(page =>
        {
            page.Size(PageSizes.A4.Landscape());
            page.Margin(1.5f, Unit.Centimetre);
            page.DefaultTextStyle(t => t.FontSize(10));

            page.Header().Column(col =>
            {
                col.Item().Text("FIRMEZA — Clients Report").Bold().FontSize(16).FontColor(Color.FromHex("#ca8a04"));
                col.Item().Text($"Generated: {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(9).FontColor(Color.FromHex("#64748b"));
                col.Item().PaddingTop(6).LineHorizontal(1).LineColor(Color.FromHex("#ca8a04"));
            });

            page.Content().PaddingTop(12).Table(table =>
            {
                table.ColumnsDefinition(cols =>
                {
                    cols.RelativeColumn(2); cols.RelativeColumn(2);
                    cols.RelativeColumn(2); cols.RelativeColumn(2);
                    cols.RelativeColumn(2); cols.ConstantColumn(50);
                });

                static IContainer Th(IContainer c) =>
                    c.Background(Color.FromHex("#1e293b")).Padding(6)
                     .DefaultTextStyle(t => t.Bold().FontColor(Colors.White).FontSize(9));

                table.Header(h =>
                {
                    h.Cell().Element(Th).Text("Full Name");
                    h.Cell().Element(Th).Text("Document");
                    h.Cell().Element(Th).Text("Email");
                    h.Cell().Element(Th).Text("Phone");
                    h.Cell().Element(Th).Text("Address");
                    h.Cell().Element(Th).AlignRight().Text("Age");
                });

                bool odd = true;
                foreach (var cl in clients)
                {
                    var bg = odd ? Color.FromHex("#ffffff") : Color.FromHex("#f8fafc");
                    odd = !odd;
                    static IContainer Td(IContainer c, QuestPDF.Infrastructure.Color bg) => c.Background(bg).Padding(6);
                    table.Cell().Element(c => Td(c, bg)).Text(cl.FullName);
                    table.Cell().Element(c => Td(c, bg)).Text($"{cl.DocumentType} {cl.DocumentNumber}");
                    table.Cell().Element(c => Td(c, bg)).Text(cl.Email);
                    table.Cell().Element(c => Td(c, bg)).Text(cl.Phone);
                    table.Cell().Element(c => Td(c, bg)).Text(cl.Address);
                    table.Cell().Element(c => Td(c, bg)).AlignRight().Text(cl.Age.ToString());
                }
            });

            page.Footer().AlignCenter().Text(t =>
            {
                t.Span($"Firmeza © {DateTime.Now.Year} | Page ").FontSize(8).FontColor(Color.FromHex("#94a3b8"));
                t.CurrentPageNumber().FontSize(8);
            });
        }));

        return File(pdf.GeneratePdf(), "application/pdf", "clients.pdf");
    }

    public async Task<IActionResult> OnGetSalesPdfAsync()
    {
        QuestPDF.Settings.License = LicenseType.Community;
        var sales = await _context.Sales
            .Include(s => s.Client)
            .OrderByDescending(s => s.SaleDate).ToListAsync();

        var pdf = Document.Create(c => c.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(1.5f, Unit.Centimetre);
            page.DefaultTextStyle(t => t.FontSize(10));

            page.Header().Column(col =>
            {
                col.Item().Text("FIRMEZA — Sales Report").Bold().FontSize(16).FontColor(Color.FromHex("#ca8a04"));
                col.Item().Text($"Generated: {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(9).FontColor(Color.FromHex("#64748b"));
                col.Item().PaddingTop(6).LineHorizontal(1).LineColor(Color.FromHex("#ca8a04"));
            });

            page.Content().PaddingTop(12).Table(table =>
            {
                table.ColumnsDefinition(cols =>
                {
                    cols.ConstantColumn(50);
                    cols.RelativeColumn(3);
                    cols.RelativeColumn(2);
                    cols.ConstantColumn(80);
                    cols.ConstantColumn(80);
                });

                static IContainer Th(IContainer c) =>
                    c.Background(Color.FromHex("#1e293b")).Padding(6)
                     .DefaultTextStyle(t => t.Bold().FontColor(Colors.White).FontSize(9));

                table.Header(h =>
                {
                    h.Cell().Element(Th).Text("# Sale");
                    h.Cell().Element(Th).Text("Client");
                    h.Cell().Element(Th).Text("Date");
                    h.Cell().Element(Th).AlignRight().Text("Total");
                    h.Cell().Element(Th).AlignCenter().Text("Status");
                });

                bool odd = true;
                foreach (var s in sales)
                {
                    var bg = odd ? Color.FromHex("#ffffff") : Color.FromHex("#f8fafc");
                    odd = !odd;
                    static IContainer Td(IContainer c, QuestPDF.Infrastructure.Color bg) => c.Background(bg).Padding(6);
                    table.Cell().Element(c => Td(c, bg)).Text($"#{s.Id}");
                    table.Cell().Element(c => Td(c, bg)).Text(s.Client.FullName);
                    table.Cell().Element(c => Td(c, bg)).Text(s.SaleDate.ToString("dd/MM/yyyy"));
                    table.Cell().Element(c => Td(c, bg)).AlignRight().Text($"${s.Total:N2}");
                    table.Cell().Element(c => Td(c, bg)).AlignCenter().Text(s.Status);
                }
            });

            page.Footer().AlignCenter().Text(t =>
            {
                t.Span($"Firmeza © {DateTime.Now.Year} | Page ").FontSize(8).FontColor(Color.FromHex("#94a3b8"));
                t.CurrentPageNumber().FontSize(8);
            });
        }));

        return File(pdf.GeneratePdf(), "application/pdf", "sales.pdf");
    }

    // Helpers

    private static void StyleHeader(ExcelWorksheet sheet, string[] headers)
    {
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = sheet.Cells[1, i + 1];
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
            cell.Style.Fill.BackgroundColor.SetColor(SysColor.FromArgb(0x1e, 0x29, 0x3b));
            cell.Style.Font.Color.SetColor(SysColor.White);
        }
    }
}
