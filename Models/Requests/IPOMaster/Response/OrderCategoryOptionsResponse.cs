namespace IPOClient.Models.Requests.IPOMaster.Response
{
    public class OrderCategoryOptionsResponse
    {
        public List<DropdownOption> OrderCategories { get; set; } = new();
        public List<DropdownOption> OrderTypes { get; set; } = new();
        public List<DropdownOption> InvestorTypes { get; set; } = new();
    }

    public class DropdownOption
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
