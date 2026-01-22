using System.ComponentModel.DataAnnotations;

namespace IPOClient.Models.Requests.IPOMaster.Request
{
    public class CreateIPOGroupRequest
    {
        public int? Id { get; set; }
        public int? IPOId { get; set; }

        [Required(ErrorMessage = "GroupName is required")]
        [MaxLength(100, ErrorMessage = "GroupName cannot exceed 100 characters")]
        public string? GroupName { get; set; }
    }
}
