namespace ClassroomBooking.Contracts;

public sealed class ContractOperationException : Exception
{
    public string ErrorCode { get; }

    public ContractOperationException(string errorCode)
        : base(errorCode)
    {
        ErrorCode = errorCode;
    }

    public ContractOperationException(string errorCode, string message)
        : base(message)
    {
        ErrorCode = errorCode;
    }
}