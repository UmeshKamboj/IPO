namespace IPOClient.Models.Requests
{
    /// <summary>
    /// User list request with global search and pagination
    /// </summary>
    public class UserFilterRequest : PaginationRequest
    {
        // Only global search is supported - searchValue will search across Email, FirstName, LastName, Phone
    }
}
