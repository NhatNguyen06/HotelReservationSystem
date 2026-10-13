namespace HotelApi.Models;

public class ServiceResult<T>
{
    public T? Data { get; private set; }
    public string? Error { get; private set; }
    public bool NotFound { get; private set; }
    public bool Success => Error == null && !NotFound;

    public static ServiceResult<T> Ok(T data) => new() { Data = data };
    public static ServiceResult<T> Fail(string error) => new() { Error = error };
    public static ServiceResult<T> Missing() => new() { NotFound = true };
}