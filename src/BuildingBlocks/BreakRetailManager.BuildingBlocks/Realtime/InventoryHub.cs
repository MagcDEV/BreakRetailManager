using Microsoft.AspNetCore.SignalR;

namespace BreakRetailManager.BuildingBlocks.Realtime;

public sealed class InventoryHub : Hub
{
    public const string HubPath = "/hubs/inventory";

    public const string StockChangedMethod = "InventoryStockChanged";

    public Task JoinLocationGroup(Guid locationId)
    {
        return Groups.AddToGroupAsync(Context.ConnectionId, locationId.ToString());
    }

    public Task LeaveLocationGroup(Guid locationId)
    {
        return Groups.RemoveFromGroupAsync(Context.ConnectionId, locationId.ToString());
    }
}