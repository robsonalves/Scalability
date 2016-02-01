using System.Data.Entity;
using Microsoft.AspNet.Identity.EntityFramework;

namespace TicketOnline.Data
{
    internal class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext() : base("DefaultConnection")
        {
        }

        public static ApplicationDbContext Create()
        {
            return new ApplicationDbContext();
        }
    }
}