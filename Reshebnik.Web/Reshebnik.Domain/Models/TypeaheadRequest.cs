namespace Reshebnik.Domain.Models;

public readonly record struct TypeaheadRequest(string? Query, int? Page);
