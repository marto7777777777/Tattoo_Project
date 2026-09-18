namespace Tattoo_Project.Services;

public sealed class DomainConflictException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}
