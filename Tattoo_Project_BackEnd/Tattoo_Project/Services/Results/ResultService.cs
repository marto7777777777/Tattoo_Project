namespace Tattoo_Project.Services.Results
{
    public class ResultService
    {
        public bool Success { get; }

        public string? ErrorMessage { get; }

        private ResultService(bool success, string? errorMessage)
        {
            Success = success;
            ErrorMessage = errorMessage;
        }

        public static ResultService Ok()
        {
            return new ResultService(true, null);
        }

        public static ResultService Fail(string errorMessage)
        {
            return new ResultService(false, errorMessage);
        }
    }
}