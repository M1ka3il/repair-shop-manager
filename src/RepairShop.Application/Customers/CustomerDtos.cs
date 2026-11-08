using System.ComponentModel.DataAnnotations;

namespace RepairShop.Application.Customers;

/// <summary>A customer with their vehicles, flattened for the API.</summary>
/// <param name="Id">Customer primary key.</param>
/// <param name="Name">Person or company name.</param>
/// <param name="Email">Contact e-mail, if known.</param>
/// <param name="Phone">Contact phone number, if known.</param>
/// <param name="Vehicles">Vehicles owned by this customer.</param>
public record CustomerDto(
    int Id,
    string Name,
    string? Email,
    string? Phone,
    IReadOnlyList<VehicleDto> Vehicles);

/// <summary>A vehicle as returned by the API.</summary>
/// <param name="Id">Vehicle primary key.</param>
/// <param name="CustomerId">Owning customer.</param>
/// <param name="LicensePlate">Normalised plate, e.g. "M-AB 1234".</param>
/// <param name="Make">Manufacturer.</param>
/// <param name="Model">Model name.</param>
/// <param name="Year">Year of construction.</param>
public record VehicleDto(
    int Id,
    int CustomerId,
    string LicensePlate,
    string Make,
    string Model,
    int Year);

/// <summary>Payload for creating or editing a customer.</summary>
public class SaveCustomerRequest
{
    /// <summary>Person or company name.</summary>
    /// <example>Anna Becker</example>
    [Required]
    [StringLength(200, MinimumLength = 2)]
    public string Name { get; init; } = string.Empty;

    /// <summary>Contact e-mail; optional.</summary>
    /// <example>anna.becker@example.com</example>
    [EmailAddress]
    [StringLength(200)]
    public string? Email { get; init; }

    /// <summary>Contact phone number; optional.</summary>
    /// <example>+49 89 1234567</example>
    [StringLength(50)]
    public string? Phone { get; init; }
}

/// <summary>Payload for registering or editing a vehicle.</summary>
public class SaveVehicleRequest
{
    /// <summary>German plate; normalised on the server (trimmed, upper-cased).</summary>
    /// <example>M-AB 1234</example>
    [Required]
    [StringLength(15)]
    public string LicensePlate { get; init; } = string.Empty;

    /// <summary>Manufacturer.</summary>
    /// <example>Volkswagen</example>
    [Required]
    [StringLength(100)]
    public string Make { get; init; } = string.Empty;

    /// <summary>Model name.</summary>
    /// <example>Golf VIII</example>
    [Required]
    [StringLength(100)]
    public string Model { get; init; } = string.Empty;

    /// <summary>Year of construction.</summary>
    /// <example>2021</example>
    public int Year { get; init; }
}
