using Microsoft.WindowsAzure.Storage.Table;
using System;

namespace TicketOnline.Models.Storage
{
    public class EventRead : TableEntity
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Status { get; set; }
        public DateTime EventDate { get; set; }

        public int TotalSeats { get; set; }
        public double TicketPrice { get; set; }
        public int AvailableSeats { get; set; }

        public string Organizer { get; set; }
    }
}