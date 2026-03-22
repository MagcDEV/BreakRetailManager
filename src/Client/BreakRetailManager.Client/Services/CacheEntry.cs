namespace BreakRetailManager.Client.Services;

internal sealed record CacheEntry<T>(T Data, DateTime ExpiresAt)
{
    public bool IsValid => DateTime.UtcNow < ExpiresAt;
}
