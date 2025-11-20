using CMCSPrototype.Models;
using System.ComponentModel.DataAnnotations;

namespace CMCSPrototype.Models
{
    public class CreateUserViewModel
    {
        [Required(ErrorMessage = "Full name is required")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Full name must be between 2 and 100 characters")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirm password is required")]
        [Compare("Password", ErrorMessage = "Passwords do not match")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Role is required")]
        public UserRole Role { get; set; }

        [Range(50, 1000, ErrorMessage = "Hourly rate must be between R50 and R1000")]
        [DataType(DataType.Currency)]
        public decimal? HourlyRate { get; set; }
    }

    public class EditUserViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Full name is required")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Full name must be between 2 and 100 characters")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Role is required")]
        public UserRole Role { get; set; }

        [Range(50, 1000, ErrorMessage = "Hourly rate must be between R50 and R1000")]
        [DataType(DataType.Currency)]
        public decimal? HourlyRate { get; set; }

        public bool IsActive { get; set; }
    }

    public class HRDashboardViewModel
    {
        public int TotalUsers { get; set; }
        public int ActiveLecturers { get; set; }
        public int PendingClaims { get; set; }
        public int ApprovedClaims { get; set; }
        public decimal TotalPaymentsThisMonth { get; set; }
        public List<User> RecentUsers { get; set; } = new List<User>();
    }
}
