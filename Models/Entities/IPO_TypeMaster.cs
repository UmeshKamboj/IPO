namespace IPOClient.Models.Entities
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    public class IPO_TypeMaster
    {
        [Key]
        [Display(Name = "IPO Type ID")]
        public int IPOTypeID { get; set; }

        [Required(ErrorMessage = "IPO Type Name is required")]
        [MaxLength(50, ErrorMessage = "IPO Type Name cannot exceed 50 characters")]
        [Display(Name = "IPO Type")]
        public string? IPOTypeName { get; set; }
    }
   

}
