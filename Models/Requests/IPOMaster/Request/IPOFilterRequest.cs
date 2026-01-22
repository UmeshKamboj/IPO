namespace IPOClient.Models.Requests.IPOMaster.Request
{
    /// <summary>
    /// IPO list request with global search and pagination
    /// </summary>
    public class IPOFilterRequest : IPOClient.Models.Requests.PaginationRequest
    {
        // Only global search is supported - searchValue will search across IPOName, Remark
    }
}
