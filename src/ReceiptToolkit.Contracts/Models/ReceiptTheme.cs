namespace ReceiptToolkit.Contracts;

/// <summary>Color theme tokens used to render the receipt UI.</summary>
public sealed record ReceiptTheme
{
    /// <summary>Background color of the receipt paper (hex).</summary>
    public string? PaperColor { get; init; }

    /// <summary>Primary text color (hex).</summary>
    public string? TextColor { get; init; }

    /// <summary>Secondary or muted text color used for labels and metadata (hex).</summary>
    public string? MutedTextColor { get; init; }

    /// <summary>Accent color used for headings and highlights (hex).</summary>
    public string? AccentColor { get; init; }

    /// <summary>Color used for divider lines between sections (hex).</summary>
    public string? DividerColor { get; init; }

    /// <summary>Highlight background color for totals or special rows (hex).</summary>
    public string? HighlightColor { get; init; }

    /// <summary>Overall page or container background color (hex).</summary>
    public string? BackgroundColor { get; init; }
}
