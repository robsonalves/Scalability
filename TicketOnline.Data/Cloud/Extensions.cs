using System;
using TicketOnline.Models;
using TicketOnline.Models.Enum;
using TicketOnline.Models.Storage;

namespace TicketOnline.Data.Cloud
{
    public static class Extensions
    {
        public static Ticket ToTicket(this TicketRead ticketAzure)
        {
            var ticket = new Ticket();
            ticket.Id = Guid.Parse(ticketAzure.RowKey);
            ticket.Attendee = ticketAzure.PartitionKey;
            ticket.AccessCode = ticketAzure.AccessCode;
            if (ticketAzure.TicketStatus == "Paid")
            {
                ticket.Status = TicketStatus.Paid;
            }
            if (ticketAzure.TicketStatus == "Pending")
            {
                ticket.Status = TicketStatus.Pending;
            }
            ticket.TotalPrice = ticketAzure.TotalPrice;
            ticket.ParentEvent = new Event()
            {
                Name = ticketAzure.ParentEventName,
                Description = ticketAzure.ParentEventDescription,
                EventDate = ticketAzure.ParentEventDate
            };
            return ticket;
        }

        public static TicketRead ToTicketRead(this Ticket ticket)
        {
            var azureTicket = new TicketRead();
            azureTicket.RowKey = ticket.Id.ToString();
            azureTicket.PartitionKey = ticket.Attendee;
            azureTicket.RowKey = ticket.Id.ToString();
            azureTicket.AccessCode = ticket.AccessCode;
            azureTicket.ParentEventName = ticket.ParentEvent.Name;
            azureTicket.ParentEventDescription = ticket.ParentEvent.Description;
            azureTicket.ParentEventDate = ticket.ParentEvent.EventDate;
            azureTicket.TicketStatus = ticket.Status.ToString();
            azureTicket.TotalPrice = ticket.TotalPrice;
            return azureTicket;
        }

        public static Event ToEvent(this EventRead eventAzure, bool userIdAsPartitionKey)
        {
            var eventObj = new Event();
            eventObj.Id = Guid.Parse(eventAzure.RowKey);
            if (userIdAsPartitionKey)
            {
                eventObj.Organizer = eventAzure.PartitionKey;
            }
            else
            {
                eventObj.Organizer = eventAzure.Organizer;
            }

            eventObj.AvailableSeats = eventAzure.AvailableSeats;
            eventObj.Description = eventAzure.Description;
            eventObj.EventDate = eventAzure.EventDate;
            eventObj.Name = eventAzure.Name;

            if (eventAzure.Status == "Draft")
            {
                eventObj.Status = EventStatus.Draft;
            }
            if (eventAzure.Status == "Live")
            {
                eventObj.Status = EventStatus.Live;
            }
            eventObj.TicketPrice = eventAzure.TicketPrice;
            eventObj.TotalSeats = eventAzure.TotalSeats;
            return eventObj;
        }

        public static EventRead ToEventRead(this Event myEvent, bool userIdAsPartitionKey)
        {
            var azureEvent = new EventRead();
            azureEvent.PartitionKey = myEvent.EventDate.Year.ToString();
            if (userIdAsPartitionKey)
            {
                azureEvent.PartitionKey = myEvent.Organizer;
            }
            azureEvent.RowKey = myEvent.Id.ToString();
            azureEvent.AvailableSeats = myEvent.AvailableSeats;
            azureEvent.Description = myEvent.Description;
            azureEvent.Organizer = myEvent.Organizer;
            azureEvent.Name = myEvent.Name;
            azureEvent.EventDate = myEvent.EventDate;
            azureEvent.TicketPrice = myEvent.TicketPrice;
            azureEvent.TotalSeats = myEvent.TotalSeats;
            return azureEvent;
        }
    }
}
