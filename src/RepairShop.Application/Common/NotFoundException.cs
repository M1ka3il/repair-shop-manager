namespace RepairShop.Application.Common;

/// <summary>
/// A requested resource does not exist. Mapped to HTTP 404 by the API middleware.
/// Lives in Application rather than Domain because "not found" is a lookup
/// concern, not a business rule.
/// </summary>
public class NotFoundException(string entity, object key)
    : Exception($"{entity} with id {key} was not found.");
