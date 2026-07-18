namespace RepairShop.Domain.Common;

/// <summary>
/// Signals a violated business rule (e.g. an illegal status transition).
/// Deliberately distinct from technical failures: the API middleware maps this
/// to HTTP 400, never to 500.
/// </summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message)
    {
    }
}
