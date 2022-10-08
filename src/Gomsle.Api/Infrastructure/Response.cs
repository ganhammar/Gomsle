namespace Gomsle.Api.Infrastructure;

public class Response<T> : Response, IResponse<T>
    where T : new()
{
    public T? Result
    {
        get => IsValid ? ResultObject : default;
        set
        {
            if (value != null)
            {
                ResultObject = value;
            }
        }
    }

    private T ResultObject { get; set; } = new T();
}

public class Response : IResponse
{
    public Response()
    {
        Errors = Enumerable.Empty<Error>();
    }

    public Response(IEnumerable<Error> errors)
    {
        Errors = errors;
    }

    public bool IsValid
    {
        get => !Errors.Any();
        private set {}
    }

    public IEnumerable<Error> Errors { get; set; }
}