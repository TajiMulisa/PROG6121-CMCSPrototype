using System.ComponentModel.DataAnnotations;

namespace CMCSPrototype.Models
{
    public class Document
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(255)]
        public string FileName { get; set; } = string.Empty;
        
        [Required]
        [StringLength(500)]
        public string FilePath { get; set; } = string.Empty;
        
        [Required]
        public int ClaimId { get; set; }
        
        public Claim? Claim { get; set; }
        
        public DateTime UploadedAt { get; set; } = DateTime.Now;
        
        public long FileSize { get; set; }
        
        [StringLength(100)]
        public string ContentType { get; set; } = string.Empty;
    }
}
