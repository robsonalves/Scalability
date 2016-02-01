using System.Security.Claims;
using System.Security.Principal;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;

namespace TicketOnline.Data
{
    public class ApplicationUser : IdentityUser
    {
        public async Task<ClaimsIdentity> GenertaUserIdentityAsync(UserManager<ApplicationUser> manager)
        {
            var userIdentity = await manager.CreateIdentityAsync(this, DefaultAuthenticationTypes.ApplicationCookie);

            return userIdentity;
        }
    }

    public static class GenericPrincpalExtensios
    {
        public static ApplicationUser ApplicationUser(this IPrincipal user)
        {
            ClaimsPrincipal userPrincipal = (ClaimsPrincipal)user;
            UserManager<ApplicationUser> userManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(new ApplicationDbContext()));

            return userPrincipal.Identity.IsAuthenticated ? userManager.FindById(userPrincipal.Identity.GetUserId()) : null;
        }
    }
}