using System.ComponentModel.DataAnnotations;
using IPOClient.Models.Requests;

namespace IPOClient.Models.Requests.Group
{
    public class CreateGroupRequest
    {
        [Required(ErrorMessage = "Group name is required")]
        public string GroupName { get; set; } = string.Empty;

        public string? MobileNo { get; set; }

        public string? Email { get; set; }

        public string? Address { get; set; }

        public string? Remark { get; set; }

        public int? IPOId { get; set; }
    }

    public class UpdateGroupRequest : CreateGroupRequest
    {
       
    }

    public class GroupFilterRequest : PaginationRequest
    {
        public int? IPOId { get; set; }
    }

    /// <summary>
    /// Simple response for group list dropdown
    /// </summary>
    public class GroupListResponse
    {
        public int IPOGroupId { get; set; }
        public string GroupName { get; set; } = string.Empty;
    }
}
