using Firmeza.Core.Entities;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Firmeza.Api.Services;

public class PdfReceiptService
{
    public byte[] GenerateReceiptBytes(Sale sale)
    {
        var subtotal = sale.Total;
        var iva = Math.Round(subtotal * 0.19m, 2);
        var grandTotal = subtotal + iva;

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Arial"));

                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("FIRMEZA")
                                .FontSize(26).Bold()
                                .FontColor(Color.FromHex("#ca8a04"));
                            c.Item().Text("Construction Materials")
                                .FontSize(11).FontColor(Color.FromHex("#64748b"));
                        });
                        row.ConstantItem(160).Column(c =>
                        {
                            c.Item().AlignRight().Text("SALES RECEIPT")
                                .FontSize(14).Bold().FontColor(Color.FromHex("#1e293b"));
                            c.Item().AlignRight().Text($"# {sale.Id:D6}")
                                .FontSize(13).FontColor(Color.FromHex("#ca8a04"));
                            c.Item().AlignRight().Text($"Date: {sale.SaleDate:MM/dd/yyyy}")
                                .FontSize(10).FontColor(Color.FromHex("#64748b"));
                        });
                    });
                    col.Item().PaddingTop(8).LineHorizontal(1.5f).LineColor(Color.FromHex("#ca8a04"));
                });

                page.Content().PaddingTop(16).Column(col =>
                {
                    col.Item().Background(Color.FromHex("#f8fafc")).Padding(12).Column(clientCol =>
                    {
                        clientCol.Item().Text("CUSTOMER INFORMATION")
                            .FontSize(9).Bold().FontColor(Color.FromHex("#64748b"))
                            .LetterSpacing(1);
                        clientCol.Item().PaddingTop(6).Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text($"Name: {sale.Client?.FullName ?? "N/A"}").SemiBold();
                                c.Item().Text($"ID: {sale.Client?.DocumentType ?? ""} {sale.Client?.DocumentNumber ?? ""}");
                            });
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text($"Email: {sale.Client?.Email ?? "N/A"}");
                                c.Item().Text($"Phone: {sale.Client?.Phone ?? ""}");
                            });
                        });
                    });

                    col.Item().PaddingTop(16);

                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text($"Status: {sale.Status}").FontSize(10);
                    });

                    col.Item().PaddingTop(12);

                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn(4);
                            cols.RelativeColumn(1);
                            cols.RelativeColumn(2);
                            cols.RelativeColumn(2);
                        });

                        static IContainer HeaderCell(IContainer c) =>
                            c.Background(Color.FromHex("#1e293b"))
                             .Padding(8)
                             .DefaultTextStyle(x => x.FontColor(Colors.White).Bold().FontSize(10));

                        table.Header(header =>
                        {
                            header.Cell().Element(HeaderCell).Text("Product");
                            header.Cell().Element(HeaderCell).AlignCenter().Text("Qty.");
                            header.Cell().Element(HeaderCell).AlignRight().Text("Unit Price");
                            header.Cell().Element(HeaderCell).AlignRight().Text("Subtotal");
                        });

                        bool odd = true;
                        foreach (var detail in sale.Details)
                        {
                            var bg = odd ? Color.FromHex("#ffffff") : Color.FromHex("#f8fafc");
                            odd = !odd;

                            static IContainer DataCell(IContainer c, Color bg) =>
                                c.Background(bg).Padding(8);

                            table.Cell().Element(c => DataCell(c, bg)).Text(detail.Product?.Name ?? $"Product #{detail.ProductId}");
                            table.Cell().Element(c => DataCell(c, bg)).AlignCenter().Text(detail.Quantity.ToString());
                            table.Cell().Element(c => DataCell(c, bg)).AlignRight().Text($"${detail.UnitPrice:N2}");
                            table.Cell().Element(c => DataCell(c, bg)).AlignRight().Text($"${detail.Subtotal:N2}");
                        }
                    });

                    col.Item().PaddingTop(12);

                    col.Item().AlignRight().Column(totals =>
                    {
                        totals.Item().Row(r =>
                        {
                            r.ConstantItem(120).Text("Subtotal:");
                            r.ConstantItem(100).AlignRight().Text($"${subtotal:N2}");
                        });
                        totals.Item().Row(r =>
                        {
                            r.ConstantItem(120).Text("VAT (19%):");
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

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Firmeza © — Construction Materials | ")
                        .FontSize(9).FontColor(Color.FromHex("#94a3b8"));
                    text.CurrentPageNumber().FontSize(9).FontColor(Color.FromHex("#94a3b8"));
                    text.Span(" / ").FontSize(9).FontColor(Color.FromHex("#94a3b8"));
                    text.TotalPages().FontSize(9).FontColor(Color.FromHex("#94a3b8"));
                });
            });
        });

        return document.GeneratePdf();
    }
}
