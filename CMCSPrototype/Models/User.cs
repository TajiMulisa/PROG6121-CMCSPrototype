using System.ComponentModel.DataAnnotations;

namespace CMCSPrototype.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        public UserRole Role { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public bool IsActive { get; set; } = true;

        // Hourly rate for lecturers (set by HR)
        [Range(50, 1000, ErrorMessage = "Hourly rate must be between R50 and R1000")]
        [DataType(DataType.Currency)]
        public decimal? HourlyRate { get; set; }
    }

    public enum UserRole
    {
        Lecturer = 1,
        Coordinator = 2,
        Manager = 3,
        HR = 4
    }
}
