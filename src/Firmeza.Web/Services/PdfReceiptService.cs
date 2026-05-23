using Firmeza.Core.Entities;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Firmeza.Web.Services;

// Servicio que genera recibos en PDF con QuestPDF y los guarda en wwwroot/recibos/.
public class PdfReceiptService
{
    private readonly IWebHostEnvironment _env;

    public PdfReceiptService(IWebHostEnvironment env)
    {
        _env = env;
    }

    // Genera el recibo PDF de la venta y guarda el archivo en wwwroot/recibos/.
    // Retorna la ruta relativa, ej: "/recibos/recibo-42.pdf".
    // La venta debe venir con Client y Details.Product cargados (Include).
    public string GenerateReceipt(Sale sale)
    {
        var folder = Path.Combine(_env.WebRootPath, "recibos");
        Directory.CreateDirectory(folder);

        var fileName = $"recibo-{sale.Id}.pdf";
        var filePath = Path.Combine(folder, fileName);

        var subtotal   = sale.Total;
        var iva        = Math.Round(subtotal * 0.19m, 2);
        var grandTotal = subtotal + iva;

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Arial"));

                // Encabezado
                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("FIRMEZA")
                                .FontSize(26).Bold()
                                .FontColor(Color.FromHex("#ca8a04"));
                            c.Item().Text("Materiales de Construcción")
                                .FontSize(11).FontColor(Color.FromHex("#64748b"));
                        });
                        row.ConstantItem(160).Column(c =>
                        {
                            c.Item().AlignRight().Text("RECIBO DE VENTA")
                                .FontSize(14).Bold().FontColor(Color.FromHex("#1e293b"));
                            c.Item().AlignRight().Text($"# {sale.Id:D6}")
                                .FontSize(13).FontColor(Color.FromHex("#ca8a04"));
                            c.Item().AlignRight().Text($"Fecha: {sale.SaleDate:dd/MM/yyyy}")
                                .FontSize(10).FontColor(Color.FromHex("#64748b"));
                        });
                    });
                    col.Item().PaddingTop(8).LineHorizontal(1.5f).LineColor(Color.FromHex("#ca8a04"));
                });

                // Contenido
                page.Content().PaddingTop(16).Column(col =>
                {
                    // Datos del cliente
                    col.Item().Background(Color.FromHex("#f8fafc")).Padding(12).Column(clientCol =>
                    {
                        clientCol.Item().Text("DATOS DEL CLIENTE")
                            .FontSize(9).Bold().FontColor(Color.FromHex("#64748b"))
                            .LetterSpacing(1);
                        clientCol.Item().PaddingTop(6).Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text($"Nombre: {sale.Client.FullName}").SemiBold();
                                c.Item().Text($"Documento: {sale.Client.DocumentType} {sale.Client.DocumentNumber}");
                            });
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text($"Email: {sale.Client.Email}");
                                c.Item().Text($"Teléfono: {sale.Client.Phone}");
                            });
                        });
                    });

                    col.Item().PaddingTop(16);

                    // Estado
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text($"Estado: {sale.Status}").FontSize(10);
                    });

                    col.Item().PaddingTop(12);

                    // Tabla de productos
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn(4); // Producto
                            cols.RelativeColumn(1); // Cant
                            cols.RelativeColumn(2); // P.Unit
                            cols.RelativeColumn(2); // Subtotal
                        });

                        // Encabezado de tabla
                        static IContainer HeaderCell(IContainer c) =>
                            c.Background(Color.FromHex("#1e293b"))
                             .Padding(8)
                             .DefaultTextStyle(x => x.FontColor(Colors.White).Bold().FontSize(10));

                        table.Header(header =>
                        {
                            header.Cell().Element(HeaderCell).Text("Producto");
                            header.Cell().Element(HeaderCell).AlignCenter().Text("Cant.");
                            header.Cell().Element(HeaderCell).AlignRight().Text("P. Unit.");
                            header.Cell().Element(HeaderCell).AlignRight().Text("Subtotal");
                        });

                        // Filas de detalle
                        bool odd = true;
                        foreach (var detail in sale.Details)
                        {
                            var bg = odd ? Color.FromHex("#ffffff") : Color.FromHex("#f8fafc");
                            odd = !odd;

                            static IContainer DataCell(IContainer c, Color bg) =>
                                c.Background(bg).Padding(8);

                            table.Cell().Element(c => DataCell(c, bg)).Text(detail.Product.Name);
                            table.Cell().Element(c => DataCell(c, bg)).AlignCenter().Text(detail.Quantity.ToString());
                            table.Cell().Element(c => DataCell(c, bg)).AlignRight().Text($"${detail.UnitPrice:N2}");
                            table.Cell().Element(c => DataCell(c, bg)).AlignRight().Text($"${detail.Subtotal:N2}");
                        }
                    });

                    col.Item().PaddingTop(12);

                    // Totales
                    col.Item().AlignRight().Column(totals =>
                    {
                        totals.Item().Row(r =>
                        {
                            r.ConstantItem(120).Text("Subtotal:");
                            r.ConstantItem(100).AlignRight().Text($"${subtotal:N2}");
                        });
                        totals.Item().Row(r =>
                        {
                            r.ConstantItem(120).Text("IVA (19%):");
                            r.ConstantItem(100).AlignRight().Text($"${iva:N2}");
                        });
                        totals.Item().LineHorizontal(1).LineColor(Color.FromHex("#ca8a04"));
                        totals.Item().Row(r =>
                        {
                            r.ConstantItem(120).Text("TOTAL:").Bold().FontSize(13);
                            r.ConstantItem(100).AlignRight().Text($"${grandTotal:N2}").Bold().FontSize(13).FontColor(Color.FromHex("#ca8a04"));
                        });
                    });
                });

                // Pie de página
                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Firmeza © — Materiales de Construcción | ")
                        .FontSize(9).FontColor(Color.FromHex("#94a3b8"));
                    text.CurrentPageNumber().FontSize(9).FontColor(Color.FromHex("#94a3b8"));
                    text.Span(" / ").FontSize(9).FontColor(Color.FromHex("#94a3b8"));
                    text.TotalPages().FontSize(9).FontColor(Color.FromHex("#94a3b8"));
                });
            });
        });

        document.GeneratePdf(filePath);
        return $"/recibos/{fileName}";
    }

    // Genera el PDF en memoria y devuelve los bytes (para descarga directa sin redirigir).
    public byte[] GenerateReceiptBytes(Sale sale)
    {
        // Reutilizamos Generate para disco y leemos; o generamos en memoria
        var path = GenerateReceipt(sale);
        var full = Path.Combine(_env.WebRootPath, "recibos", $"recibo-{sale.Id}.pdf");
        return File.ReadAllBytes(full);
    }
}
