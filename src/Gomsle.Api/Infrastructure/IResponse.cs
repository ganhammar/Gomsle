namespace Gomsle.Api.Infrastructure;

public interface IResponse
{
    IEnumerable<Error> Errors { get; set; }

    bool IsValid { get; }
}

public interface IResponse<TResult> : IResponse
{
    TResult? Result { get; set; }
}