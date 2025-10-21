using System.ComponentModel.DataAnnotations;
using System.Reflection.Metadata;

namespace CMCSPrototype.Models
{
    public class Claim
    {
        public int Id { get; set; }
        [Required] public string LecturerName { get; set; }
        [Required] public double HoursWorked { get; set; }
        [Required] public decimal HourlyRate { get; set; }
        public string Notes { get; set; }
        public DateTime SubmissionDate { get; set; }
        public ClaimStatus Status { get; set; } = ClaimStatus.Pending;
        public DateTime SubmittedAt { get; set; } = DateTime.Now;
        public List<Document> Documents { get; set; } = new List<Document>();
    }
    public enum ClaimStatus
    {
        Pending,
        Approved,
        Rejected
    }
}
