namespace Identity.Sso.Application.Exceptions;

public class MediatorException : Exception
{
    public MediatorException(string message) : base(message)
    {
    }

    public MediatorException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
