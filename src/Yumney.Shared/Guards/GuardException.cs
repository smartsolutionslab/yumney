namespace Yumney.Shared.Guards;

public class GuardException : ArgumentException
{
    public GuardException(string message)
        : base(message)
    {
    }

    public GuardException(string message, string paramName)
        : base(message, paramName)
    {
    }
}
