namespace Tattoo_Project.Services.Results
{
    public class ResultService<T>
    {
        public bool Success { get; }

        public string? ErrorMessage { get; }

        public T? Data { get; }

        private ResultService(bool success, T? data, string? errorMessage)
        {
            Success = success;
            Data = data;
            ErrorMessage = errorMessage;
        }

        public static ResultService<T> Ok(T data)
        {
            return new ResultService<T>(true, data, null);
        }

        public static ResultService<T> Fail(string errorMessage)
        {
            return new ResultService<T>(false, default, errorMessage);
        }
    }
}