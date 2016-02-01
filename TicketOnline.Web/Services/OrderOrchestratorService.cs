using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TicketOnline.Data;
using TicketOnline.Data.Cloud;
using TicketOnline.Models;
using TicketOnline.Models.Enum;
using TicketOnline.Web.ViewModels;

namespace TicketOnline.Web.Services
{
    public class OrderOrchestratorService
    {
        private TicketOnlineContext _bdContext;
        private CloudContext cloud_bdContext;
        private static CircuitBreaker _circuitBreaker;

        public OrderOrchestratorService(TicketOnlineContext dbContext, CloudContext azureContext)
        {
            _bdContext = dbContext;
            cloud_bdContext = azureContext;
            if (_circuitBreaker == null)
            {
                _circuitBreaker = new CircuitBreaker();
            }
        }
        public async Task<Guid> PlaceOrder(Guid eventId, string userId)
        {
            var parentEvent = _bdContext.Events.Where(e => e.Id == eventId).Single();
            return await cloud_bdContext.PlaceOrderInQueue(eventId, userId);
        }

        public async Task<TicketSummary> GetTicketSummary(Guid ticketId, string userId)
        {
            var ticket = await cloud_bdContext.GetTicket(userId, ticketId);
            if (ticket != null)
            {
                var ticketSummary = new TicketSummary()
                {
                    TicketId = ticket.Id,
                    TicketDescription = "Ticket for " + ticket.ParentEvent.Name,
                    TicketPrice = ticket.TotalPrice,
                    IsPending = ticket.Status == TicketStatus.Pending
                };
                return ticketSummary;
            }
            return new TicketSummary();
        }


        public bool ConfirmTicket(Guid ticketId)
        {
            var result = false;
            bool hasBeenConfirmed = false;

            var ticket = _bdContext.Tickets.Single(t => t.Id == ticketId);

            if (ticket.ParentEvent.AvailableSeats > 0)
            {
                result = true;
                ticket.Status = TicketStatus.Paid;
                ticket.ParentEvent.AvailableSeats--;
                hasBeenConfirmed = true;
            }
            else
            {
                _bdContext.Tickets.Remove(ticket);
            }
            _bdContext.SaveChanges();

            // Update read model
            if (hasBeenConfirmed)
            {
                cloud_bdContext.ConfirmTicket(ticket);
                cloud_bdContext.UpdateEventSeats(ticket.ParentEvent);
            }
            else
            {
                cloud_bdContext.DeleteTicket(ticket);
            }

            return result;
        }

        public bool DeleteTicket(Guid ticketId)
        {
            var result = false;

            var ticket = _bdContext.Tickets.Where(t => t.Id == ticketId).Single();
            var parentEvent = ticket.ParentEvent;

            if (ticket != null)
            {
                if (ticket.Status == TicketStatus.Paid)
                {
                    // Increase available tickets
                    ticket.ParentEvent.AvailableSeats++;
                }
                _bdContext.Tickets.Remove(ticket);
                _bdContext.SaveChanges();
                result = true;
            }
            // Update read model
            if (result)
            {
                cloud_bdContext.DeleteTicket(ticket);
                cloud_bdContext.UpdateEventSeats(parentEvent);
            }
            return result;
        }

        public async Task<List<Ticket>> GetMyTickets(string userId)
        {
            List<Ticket> result = new List<Ticket>();

            try
            {
                await _circuitBreaker.ExecuteAsync(async () =>
                {
                    result = await cloud_bdContext.GetMyTickets(userId);
                });
            }
            catch (CircuitBreakerOpenException cboe)
            {
                // Log the method, return empty list
                // or get the list from a local cache
                // or surface the error to the user
                throw new Exception("Couldn't contact the ticket store, please try again.", cboe);
            }

            return result;
        }


        public async Task<Ticket> GetTicket(string userId, Guid ticketId)
        {
            return await cloud_bdContext.GetTicket(userId, ticketId);
        }
    }
}