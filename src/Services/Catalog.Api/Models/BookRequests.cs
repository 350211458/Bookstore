using System.ComponentModel.DataAnnotations;

namespace Catalog.Api.Models;

/// <summary>Request body for POST /api/books (spec 03). Only spec-defined editable fields.</summary>
/// <remarks>
/// Validation attributes target the primary-constructor <c>param</c>s: for records, MVC requires
/// validation metadata on the constructor parameter, not the synthesized property.
/// </remarks>
public sealed record CreateBookRequest(
    [param: Required] string Title,
    string Author,
    [param: Required] string ISBN,
    [param: Range(0, double.MaxValue)] decimal Price,
    [param: Range(0, int.MaxValue)] int StockQuantity,
    string? Category);

/// <summary>Request body for PUT /api/books/{id} (spec 03). Only spec-defined editable fields.</summary>
public sealed record UpdateBookRequest(
    [param: Required] string Title,
    string Author,
    [param: Required] string ISBN,
    [param: Range(0, double.MaxValue)] decimal Price,
    [param: Range(0, int.MaxValue)] int StockQuantity,
    string? Category);

/// <summary>Request body for PATCH /api/books/{id}/stock (spec 03). Delta may not take stock below zero.</summary>
public sealed record StockAdjustRequest(int Delta);

/// <summary>Paginated envelope returned by GET /api/books (spec 03).</summary>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize);
