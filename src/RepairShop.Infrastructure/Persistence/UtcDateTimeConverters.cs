using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace RepairShop.Infrastructure.Persistence;

/// <summary>
/// Every timestamp in this model is UTC (the <c>…Utc</c> suffix is the contract),
/// but SQL Server's <c>datetime2</c> stores no offset, so EF Core reads values back
/// as <see cref="DateTimeKind.Unspecified"/>. System.Text.Json then writes them
/// without a trailing <c>Z</c>, and a browser parses such a string as local time —
/// shifting every displayed time by the viewer's offset.
/// <para>
/// These converters stamp <see cref="DateTimeKind.Utc"/> on the way out of the
/// database and normalise to UTC on the way in. Registered as a convention in
/// <see cref="RepairShopDbContext"/>, so new DateTime properties are covered
/// without anyone having to remember it. The column type is unchanged: no migration.
/// </para>
/// </summary>
internal sealed class UtcDateTimeConverter() : ValueConverter<DateTime, DateTime>(
    value => ToUtc(value),
    value => DateTime.SpecifyKind(value, DateTimeKind.Utc))
{
    // Local values are converted; Unspecified ones are taken to be UTC already,
    // which is what every writer in this codebase passes (IClock.UtcNow).
    internal static DateTime ToUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };
}

/// <inheritdoc cref="UtcDateTimeConverter"/>
internal sealed class NullableUtcDateTimeConverter() : ValueConverter<DateTime?, DateTime?>(
    value => value.HasValue ? UtcDateTimeConverter.ToUtc(value.Value) : value,
    value => value.HasValue ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc) : value);
