using EnterpriseHomeAssignment.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using EnterpriseHomeAssignment.Interfaces;
using EnterpriseHomeAssignment.Repositories;
using EnterpriseHomeAssignment.Factories;


namespace EnterpriseHomeAssignment
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(connectionString));
            builder.Services.AddDatabaseDeveloperPageExceptionFilter();

            builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = false)
                .AddEntityFrameworkStores<ApplicationDbContext>();
            builder.Services.AddControllersWithViews();

            builder.Services.AddMemoryCache();

            builder.Services.AddScoped<ImportItemFactory>();

            builder.Services.AddKeyedScoped<IItemsRepository, ItemsInMemoryRepository>("InMemory");
            builder.Services.AddKeyedScoped<IItemsRepository, ItemsDbRepository>("Db");

            var app = builder.Build();

            // Seed admin user for development
            using (var scope = app.Services.CreateScope())
            {
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
                var adminEmail = "admin@example.com";
                var adminUser = await userManager.FindByEmailAsync(adminEmail);
                if (adminUser == null)
                {
                    var admin = new IdentityUser 
                    { 
                        UserName = adminEmail, 
                        Email = adminEmail, 
                        EmailConfirmed = true 
                    };
                    var result = await userManager.CreateAsync(admin, "Admin@123!");
                    if (result.Succeeded)
                    {
                        Console.WriteLine($"Admin user created: {adminEmail} / Admin@123!");
                    }
                }
                
                // Seed restaurant owners for testing
                var owners = new[] { "luca.owner@example.com", "hana.owner@example.com" };
                foreach (var ownerEmail in owners)
                {
                    var owner = await userManager.FindByEmailAsync(ownerEmail);
                    if (owner == null)
                    {
                        var newOwner = new IdentityUser 
                        { 
                            UserName = ownerEmail, 
                            Email = ownerEmail, 
                            EmailConfirmed = true 
                        };
                        await userManager.CreateAsync(newOwner, "Owner@123!");
                    }
                }
            }

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseMigrationsEndPoint();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");
            app.MapRazorPages();

            app.Run();
        }
    }
}
