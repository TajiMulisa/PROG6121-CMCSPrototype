using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace CMCSPrototype.Models
{
    public class SubmitClaimViewModel
    {
        [Required(ErrorMessage = "Hours worked is required")]
        [Range(1, 744, ErrorMessage = "Hours worked must be between 1 and 744 (max hours in a month)")]
        public double HoursWorked { get; set; }

        // Hourly rate is auto-populated from user profile (set by HR)
        [Range(50, 1000, ErrorMessage = "Hourly rate must be between R50 and R1000")]
        [DataType(DataType.Currency)]
        public decimal HourlyRate { get; set; }

        [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters")]
        public string? Notes { get; set; }

        [Required(ErrorMessage = "Claim month is required")]
        public int ClaimMonth { get; set; }

        [Required(ErrorMessage = "Claim year is required")]
        public int ClaimYear { get; set; }

        public IEnumerable<SelectListItem> Months { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> Years { get; set; } = new List<SelectListItem>();

        public IFormFile? Document { get; set; }

        // Calculated property for display
        public decimal TotalAmount => (decimal)HoursWorked * HourlyRate;
    }
}
