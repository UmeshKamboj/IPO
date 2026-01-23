using System.ComponentModel.DataAnnotations;
using IPOClient.Models.Requests;

namespace IPOClient.Models.Requests.ClientSetup
{
    public class CreateClientSetupRequest
    {
        [Required(ErrorMessage = "PAN Number is required")]
        [MaxLength(10, ErrorMessage = "PAN Number cannot exceed 10 characters")]
        public string PANNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Name is required")]
        [MaxLength(200, ErrorMessage = "Name cannot exceed 200 characters")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Group is required")]
        public int GroupId { get; set; }

        [MaxLength(100, ErrorMessage = "Client-ID/DP-ID cannot exceed 100 characters")]
        public string? ClientDPId { get; set; }
    }

    public class UpdateClientSetupRequest : CreateClientSetupRequest
    {
        [Required(ErrorMessage = "Client ID is required")]
        public int ClientId { get; set; }
    }

    public class ClientSetupFilterRequest : PaginationRequest
    {
        public int? GroupId { get; set; }
        public bool IncludeDeleted { get; set; } = false;
    }

    public class DeleteAllClientsRequest
    {
        public string? Remark { get; set; }
    }

    public class ClientDeleteHistoryFilterRequest : PaginationRequest
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }
}
