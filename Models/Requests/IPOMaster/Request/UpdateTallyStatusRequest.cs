namespace IPOClient.Models.Requests.IPOMaster.Request
{
    public class UpdateTallyStatusRequest
    {
        /// <summary>
        /// Update Type: "One" for single group, "All" for all groups
        /// </summary>
        public string UpdateType { get; set; } = "One";

        /// <summary>
        /// Group ID to update (required when UpdateType is "One")
        /// </summary>
        public int? GroupId { get; set; }

        /// <summary>
        /// Tally status: true = synced, false = not synced
        /// </summary>
        public bool Status { get; set; }

        /// <summary>
        /// IPO ID
        /// </summary>
        public int IPO_Id { get; set; }
    }
}
