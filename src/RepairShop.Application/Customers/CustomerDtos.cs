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
