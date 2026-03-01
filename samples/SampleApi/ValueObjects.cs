// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace SampleApi;

/// <summary>
/// Represents a monetary amount paired with an ISO 4217 currency code,
/// ensuring that different currencies are never accidentally mixed in arithmetic.
/// </summary>
/// <param name="Amount">The numeric value of the monetary amount.</param>
/// <param name="Currency">The three-letter ISO 4217 currency code (e.g., <c>"USD"</c>, <c>"EUR"</c>, <c>"GBP"</c>).</param>
/// <remarks>
/// <see cref="Money"/> is a value object implemented as a <see langword="readonly"/>
/// record struct to guarantee immutability and value-based equality. Using
/// <see cref="Money"/> instead of a bare <see cref="decimal"/> prevents subtle
/// bugs where values in different currencies are summed or compared.
///
/// The <see cref="Amount"/> is stored as a <see cref="decimal"/> to avoid the
/// floating-point precision issues inherent to <see cref="double"/> in financial
/// calculations.
/// </remarks>
/// <example>
/// Creating and displaying monetary values:
/// <code language="csharp">
/// var price = Money.Usd(29.99m);
/// var shipping = Money.Eur(5.50m);
///
/// Console.WriteLine(price);    // "29.99 USD"
/// Console.WriteLine(shipping); // "5.50 EUR"
/// </code>
/// </example>
/// <seealso cref="OrderItem"/>
public readonly record struct Money(decimal Amount, string Currency)
{
    /// <summary>
    /// Creates a <see cref="Money"/> value denominated in US dollars.
    /// </summary>
    /// <param name="amount">The dollar amount.</param>
    /// <returns>A new <see cref="Money"/> instance with currency set to "USD".</returns>
    public static Money Usd(decimal amount) => new(amount, "USD");

    /// <summary>
    /// Creates a <see cref="Money"/> value denominated in euros.
    /// </summary>
    /// <param name="amount">The euro amount.</param>
    /// <returns>A new <see cref="Money"/> instance with currency set to "EUR".</returns>
    public static Money Eur(decimal amount) => new(amount, "EUR");

    /// <summary>
    /// Returns a string representation in the format "100.00 USD".
    /// </summary>
    /// <returns>A formatted string of the monetary value.</returns>
    public override string ToString() => $"{Amount:F2} {Currency}";
}

/// <summary>
/// Represents a physical or mailing address, decomposed into street, city,
/// state, postal code, and country components.
/// </summary>
/// <param name="Street">The street address, including house or building number (e.g., <c>"123 Main St"</c>).</param>
/// <param name="City">The city or locality name (e.g., <c>"Seattle"</c>).</param>
/// <param name="State">The state, province, or region (e.g., <c>"WA"</c>).</param>
/// <param name="PostalCode">The postal or ZIP code (e.g., <c>"98101"</c>).</param>
/// <param name="Country">The ISO 3166-1 alpha-2 country code (e.g., <c>"US"</c>, <c>"DE"</c>).</param>
/// <remarks>
/// <see cref="Address"/> is a record class providing value-based equality and
/// immutability by default, making it suitable for use as an embedded value
/// object within aggregate roots such as <see cref="Customer"/>.
///
/// The <paramref name="Country"/> field uses ISO 3166-1 alpha-2 codes rather
/// than full country names to ensure consistent, locale-independent storage.
/// </remarks>
/// <example>
/// Creating an address for a US location:
/// <code language="csharp">
/// var address = new Address(
///     Street: "One Microsoft Way",
///     City: "Redmond",
///     State: "WA",
///     PostalCode: "98052",
///     Country: "US"
/// );
/// </code>
/// </example>
/// <seealso cref="Customer"/>
public record class Address(
    string Street,
    string City,
    string State,
    string PostalCode,
    string Country);

