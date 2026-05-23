namespace Firmeza.Web.ViewModels;

public class DashboardViewModel
{
    public int TotalProducts { get; set; }
    public int TotalClients  { get; set; }
    public int TotalSales    { get; set; }
    public decimal TotalRevenue { get; set; }

    // Ventas recientes para mostrar en la tabla del dashboard
    public List<RecentSaleItem> RecentSales { get; set; } = [];

    // Productos con poco stock para alertar
    public List<LowStockItem> LowStockProducts { get; set; } = [];
}

public class RecentSaleItem
{
    public int    SaleId     { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public decimal Total     { get; set; }
    public string Status     { get; set; } = string.Empty;
    public DateTime Date     { get; set; }
}

public class LowStockItem
{
    public string ProductName { get; set; } = string.Empty;
    public int    Stock       { get; set; }
    public string Unit        { get; set; } = string.Empty;
}
