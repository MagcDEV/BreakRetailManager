namespace BreakRetailManager.AccountsControl.Contracts;

public sealed record MovementPageDto(
    IReadOnlyList<MovementDto> Items,
    int TotalCount,
    int Page,
    int PageSize)
{
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    public bool HasNextPage => Page < TotalPages;

    public bool HasPreviousPage => Page > 1;
}
