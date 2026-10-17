namespace JassTournamentManager.Application.Common
{
    public sealed record Result<T>
    {
        private readonly T? _value;

        private readonly Error? _error;

        public bool IsSuccess { get; }

        public bool IsFailure => !IsSuccess;

        public T Value
        {
            get
            {
                if (IsFailure || _value is null)
                {
                    throw new InvalidOperationException("The value of a failed result cannot be accessed.");
                }

                return _value;
            }
        }

        public Error Error
        {
            get
            {
                if (IsSuccess || _error is null)
                {
                    throw new InvalidOperationException("The error of a successful result cannot be accessed.");
                }

                return _error;
            }
        }

        private Result(bool isSuccess, T? value, Error? error)
        {
            IsSuccess = isSuccess;
            _value = value;
            _error = error;
        }

        public static Result<T> Success(T value)
        {
            ArgumentNullException.ThrowIfNull(value);
            return new Result<T>(true, value, null);
        }

        public static Result<T> Failure(Error error)
        {
            ArgumentNullException.ThrowIfNull(error);
            return new Result<T>(false, default, error);
        }

    }

    public sealed record Result
    {
        private readonly Error? _error;

        public bool IsSuccess { get; }

        public bool IsFailure => !IsSuccess;

        public Error Error
        {
            get
            {
                if (IsSuccess || _error is null)
                {
                    throw new InvalidOperationException("The error of a successful result cannot be accessed.");
                }

                return _error;
            }
        }

        private Result(bool isSuccess, Error? error)
        {
            IsSuccess = isSuccess;
            _error = error;
        }

        public static Result Success() => new(true, null);

        public static Result Failure(Error error)
        {
            ArgumentNullException.ThrowIfNull(error);
            return new(false, error);
        }
    }
}
