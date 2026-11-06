namespace RepairShop.Application.Common;

/// <summary>
/// The request collides with existing data, e.g. a license plate that is already
/// registered. Mapped to HTTP 409 by the API middleware: unlike a
/// <c>DomainException</c>, the request itself is well-formed and would succeed
/// against different data.
/// </summary>
public class ConflictException(string message) : Exception(message);
