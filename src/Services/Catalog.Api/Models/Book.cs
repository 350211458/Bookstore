namespace Catalog.Api.Models;

/// <summary>
/// A book in the catalog (spec 03). The <see cref="Category"/> and <see cref="IsDeleted"/>
/// fields are the two spec-implied additions confirmed during spec review: Category backs the
/// <c>category</c> list filter, IsDeleted backs the soft-delete requirement.
/// </summary>
public sealed class Book
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Author { get; set; } = string.Empty;

    public string ISBN { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public int StockQuantity { get; set; }

    public string? Category { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public bool IsDeleted { get; set; }
}
