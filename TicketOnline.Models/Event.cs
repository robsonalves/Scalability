using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using TicketOnline.Models.Enum;

namespace TicketOnline.Models
{
    public class Event
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int StatusId { get; set; }
        public DateTime EventDate { get; set; }
        public int TotalSeats { get; set; }
        public double TicketPrice { get; set; }
        public int AvailableSeats { get; set; }
        public string Organizer { get; set; }
        public List<Ticket> Tickects { get; set; }

        [NotMapped]
        public EventStatus Status
        {
            get { return (EventStatus)StatusId; }
            set { StatusId = (int) value; }
        }

    }
}
