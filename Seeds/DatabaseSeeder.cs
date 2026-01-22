using IPOClient.Data;
using IPOClient.Models.Entities;
using IPOClient.Utilities;
using Microsoft.EntityFrameworkCore;

namespace IPOClient.Seeds
{
    public static class DatabaseSeeder
    {
        public static async Task SeedAsync(IPOClientDbContext context)
        {
            // Seed users if they do not exist
            if (!await context.IPO_UserMasters.AnyAsync())
            {
                // 1. Create the admin user
                var adminUser = new IPO_UserMaster
                {
                    FName = "Admin",
                    LName = "User",
                    Email = "admin@ivotiontech.com",
                    Password = PasswordHelper.HashPassword("Admin@123"),
                    Phone = "+1234567890",
                    IsAdmin = true,
                    CreatedDate = DateTime.UtcNow,
                    ExpiryDate = new DateTime(2027, 12, 31)
                };

                // Add admin user first to get ID (if using Identity)
                await context.IPO_UserMasters.AddAsync(adminUser);
                await context.SaveChangesAsync(); // Saves admin and generates ID
                adminUser.CreatedBy = adminUser.Id;
                context.IPO_UserMasters.Update(adminUser);
                await context.SaveChangesAsync();
                // 2. Create non-admin users referencing adminUser.Id
                var users = new List<IPO_UserMaster>
                {
                    new IPO_UserMaster
                    {
                        FName = "John",
                        LName = "User",
                        Email = "user@ivotiontech.com",
                        Password = PasswordHelper.HashPassword("User@123"),
                        Phone = "+9876543210",
                        IsAdmin = false,
                        CreatedBy = adminUser.Id, // <-- admin as creator
                        CreatedDate = DateTime.UtcNow,
                        ExpiryDate = new DateTime(2027, 12, 31)
                    },
                    // You can add more non-admin users here
                };

                await context.IPO_UserMasters.AddRangeAsync(users);
                await context.SaveChangesAsync();
            }

            // Seed IPOTypeMaster if it does not exist
            if (!await context.IPO_TypeMaster.AnyAsync())
            {
                var ipoTypes = new List<IPO_TypeMaster>
            {
                new IPO_TypeMaster { IPOTypeID = 1, IPOTypeName = "Main Board IPOs" },
                new IPO_TypeMaster { IPOTypeID = 2, IPOTypeName = "SME IPOs" }
            };

                await context.IPO_TypeMaster.AddRangeAsync(ipoTypes);
            }

            await context.SaveChangesAsync();
        }
    }

}
