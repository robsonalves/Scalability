using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TicketOnline.Web.ViewModels
{
    public class TicketSummary
    {
        public Guid TicketId { get; set; }
        public string TicketDescription { get; set; }
        public double TicketPrice { get; set; }
        public bool IsPending { get; set; }
    }
}