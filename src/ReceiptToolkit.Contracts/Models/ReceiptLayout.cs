namespace ReceiptToolkit.Contracts;

/// <summary>Layout and spacing configuration for the rendered receipt.</summary>
public sealed record ReceiptLayout
{
    /// <summary>Total width of the receipt in pixels.</summary>
    public int ReceiptWidth { get; init; }

    /// <summary>Inner padding around receipt content in pixels.</summary>
    public int Padding { get; init; }

    /// <summary>Vertical gap between receipt sections in pixels.</summary>
    public int SectionGap { get; init; }

    /// <summary>Corner border radius of the receipt container in pixels.</summary>
    public int BorderRadius { get; init; }

    /// <summary>Whether to render a drop shadow behind the receipt.</summary>
    public bool ShowShadow { get; init; }

    /// <summary>Whether to render a perforated-edge decoration at the bottom.</summary>
    public bool ShowPerforatedBottom { get; init; }

    /// <summary>Style of section dividers (e.g. "solid", "dashed").</summary>
    public string? DividerStyle { get; init; }
}
