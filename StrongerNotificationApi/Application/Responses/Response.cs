using System;

namespace Stronger.Domain.Responses;

public class Response<T> where T : class
{
    public int StatusCode { get; set; }

    public T? Content { get; set; }

    public ErrorModel? Error { get; set; }

    public class ErrorModel
    {
        public int StatusCode { get; set; }
        public String Message { get; set; }

        public ErrorModel()
        {
            Message = String.Empty;
        }
    }
}
