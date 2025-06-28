namespace Reshebnik.Domain.Models;

public readonly record struct PaginationDto<T>(IEnumerable<T> Items, int TotalCount, int TotalPages);