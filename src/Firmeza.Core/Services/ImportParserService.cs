using Firmeza.Core.Entities;

namespace Firmeza.Core.Services;

// Servicio para normalizar filas de Excel desnormalizadas en entidades del dominio.
public class ImportParserService
{
    // Mapeo flexible: nombre de columna (sin espacios/guiones) → nombre canónico
    private static readonly Dictionary<string, string> ProductColumnMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "productname",    "Name"        }, { "nombreproducto", "Name"        },
        { "producto",       "Name"        }, { "name",           "Name"        },
        { "category",       "Category"    }, { "categoria",      "Category"    },
        { "unit",           "Unit"        }, { "unidad",         "Unit"        },
        { "price",          "Price"       }, { "precio",         "Price"       },
        { "stock",          "Stock"       }, { "existencias",    "Stock"       },
        { "description",    "Description" }, { "descripcion",    "Description" },
    };

    private static readonly Dictionary<string, string> ClientColumnMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "clientfirstname", "FirstName"      }, { "nombrecliente",  "FirstName"      },
        { "nombre",          "FirstName"      }, { "firstname",      "FirstName"      },
        { "clientlastname",  "LastName"       }, { "apellidocliente","LastName"       },
        { "apellido",        "LastName"       }, { "lastname",       "LastName"       },
        { "documentnumber",  "DocumentNumber" }, { "documento",      "DocumentNumber" },
        { "cedula",          "DocumentNumber" }, { "documenttype",   "DocumentType"   },
        { "tipodocumento",   "DocumentType"   }, { "email",          "Email"          },
        { "correo",          "Email"          }, { "phone",          "Phone"          },
        { "telefono",        "Phone"          }, { "address",        "Address"        },
        { "direccion",       "Address"        }, { "age",            "Age"            },
        { "edad",            "Age"            },
    };

    // Construye un Product a partir de una fila del Excel.
    // Devuelve null en product si la fila tiene errores de validación.
    public (Product? product, string? error) ParseProductRow(Dictionary<string, string> row)
    {
        var m = MapRow(row, ProductColumnMap);

        if (!m.TryGetValue("Name", out var name) || string.IsNullOrWhiteSpace(name))
            return (null, "Fila de producto sin 'Name' (ProductName / Producto / Name).");

        if (!m.TryGetValue("Price", out var priceStr) ||
            !decimal.TryParse(priceStr,
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var price) || price < 0)
            return (null, $"Producto '{name}': 'Price' inválido o ausente (valor: '{priceStr}').");

        m.TryGetValue("Category",    out var category);
        m.TryGetValue("Unit",        out var unit);
        m.TryGetValue("Description", out var description);
        int.TryParse(m.GetValueOrDefault("Stock", "0"), out var stock);

        return (new Product
        {
            Name        = name.Trim(),
            Category    = category?.Trim()    ?? string.Empty,
            Unit        = unit?.Trim()        ?? string.Empty,
            Description = description?.Trim() ?? string.Empty,
            Price       = price,
            Stock       = stock,
        }, null);
    }

    // Construye un Client a partir de una fila del Excel.
    // Devuelve null en client si la fila tiene errores de validación.
    public (Client? client, string? error) ParseClientRow(Dictionary<string, string> row)
    {
        var m = MapRow(row, ClientColumnMap);

        if (!m.TryGetValue("FirstName", out var firstName) || string.IsNullOrWhiteSpace(firstName))
            return (null, "Fila de cliente sin 'FirstName' (ClientFirstName / Nombre / FirstName).");

        if (!m.TryGetValue("DocumentNumber", out var docNum) || string.IsNullOrWhiteSpace(docNum))
            return (null, $"Cliente '{firstName}': 'DocumentNumber' ausente (Documento / Cedula).");

        m.TryGetValue("LastName",     out var lastName);
        m.TryGetValue("DocumentType", out var docType);
        m.TryGetValue("Email",        out var email);
        m.TryGetValue("Phone",        out var phone);
        m.TryGetValue("Address",      out var address);
        int.TryParse(m.GetValueOrDefault("Age", "0"), out var age);

        return (new Client
        {
            FirstName      = firstName.Trim(),
            LastName       = lastName?.Trim()  ?? string.Empty,
            DocumentNumber = docNum.Trim(),
            DocumentType   = docType?.Trim()   ?? "CC",
            Email          = email?.Trim()     ?? string.Empty,
            Phone          = phone?.Trim()     ?? string.Empty,
            Address        = address?.Trim()   ?? string.Empty,
            Age            = age,
        }, null);
    }

    private static readonly Dictionary<string, string> SaleColumnMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "quantity",     "Quantity" },
        { "cantidad",     "Quantity" },
        { "cant",         "Quantity" },
        { "qty",          "Quantity" },
        { "saledate",     "SaleDate" },
        { "fechaventa",   "SaleDate" },
        { "fecha",        "SaleDate" },
        { "date",         "SaleDate" },
        { "status",       "Status"   },
        { "estado",       "Status"   },
    };

    public (int? quantity, DateTime? saleDate, string? status, string? error) ParseSaleRow(Dictionary<string, string> row)
    {
        var m = MapRow(row, SaleColumnMap);

        if (!m.TryGetValue("Quantity", out var qtyStr) || string.IsNullOrWhiteSpace(qtyStr))
            return (null, null, null, null);

        if (!int.TryParse(qtyStr, out var quantity) || quantity <= 0)
            return (null, null, null, $"Venta: 'Quantity' inválido o ausente (valor: '{qtyStr}').");

        DateTime? saleDate = null;
        if (m.TryGetValue("SaleDate", out var dateStr) && !string.IsNullOrWhiteSpace(dateStr))
        {
            if (DateTime.TryParse(dateStr, System.Globalization.CultureInfo.InvariantCulture, out var parsedDate))
            {
                saleDate = parsedDate;
            }
            else if (DateTime.TryParse(dateStr, out var parsedDateLocal))
            {
                saleDate = parsedDateLocal;
            }
        }

        m.TryGetValue("Status", out var status);

        return (quantity, saleDate, status?.Trim(), null);
    }

    // Normaliza claves: quita espacios, guiones y guiones bajos; luego busca en el mapa
    private static Dictionary<string, string> MapRow(
        Dictionary<string, string> raw,
        Dictionary<string, string> columnMap)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in raw)
        {
            var normalKey = key.Replace(" ", "").Replace("_", "").Replace("-", "");
            if (columnMap.TryGetValue(normalKey, out var mappedKey))
                result[mappedKey] = value;
        }
        return result;
    }
}

