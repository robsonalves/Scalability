using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TicketOnline.Data;
using TicketOnline.Data.Cloud;
using TicketOnline.Models;
using TicketOnline.Models.Enum;

namespace TicketOnline.Web.Services
{
    public class EventManagementService
    {
        private readonly TicketOnlineContext _dbContext;
        private readonly CloudContext _cloudContext;

        public EventManagementService(TicketOnlineContext dbContext, CloudContext azureContext)
        {
            _dbContext = dbContext;
            _cloudContext = azureContext;
        }

        public bool CreateNewEvent(Event eventCreated)
        {
            bool result;

            var newEvent = eventCreated;
            newEvent.Id = Guid.NewGuid();
            newEvent.Status = EventStatus.Draft;

            try
            {
                _dbContext.Events.Add(newEvent);
                _dbContext.SaveChanges();
                result = true;

                // Update the read model
                _cloudContext.AddEvent(newEvent);
            }
            catch (Exception)
            {
                // Log the exception somewhere
                result = false;
            }
            return result;
        }

        public bool MakeEventLive(Guid eventId)
        {
            var ev = _dbContext.Events.Single(e => e.Id == eventId);
            if (ev == null || ev.Status != EventStatus.Draft)
            {
                return false;
            }
            ev.Status = EventStatus.Live;
            _dbContext.SaveChanges();

            // Update the read model
            _cloudContext.MakeEventLive(ev);

            return true;
        }

        public bool DeleteEvent(Guid eventId)
        {
            var ev = _dbContext.Events.Single(e => e.Id == eventId);
            if (ev == null || ev.Status != EventStatus.Draft)
            {
                return false;
            }
            _dbContext.Events.Remove(ev);
            _dbContext.SaveChanges();
            // Update the read model
            _cloudContext.DeleteEvent(ev);

            return true;
        }


        public async Task<List<Event>> GetMyEvents(string userId)
        {
            //return ctx.Events.Where(e => e.Organizer == userId).ToList();
            return await _cloudContext.GetMyEvents(userId);
        }

        public async Task<List<Event>> GetLiveEvents(DateTime currentDate)
        {
            //return ctx.Events.Where(e => e.StatusId == (int)EventStatus.Live && e.EventDate >= DateTime.Now).ToList();
            return await _cloudContext.GetLiveEvents(DateTime.Now);
        }
    }


}