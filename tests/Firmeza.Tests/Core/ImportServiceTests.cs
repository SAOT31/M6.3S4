using Firmeza.Core.Services;

namespace Firmeza.Tests.Core;

public class ImportServiceTests
{
    private readonly ImportParserService _parser = new();

    // Productos

    [Fact]
    public void ParseProductRow_DatosValidos_RetornaProducto()
    {
        var fila = new Dictionary<string, string>
        {
            { "ProductName", "Cemento Gris 50kg" },
            { "Category",    "Cemento"            },
            { "Unit",        "bolsa"              },
            { "Price",       "45.50"              },
            { "Stock",       "200"                },
        };

        var (producto, error) = _parser.ParseProductRow(fila);

        Assert.Null(error);
        Assert.NotNull(producto);
        Assert.Equal("Cemento Gris 50kg", producto!.Name);
        Assert.Equal(45.50m, producto.Price);
        Assert.Equal(200, producto.Stock);
        Assert.Equal("Cemento", producto.Category);
    }

    [Fact]
    public void ParseProductRow_NombreAusente_RetornaError()
    {
        var fila = new Dictionary<string, string> { { "Price", "10" } };

        var (producto, error) = _parser.ParseProductRow(fila);

        Assert.Null(producto);
        Assert.NotNull(error);
        Assert.Contains("Name", error!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ParseProductRow_PrecioInvalido_RetornaError()
    {
        var fila = new Dictionary<string, string>
        {
            { "ProductName", "Varilla 3/8"  },
            { "Price",       "no_es_numero" },
        };

        var (producto, error) = _parser.ParseProductRow(fila);

        Assert.Null(producto);
        Assert.NotNull(error);
        Assert.Contains("Price", error!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ParseProductRow_ColumnasEnEspanol_RetornaProducto()
    {
        var fila = new Dictionary<string, string>
        {
            { "Producto", "Pintura Blanca 1L" },
            { "Precio",   "12.00"             },
            { "Stock",    "50"                },
        };

        var (producto, error) = _parser.ParseProductRow(fila);

        Assert.Null(error);
        Assert.NotNull(producto);
        Assert.Equal("Pintura Blanca 1L", producto!.Name);
        Assert.Equal(12.00m, producto.Price);
    }

    // Clientes

    [Fact]
    public void ParseClientRow_DatosValidos_RetornaCliente()
    {
        var fila = new Dictionary<string, string>
        {
            { "ClientFirstName", "Juan"             },
            { "ClientLastName",  "Pérez"            },
            { "DocumentNumber",  "123456789"        },
            { "DocumentType",    "CC"               },
            { "Email",           "juan@example.com" },
            { "Age",             "35"               },
        };

        var (cliente, error) = _parser.ParseClientRow(fila);

        Assert.Null(error);
        Assert.NotNull(cliente);
        Assert.Equal("Juan",      cliente!.FirstName);
        Assert.Equal("Pérez",     cliente.LastName);
        Assert.Equal("123456789", cliente.DocumentNumber);
        Assert.Equal(35,          cliente.Age);
    }

    [Fact]
    public void ParseClientRow_NumeroDocumentoAusente_RetornaError()
    {
        var fila = new Dictionary<string, string>
        {
            { "ClientFirstName", "María"  },
            { "ClientLastName",  "García" },
        };

        var (cliente, error) = _parser.ParseClientRow(fila);

        Assert.Null(cliente);
        Assert.NotNull(error);
        Assert.Contains("DocumentNumber", error!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ParseClientRow_NombreAusente_RetornaError()
    {
        var fila = new Dictionary<string, string>
        {
            { "DocumentNumber", "987654321" },
        };

        var (cliente, error) = _parser.ParseClientRow(fila);

        Assert.Null(cliente);
        Assert.NotNull(error);
        Assert.Contains("FirstName", error!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ParseSaleRow_DatosValidos_RetornaDatosDeVenta()
    {
        var fila = new Dictionary<string, string>
        {
            { "Quantity",  "5"          },
            { "SaleDate",  "2026-05-23" },
            { "Status",    "Completed"  },
        };

        var (qty, saleDate, status, error) = _parser.ParseSaleRow(fila);

        Assert.Null(error);
        Assert.NotNull(qty);
        Assert.Equal(5, qty!.Value);
        Assert.NotNull(saleDate);
        Assert.Equal(new DateTime(2026, 5, 23), saleDate!.Value.Date);
        Assert.Equal("Completed", status);
    }

    [Fact]
    public void ParseSaleRow_CantidadInvalida_RetornaError()
    {
        var fila = new Dictionary<string, string>
        {
            { "Quantity", "cero" },
        };

        var (qty, saleDate, status, error) = _parser.ParseSaleRow(fila);

        Assert.Null(qty);
        Assert.NotNull(error);
        Assert.Contains("Quantity", error!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ParseSaleRow_SinDatosDeVenta_RetornaTodoNulo()
    {
        var fila = new Dictionary<string, string>
        {
            { "ProductName", "Cemento" }
        };

        var (qty, saleDate, status, error) = _parser.ParseSaleRow(fila);

        Assert.Null(qty);
        Assert.Null(saleDate);
        Assert.Null(status);
        Assert.Null(error);
    }
}
