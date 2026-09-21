namespace Identity.Sso.Domain.Exceptions;

public class BusinessRuleException : Exception
{
    public string ErrorCode { get; }

    public object[] Args { get; }

    public BusinessRuleException(string errorCode, params object[] args)
        : base(errorCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(errorCode);

        ErrorCode = errorCode;
        Args = args.Length == 0 ? [] : [.. args];
    }
}
