using System;
using System.ComponentModel.DataAnnotations.Schema;
using TicketOnline.Models.Enum;

namespace TicketOnline.Models
{
    public class Ticket
    {
        public Guid Id { get; set; }
        public string Attendee { get; set; }
        public double TotalPrice { get; set; }
        public int TicketStatusId { get; set; }
        public string AccessCode { get; set; }

        public virtual Event ParentEvent { get; set; }

        [NotMapped]
        public TicketStatus Status
        {
            get { return (TicketStatus)TicketStatusId; }
            set { TicketStatusId = (int)value; }
        }
    }
}