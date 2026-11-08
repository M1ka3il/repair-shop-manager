namespace RepairShop.Application.Common;

/// <summary>
/// The caller is known but not allowed to do this. Mapped to HTTP 403 by the API
/// middleware — distinct from a broken business rule (400): the same request
/// from someone with the right role would succeed.
/// </summary>
public class ForbiddenException(string message) : Exception(message);
