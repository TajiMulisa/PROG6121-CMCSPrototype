using System.ComponentModel.DataAnnotations;

namespace CMCSPrototype.Models
{
    public class ClaimHistory
    {
        public int Id { get; set; }
        
        [Required]
        public int ClaimId { get; set; }
        
        public Claim? Claim { get; set; }
        
        [Required]
        public ClaimStatus Status { get; set; }
        
        [Required]
        [StringLength(100)]
        public string ChangedBy { get; set; } = string.Empty;
        
        public DateTime ChangedAt { get; set; } = DateTime.Now;
        
        [StringLength(500)]
        public string? Comments { get; set; }
        
        [StringLength(50)]
        public string Action { get; set; } = string.Empty; // "Submitted", "Approved", "Rejected"
    }
}
