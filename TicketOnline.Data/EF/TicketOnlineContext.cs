using System.Data.Entity;
using TicketOnline.Models;

namespace TicketOnline.Data
{
    public class TicketOnlineContext : DbContext
    {
        public DbSet<Ticket> Tickets { get; set; }
        public DbSet<Event> Events { get; set; }

        public TicketOnlineContext()
            : base("DefaultConnection")
        { }
    }
}
