namespace IPOClient.Models.Responses
{
    public class ReturnData<T>
    {
        public T? Data { get; set; }
        public ResponseType ResponseType { get; set; }
        public string? ResponseMessage { get; set; }
        public int? ResponseCode { get; set; }
        public int? ReturnId { get; set; }

        public bool Success => ResponseType == ResponseType.Success;

        public static ReturnData<T> SuccessResponse(T? data, string message = "Success", int code = 200, int? returnId = null)
        {
            return new ReturnData<T>
            {
                Data = data,
                ResponseType = Models.Responses.ResponseType.Success,
                ResponseMessage = message,
                ResponseCode = code,
                ReturnId = returnId
            };
        }

        public static ReturnData<T> ErrorResponse(string message, int code = 400, T? data = default)
        {
            return new ReturnData<T>
            {
                Data = data,
                ResponseType = Models.Responses.ResponseType.Error,
                ResponseMessage = message,
                ResponseCode = code
            };
        }

        public static ReturnData<T> WarningResponse(string message, int code = 206, T? data = default)
        {
            return new ReturnData<T>
            {
                Data = data,
                ResponseType = Models.Responses.ResponseType.Warning,
                ResponseMessage = message,
                ResponseCode = code
            };
        }
    }

    public class ReturnData
    {
        public ResponseType ResponseType { get; set; }
        public string? ResponseMessage { get; set; }
        public int? ResponseCode { get; set; }
        public int? ReturnId { get; set; }

        public bool Success => ResponseType == ResponseType.Success;

        public static ReturnData SuccessResponse(string message = "Success", int code = 200, int? returnId = null)
        {
            return new ReturnData
            {
                ResponseType = Models.Responses.ResponseType.Success,
                ResponseMessage = message,
                ResponseCode = code,
                ReturnId = returnId
            };
        }

        public static ReturnData ErrorResponse(string message, int code = 400)
        {
            return new ReturnData
            {
                ResponseType = Models.Responses.ResponseType.Error,
                ResponseMessage = message,
                ResponseCode = code
            };
        }

        public static ReturnData WarningResponse(string message, int code = 206)
        {
            return new ReturnData
            {
                ResponseType = Models.Responses.ResponseType.Warning,
                ResponseMessage = message,
                ResponseCode = code
            };
        }
    }
}
