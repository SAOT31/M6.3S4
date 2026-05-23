using Firmeza.Core.Entities;
using Firmeza.Core.Services;
using Firmeza.Infrastructure.Data;
using Firmeza.Infrastructure.Identity;
using Firmeza.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;

namespace Firmeza.Web.Pages.Import;

[Authorize(Roles = "Admin")]
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly ImportParserService  _parser;
    private readonly PdfReceiptService    _pdf;
    private readonly UserManager<ApplicationUser> _userManager;

    public IndexModel(
        ApplicationDbContext context,
        ImportParserService parser,
        PdfReceiptService pdf,
        UserManager<ApplicationUser> userManager)
    {
        _context     = context;
        _parser      = parser;
        _pdf         = pdf;
        _userManager = userManager;
    }

    public List<string> ImportLog       { get; set; } = [];
    public int ProductsInserted { get; set; }
    public int ProductsUpdated  { get; set; }
    public int ClientsInserted  { get; set; }
    public int ClientsUpdated   { get; set; }
    public int SalesInserted    { get; set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync(IFormFile? file)
    {
        if (file is null || file.Length == 0)
        {
            ImportLog.Add("✗ No file received. Please select a .xlsx file.");
            return Page();
        }

        if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            ImportLog.Add("✗ Only .xlsx files are supported.");
            return Page();
        }

        ExcelPackage.License.SetNonCommercialOrganization("Firmeza");

        using var stream  = new MemoryStream();
        await file.CopyToAsync(stream);
        using var package = new ExcelPackage(stream);

        var sheet = package.Workbook.Worksheets.FirstOrDefault();
        if (sheet is null || sheet.Dimension is null)
        {
            ImportLog.Add("✗ The file has no data or the first sheet is empty.");
            return Page();
        }

        int colCount = sheet.Dimension.End.Column;
        int rowCount = sheet.Dimension.End.Row;

        var headers = new Dictionary<int, string>();
        for (int c = 1; c <= colCount; c++)
        {
            var h = sheet.Cells[1, c].Text?.Trim();
            if (!string.IsNullOrEmpty(h))
                headers[c] = h;
        }

        ImportLog.Add($"─── File: {file.FileName}  |  Rows: {rowCount - 1}  |  Columns: {headers.Count} ───");

        var existingProducts = await _context.Products.ToDictionaryAsync(p => p.Name.ToLower());
        var existingClients  = await _context.Clients.ToDictionaryAsync(c => c.DocumentNumber);
        var newSales         = new List<Sale>();

        for (int r = 2; r <= rowCount; r++)
        {
            var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var (col, header) in headers)
                row[header] = sheet.Cells[r, col].Text?.Trim() ?? string.Empty;

            if (row.Values.All(string.IsNullOrWhiteSpace)) continue;

            var (product, productError) = _parser.ParseProductRow(row);
            Product? resolvedProduct = null;
            if (product != null)
            {
                var key = product.Name.ToLower();
                if (existingProducts.TryGetValue(key, out var existing))
                {
                    existing.Category    = product.Category;
                    existing.Unit        = product.Unit;
                    existing.Description = product.Description;
                    existing.Price       = product.Price;
                    existing.Stock       = product.Stock;
                    existing.UpdatedAt   = DateTime.UtcNow;
                    ProductsUpdated++;
                    ImportLog.Add($"✓ Row {r}: Product UPDATED → '{product.Name}'");
                    resolvedProduct = existing;
                }
                else
                {
                    _context.Products.Add(product);
                    existingProducts[key] = product;
                    ProductsInserted++;
                    ImportLog.Add($"✓ Row {r}: Product INSERTED → '{product.Name}'");
                    resolvedProduct = product;
                }
            }
            else
            {
                if (row.TryGetValue("ProductName", out var pName) ||
                    row.TryGetValue("Producto",    out pName) ||
                    row.TryGetValue("Name",        out pName))
                {
                    var cleanProdName = pName?.Trim();
                    if (!string.IsNullOrEmpty(cleanProdName))
                    {
                        existingProducts.TryGetValue(cleanProdName.ToLower(), out resolvedProduct);
                    }
                }
            }

            var (client, clientError) = _parser.ParseClientRow(row);
            Client? resolvedClient = null;
            if (client != null)
            {
                if (existingClients.TryGetValue(client.DocumentNumber, out var existingClient))
                {
                    existingClient.FirstName  = client.FirstName;
                    existingClient.LastName   = client.LastName;
                    existingClient.Email      = client.Email;
                    existingClient.Phone      = client.Phone;
                    existingClient.Address    = client.Address;
                    existingClient.Age        = client.Age;
                    ClientsUpdated++;
                    ImportLog.Add($"✓ Row {r}: Client UPDATED  → '{client.FullName}' ({client.DocumentNumber})");
                    resolvedClient = existingClient;
                }
                else
                {
                    _context.Clients.Add(client);
                    existingClients[client.DocumentNumber] = client;
                    ClientsInserted++;
                    ImportLog.Add($"✓ Row {r}: Client INSERTED → '{client.FullName}' ({client.DocumentNumber})");
                    resolvedClient = client;
                }
            }
            else
            {
                if (row.TryGetValue("DocumentNumber", out var dNum) ||
                    row.TryGetValue("Documento",      out dNum) ||
                    row.TryGetValue("Cedula",         out dNum))
                {
                    var cleanDocNum = dNum?.Trim();
                    if (!string.IsNullOrEmpty(cleanDocNum))
                    {
                        existingClients.TryGetValue(cleanDocNum, out resolvedClient);
                    }
                }
            }

            var (qty, saleDate, status, saleError) = _parser.ParseSaleRow(row);
            if (saleError != null)
            {
                ImportLog.Add($"✗ Row {r}: Venta omitida — {saleError}");
            }
            else if (qty.HasValue)
            {
                if (resolvedProduct is null)
                {
                    ImportLog.Add($"✗ Row {r}: Venta omitida — No se especificó o no se encontró el producto.");
                }
                else if (resolvedClient is null)
                {
                    ImportLog.Add($"✗ Row {r}: Venta omitida — No se especificó o no se encontró el cliente.");
                }
                else
                {
                    if (resolvedProduct.Stock < qty.Value)
                    {
                        ImportLog.Add($"✗ Row {r}: Venta omitida — Stock insuficiente para '{resolvedProduct.Name}'. Disponible: {resolvedProduct.Stock}, Solicitado: {qty.Value}");
                    }
                    else
                    {
                        resolvedProduct.Stock -= qty.Value;

                        var userId = _userManager.GetUserId(User) ?? string.Empty;
                        var sale = new Sale
                        {
                            Client   = resolvedClient,
                            UserId   = userId,
                            SaleDate = saleDate ?? DateTime.UtcNow,
                            Status   = status ?? "Completed"
                        };

                        var detail = new SaleDetail
                        {
                            Product   = resolvedProduct,
                            Quantity  = qty.Value,
                            UnitPrice = resolvedProduct.Price
                        };

                        sale.Details.Add(detail);
                        sale.Total = detail.Subtotal;

                        _context.Sales.Add(sale);
                        newSales.Add(sale);
                        SalesInserted++;
                        ImportLog.Add($"✓ Row {r}: Sale REGISTERED → '{resolvedClient.FullName}' compró {qty.Value}x '{resolvedProduct.Name}'");
                    }
                }
            }

            if (product is null && client is null && !qty.HasValue)
            {
                var info = productError ?? clientError ?? "Could not identify row as product, client or sale.";
                ImportLog.Add($"✗ Row {r}: SKIPPED — {info}");
            }
        }

        await _context.SaveChangesAsync();

        foreach (var sale in newSales)
        {
            try
            {
                _pdf.GenerateReceipt(sale);
            }
            catch (Exception ex)
            {
                ImportLog.Add($"⚠ Row for sale {sale.Id}: PDF generation failed: {ex.Message}");
            }
        }

        ImportLog.Add($"─── Done: {ProductsInserted} products inserted, {ProductsUpdated} updated | " +
                      $"{ClientsInserted} clients inserted, {ClientsUpdated} updated | " +
                      $"{SalesInserted} sales registered ───");

        return Page();
    }
}

