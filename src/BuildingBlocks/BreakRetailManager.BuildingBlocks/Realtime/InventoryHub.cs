using Microsoft.AspNetCore.SignalR;

namespace BreakRetailManager.BuildingBlocks.Realtime;

public sealed class InventoryHub : Hub
{
    public const string HubPath = "/hubs/inventory";

    public const string StockChangedMethod = "InventoryStockChanged";
}