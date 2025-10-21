using System.ComponentModel.DataAnnotations;
using System.Reflection.Metadata;

namespace CMCSPrototype.Models
{
    public class Claim
    {
        public int Id { get; set; }
        
        [Required(ErrorMessage = "Lecturer name is required")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Lecturer name must be between 2 and 100 characters")]
        public string LecturerName { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Hours worked is required")]
        [Range(1, 744, ErrorMessage = "Hours worked must be between 1 and 744 (max hours in a month)")]
        public double HoursWorked { get; set; }
        
        [Required(ErrorMessage = "Hourly rate is required")]
        [Range(50, 1000, ErrorMessage = "Hourly rate must be between R50 and R1000")]
        [DataType(DataType.Currency)]
        public decimal HourlyRate { get; set; }
        
        [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters")]
        public string? Notes { get; set; }
        
        [DataType(DataType.Date)]
        public DateTime SubmissionDate { get; set; }
        
        public ClaimStatus Status { get; set; } = ClaimStatus.Pending;
        
        public DateTime SubmittedAt { get; set; } = DateTime.Now;
        
        public List<Document> Documents { get; set; } = new List<Document>();
        
        // Calculated property for total amount
        public decimal TotalAmount => (decimal)HoursWorked * HourlyRate;
    }
    
    public enum ClaimStatus
    {
        Pending,
        Approved,
        Rejected
    }
}
