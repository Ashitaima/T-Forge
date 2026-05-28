namespace Computational_Practice.Exceptions
{
    public class BusinessLogicException : Exception
    {
        public BusinessLogicException(string message) : base(message)
        {
        }

        public BusinessLogicException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

    public class EntityNotFoundException : Exception
    {
        public EntityNotFoundException(string entityName, object key)
            : base($"{entityName} з ключем '{key}' не знайдено")
        {
        }

        public EntityNotFoundException(string message) : base(message)
        {
        }
    }

    public class ValidationException : Exception
    {
        public Dictionary<string, string[]> Errors { get; }

        public ValidationException(Dictionary<string, string[]> errors)
            : base("Помилки валідації")
        {
            Errors = errors;
        }

        public ValidationException(string field, string error)
            : base("Помилки валідації")
        {
            Errors = new Dictionary<string, string[]>
            {
                { field, new[] { error } }
            };
        }
    }
}
