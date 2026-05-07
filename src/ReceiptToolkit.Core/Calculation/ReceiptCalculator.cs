using ReceiptToolkit.Contracts;
using ReceiptToolkit.Core.Currency;

namespace ReceiptToolkit.Core.Calculation;

/// <summary>
///   Pure-function calculator that derives the aggregated <see cref="ReceiptTotals"/>
///   for a <see cref="ReceiptData"/> instance from its line items.
/// </summary>
public static class ReceiptCalculator
{
    /// <summary>
    ///   Computes <see cref="ReceiptTotals.Subtotal"/>, <see cref="ReceiptTotals.TaxTotal"/>,
    ///   <see cref="ReceiptTotals.DiscountTotal"/>, and <see cref="ReceiptTotals.GrandTotal"/>
    ///   from <paramref name="data"/>'s line items, returning a new <see cref="ReceiptData"/>
    ///   with the recalculated <see cref="ReceiptData.Totals"/>.
    /// </summary>
    /// <param name="data">The receipt to calculate totals for. Not mutated.</param>
    /// <returns>
    ///   A new <see cref="ReceiptData"/> with refreshed <see cref="ReceiptData.Totals"/> when
    ///   <see cref="ReceiptOptions.AutoCalculateTotals"/> is <see langword="true"/>; otherwise
    ///   <paramref name="data"/> is returned unchanged so manually supplied totals are honoured.
    /// </returns>
    /// <remarks>
    ///   <para>
    ///     The per-line gross is <c>quantity * unitPrice - discount</c>. Per-item tax is
    ///     accumulated as a <see cref="decimal"/> running sum (<c>lineGross * (decimal)taxRate</c>)
    ///     and rounded once at the end. The grand total formula is
    ///     <c>subtotal − discountTotal + taxTotal + serviceCharge + roundingAdjustment</c>.
    ///   </para>
    ///   <para>
    ///     All rounding uses <see cref="MidpointRounding.ToEven"/> (banker's rounding) so that
    ///     exact <c>.x5</c> midpoints round to the nearest even digit (e.g. <c>1.245</c> rounds
    ///     to <c>1.24</c> while <c>1.255</c> rounds to <c>1.26</c>). The number of decimal
    ///     places comes from <see cref="CurrencyTable.TryGet(string, out CurrencyInfo?)"/> for
    ///     the configured <see cref="ReceiptOptions.Currency"/>, defaulting to <c>2</c> when
    ///     the currency is missing or unknown (including a <see langword="null"/> or empty code).
    ///   </para>
    ///   <para>
    ///     <b>Idempotence.</b> <c>CalculateTotals(CalculateTotals(d))</c> yields the same totals
    ///     as <c>CalculateTotals(d)</c>. Because <see cref="ReceiptTotals"/> exposes a single
    ///     <see cref="ReceiptTotals.DiscountTotal"/> field that must serve both as a
    ///     receipt-level seed on input <i>and</i> the summed output, the calculator decides
    ///     between the two interpretations using a fingerprint: if the input
    ///     <see cref="ReceiptTotals.Subtotal"/> equals the freshly computed subtotal (and is
    ///     non-zero), the input is treated as the prior output of this function and
    ///     <see cref="ReceiptTotals.DiscountTotal"/> is preserved verbatim (item discounts are
    ///     already folded in). Otherwise the input is treated as a fresh receipt-level seed and
    ///     the output is <c>inputTotals.DiscountTotal + Σ items[i].Discount</c>.
    ///   </para>
    ///   <para>
    ///     When <see cref="ReceiptOptions.AutoCalculateTotals"/> is <see langword="false"/> (or
    ///     <see cref="ReceiptData.Options"/> is <see langword="null"/>), the input
    ///     <paramref name="data"/> is returned unchanged so that manually supplied totals
    ///     pass through untouched.
    ///   </para>
    /// </remarks>
    public static ReceiptData CalculateTotals(ReceiptData data)
    {
        ArgumentNullException.ThrowIfNull(data);

        if (data.Options?.AutoCalculateTotals != true)
        {
            return data;
        }

        var currencyCode = data.Options?.Currency ?? string.Empty;
        // CurrencyTable.TryGet's contract guarantees `info` is non-null whenever it returns
        // true (see CurrencyTable.TryGet), so the dereference below is safe. The
        // null-forgiving (!) operator documents that contractual guarantee to the flow
        // analyser without re-checking it at runtime.
        var decimalPlaces = CurrencyTable.TryGet(currencyCode, out var info)
            ? info!.DecimalPlaces
            : 2;

        decimal Round(decimal value) => Math.Round(value, decimalPlaces, MidpointRounding.ToEven);

        var subtotalAccum = 0m;
        var taxAccum = 0m;
        var itemDiscountAccum = 0m;

        foreach (var item in data.Items)
        {
            var lineGross = (item.Quantity * item.UnitPrice) - item.Discount;
            subtotalAccum += lineGross;
            taxAccum += lineGross * (decimal)item.TaxRate;
            itemDiscountAccum += item.Discount;
        }

        var inputTotals = data.Totals;

        var subtotal = Round(subtotalAccum);
        var taxTotal = Round(taxAccum);

        // Idempotence fingerprint: when the input subtotal equals the freshly computed
        // subtotal (and is non-zero), treat the input as a prior output of this function
        // and preserve DiscountTotal verbatim — item discounts are already folded in.
        // Otherwise treat it as a fresh receipt-level seed and add the item discounts on top.
        // See <remarks> on CalculateTotals for the full contract.
        var isPriorOutput = inputTotals.Subtotal == subtotal && subtotal != 0m;
        var discountTotal = isPriorOutput
            ? Round(inputTotals.DiscountTotal)
            : Round(inputTotals.DiscountTotal + itemDiscountAccum);

        var serviceCharge = Round(inputTotals.ServiceCharge);
        var roundingAdjustment = inputTotals.RoundingAdjustment;
        var grandTotal = Round(subtotal - discountTotal + taxTotal + serviceCharge + roundingAdjustment);

        var newTotals = new ReceiptTotals
        {
            Subtotal = subtotal,
            DiscountTotal = discountTotal,
            ServiceCharge = serviceCharge,
            TaxLabel = inputTotals.TaxLabel,
            TaxTotal = taxTotal,
            RoundingAdjustment = roundingAdjustment,
            GrandTotal = grandTotal,
        };

        return data with { Totals = newTotals };
    }
}
