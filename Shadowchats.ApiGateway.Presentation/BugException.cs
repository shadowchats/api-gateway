namespace Shadowchats.ApiGateway.Presentation;

public class BugException : Exception
{
    public BugException(string message) : base(message) { }
}