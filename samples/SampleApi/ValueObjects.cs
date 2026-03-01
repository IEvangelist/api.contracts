// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace SampleApi;

/// <summary>
/// Represents a monetary amount in a specific currency.
/// </summary>
/// <param name="Amount">The numeric value of the monetary amount.</param>
/// <param name="Currency">The ISO 4217 currency code (e.g., "USD", "EUR").</param>
/// <remarks>
/// <see cref="Money"/> is a value object implemented as a readonly record struct to ensure
/// immutability and value-based equality. Use this type instead of bare <see cref="decimal"/>
/// values to avoid accidental mixing of different currencies.
/// </remarks>
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
/// Represents a physical or mailing address.
/// </summary>
/// <param name="Street">The street address, including house or building number.</param>
/// <param name="City">The city or locality name.</param>
/// <param name="State">The state, province, or region.</param>
/// <param name="PostalCode">The postal or ZIP code.</param>
/// <param name="Country">The ISO 3166-1 alpha-2 country code (e.g., "US", "DE").</param>
/// <remarks>
/// This record class provides value-based equality and immutability by default,
/// making it suitable for use as an embedded value object within aggregate roots.
/// </remarks>
/// <seealso cref="Customer"/>
public record class Address(
    string Street,
    string City,
    string State,
    string PostalCode,
    string Country);

/// <summary>
/// Represents an inclusive date range with a start and end date.
/// </summary>
/// <remarks>
/// <see cref="DateRange"/> is a readonly struct that implements <see cref="IEquatable{T}"/>
/// for efficient value-based comparisons. The <see cref="Duration"/> property provides the
/// calculated time span between the two boundary dates. A <see cref="DateRange"/> is considered
/// valid when <see cref="Start"/> is less than or equal to <see cref="End"/>.
/// </remarks>
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
