using CMCSPrototype.Data;
using CMCSPrototype.Models;
using CMCSPrototype.Services;

namespace CMCSPrototype
{
    public static class DatabaseSeeder
    {
        public static void SeedDatabase(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var authService = scope.ServiceProvider.GetRequiredService<IAuthService>();

            // Check if database is already seeded
            if (context.Users.Any())
            {
                return; // Database already has data
            }

            // Create HR User
            var hrUser = new User
            {
                FullName = "HR Administrator",
                Email = "hr@cmcs.com",
                PasswordHash = authService.HashPassword("hr123"),
                Role = UserRole.HR,
                IsActive = true,
                CreatedAt = DateTime.Now
            };
            context.Users.Add(hrUser);

            // Create Sample Lecturers
            var lecturer1 = new User
            {
                FullName = "John Smith",
                Email = "john.smith@cmcs.com",
                PasswordHash = authService.HashPassword("lecturer123"),
                Role = UserRole.Lecturer,
                HourlyRate = 250.00m,
                IsActive = true,
                CreatedAt = DateTime.Now
            };
            context.Users.Add(lecturer1);

            var lecturer2 = new User
            {
                FullName = "Sarah Johnson",
                Email = "sarah.johnson@cmcs.com",
                PasswordHash = authService.HashPassword("lecturer123"),
                Role = UserRole.Lecturer,
                HourlyRate = 300.00m,
                IsActive = true,
                CreatedAt = DateTime.Now
            };
            context.Users.Add(lecturer2);

            var lecturer3 = new User
            {
                FullName = "Michael Brown",
                Email = "michael.brown@cmcs.com",
                PasswordHash = authService.HashPassword("lecturer123"),
                Role = UserRole.Lecturer,
                HourlyRate = 275.00m,
                IsActive = true,
                CreatedAt = DateTime.Now
            };
            context.Users.Add(lecturer3);

            // Create Coordinator
            var coordinator = new User
            {
                FullName = "Jane Coordinator",
                Email = "coordinator@cmcs.com",
                PasswordHash = authService.HashPassword("coord123"),
                Role = UserRole.Coordinator,
                IsActive = true,
                CreatedAt = DateTime.Now
            };
            context.Users.Add(coordinator);

            // Create Manager
            var manager = new User
            {
                FullName = "Robert Manager",
                Email = "manager@cmcs.com",
                PasswordHash = authService.HashPassword("manager123"),
                Role = UserRole.Manager,
                IsActive = true,
                CreatedAt = DateTime.Now
            };
            context.Users.Add(manager);

            // Save users first
            context.SaveChanges();

            // Create some sample claims
            var claim1 = new Claim
            {
                LecturerName = "John Smith",
                HoursWorked = 80,
                HourlyRate = 250.00m,
                Notes = "October 2024 teaching hours",
                SubmissionDate = new DateTime(2024, 10, 31),
                SubmittedAt = new DateTime(2024, 10, 31),
                Status = ClaimStatus.Pending
            };
            context.Claims.Add(claim1);

            var claim2 = new Claim
            {
                LecturerName = "Sarah Johnson",
                HoursWorked = 95,
                HourlyRate = 300.00m,
                Notes = "October 2024 teaching and research hours",
                SubmissionDate = new DateTime(2024, 10, 30),
                SubmittedAt = new DateTime(2024, 10, 30),
                Status = ClaimStatus.Approved,
                CoordinatorApprovedBy = "Coordinator",
                CoordinatorApprovedAt = new DateTime(2024, 11, 1),
                CoordinatorApprovalComments = "Approved - all documentation in order"
            };
            context.Claims.Add(claim2);

            var claim3 = new Claim
            {
                LecturerName = "Michael Brown",
                HoursWorked = 120,
                HourlyRate = 275.00m,
                Notes = "September 2024 - Full month teaching",
                SubmissionDate = new DateTime(2024, 9, 30),
                SubmittedAt = new DateTime(2024, 9, 30),
                Status = ClaimStatus.Approved,
                CoordinatorApprovedBy = "Coordinator",
                CoordinatorApprovedAt = new DateTime(2024, 10, 2),
                CoordinatorApprovalComments = "Approved",
                ManagerApprovedBy = "Manager",
                ManagerApprovedAt = DateTime.Now.AddDays(-25),
                ManagerApprovalComments = "Approved for payment",
            };
            context.Claims.Add(claim3);

            context.SaveChanges();

            Console.WriteLine("Database seeded successfully!");
            Console.WriteLine("\n=== Login Credentials ===");
            Console.WriteLine("HR: hr@cmcs.com / hr123");
            Console.WriteLine("Lecturer 1: john.smith@cmcs.com / lecturer123");
            Console.WriteLine("Lecturer 2: sarah.johnson@cmcs.com / lecturer123");
            Console.WriteLine("Lecturer 3: michael.brown@cmcs.com / lecturer123");
            Console.WriteLine("Coordinator: coordinator@cmcs.com / coord123");
            Console.WriteLine("Manager: manager@cmcs.com / manager123");
            Console.WriteLine("========================\n");
        }
    }
}
