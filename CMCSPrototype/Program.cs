using CMCSPrototype.Data;
using CMCSPrototype.Services;
using Microsoft.EntityFrameworkCore;

namespace CMCSPrototype
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container
            builder.Services.AddControllersWithViews();

            // Register AppDbContext with in-memory database
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase("CMCS"));

            // Register ClaimService with interface
            builder.Services.AddScoped<IClaimService, ClaimService>();

            var app = builder.Build();

            // Configure the HTTP request pipeline
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();
            app.UseRouting();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}