/// <summary>
/// Represents an inclusive date range defined by a start and end boundary,
/// with helpers for containment checks, overlap detection, and duration calculation.
/// </summary>
/// <remarks>
/// <see cref="DateRange"/> is a <see langword="readonly"/> struct that implements
/// <see cref="IEquatable{T}"/> for efficient, allocation-free value comparisons.
/// The constructor enforces the invariant that <see cref="Start"/> must be on or
/// before <see cref="End"/>, throwing <see cref="ArgumentException"/> otherwise.
///
/// Common use cases include filtering orders by date, defining subscription
/// periods, and computing business-day durations. The <see cref="Contains"/> and
/// <see cref="Overlaps"/> methods enable calendar-style range queries.
/// </remarks>
/// <example>
/// Creating a date range and testing containment:
/// <code language="csharp">
/// var q1 = new DateRange(
///     new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
///     new DateTimeOffset(2026, 3, 31, 23, 59, 59, TimeSpan.Zero)
/// );
///
/// bool inRange = q1.Contains(DateTimeOffset.UtcNow);
/// Console.WriteLine($"Q1 duration: {q1.Duration.TotalDays} days");
/// </code>
/// </example>
/// <example>
/// Checking whether two ranges overlap:
/// <code language="csharp">
/// var feb = new DateRange(
///     new DateTimeOffset(2026, 2, 1, 0, 0, 0, TimeSpan.Zero),
///     new DateTimeOffset(2026, 2, 28, 23, 59, 59, TimeSpan.Zero)
/// );
/// bool overlaps = q1.Overlaps(feb); // true
/// </code>
/// </example>
/// <seealso cref="Order.OrderDate"/>
public readonly struct DateRange : IEquatable<DateRange>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DateRange"/> struct.
    /// </summary>
    /// <param name="start">The inclusive start date of the range.</param>
    /// <param name="end">The inclusive end date of the range.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="start"/> is after <paramref name="end"/>.</exception>
    public DateRange(DateTimeOffset start, DateTimeOffset end)
    {
        if (start > end)
        {
            throw new ArgumentException("Start date must be on or before end date.", nameof(start));
        }

        Start = start;
        End = end;
    }

    /// <summary>
    /// Gets the inclusive start date of the range.
    /// </summary>
    public DateTimeOffset Start { get; }

    /// <summary>
    /// Gets the inclusive end date of the range.
    /// </summary>
    public DateTimeOffset End { get; }

    /// <summary>
    /// Gets the duration of time between <see cref="Start"/> and <see cref="End"/>.
    /// </summary>
    public TimeSpan Duration => End - Start;

    /// <summary>
    /// Determines whether the specified date falls within this range.
    /// </summary>
    /// <param name="date">The date to check.</param>
    /// <returns><see langword="true"/> if <paramref name="date"/> is within the range; otherwise, <see langword="false"/>.</returns>
    public bool Contains(DateTimeOffset date) => date >= Start && date <= End;

    /// <summary>
    /// Determines whether this range overlaps with another range.
    /// </summary>
    /// <param name="other">The other date range to test.</param>
    /// <returns><see langword="true"/> if the ranges overlap; otherwise, <see langword="false"/>.</returns>
    public bool Overlaps(DateRange other) => Start <= other.End && End >= other.Start;

    /// <inheritdoc/>
    public bool Equals(DateRange other) => Start == other.Start && End == other.End;

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is DateRange other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(Start, End);

    /// <summary>
    /// Determines whether two <see cref="DateRange"/> instances are equal.
    /// </summary>
    public static bool operator ==(DateRange left, DateRange right) => left.Equals(right);

    /// <summary>
    /// Determines whether two <see cref="DateRange"/> instances are not equal.
    /// </summary>
    public static bool operator !=(DateRange left, DateRange right) => !left.Equals(right);

    /// <summary>
    /// Returns a human-readable representation of the date range.
    /// </summary>
    public override string ToString() => $"{Start:O} – {End:O}";
}
