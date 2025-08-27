namespace LegoCatalog.App.Models;

/// <summary>
/// A Lego figure catalog entry.
/// </summary>
public class LegoFigure
{
    public string Id { get; set; } = string.Empty; // e.g. LF-0001
    public string Name { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public Category? Category { get; set; }
    public string Description { get; set; } = string.Empty;
    public string ImageFile { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; }
    public DateTime LastUpdatedUtc { get; set; }
}
