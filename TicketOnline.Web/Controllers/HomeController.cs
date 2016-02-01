using System;
using System.EnterpriseServices;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using TicketOnline.Data;
using TicketOnline.Data.Cloud;
using TicketOnline.Models;
using TicketOnline.Models.Enum;
using TicketOnline.Web.Services;

namespace TicketOnline.Web.Controllers
{
    public class HomeController : Controller
    {
        private TicketOnlineContext _dbContext;
        private CloudContext _cloudContext;
        private OrderOrchestratorService _orderService;
        private EventManagementService _eventService;
        private Cache cache;



        public HomeController()
        {
            _dbContext = new TicketOnlineContext();
            cache = new Cache();
            _cloudContext = new CloudContext(cache);
            _orderService = new OrderOrchestratorService(_dbContext, _cloudContext);
            _eventService = new EventManagementService(_dbContext, _cloudContext);
        }

        public ActionResult Index()
        {
            return View();
        }

        [Authorize]
        public async Task<ActionResult> MyEvents()
        {
            var user = User.ApplicationUser();
            //var list = _dbContext.Events.Where(x => x.Organizer == user.Id).ToList();
            var list = await _eventService.GetMyEvents(user.Id);

            return View(list);
        }

        public ActionResult Events()
        {
            var list = _dbContext.Events.Where(e => e.StatusId == (int)EventStatus.Live).ToList();

            return View(list);
        }

        public ActionResult CreateEvent()
        {
            return View();
        }

        public ActionResult OrderTicket(Guid eventid)
        {
            throw new NotImplementedException();
        }

        public ActionResult MakeEventLive(Guid eventid)
        {
            bool result;

            var ev = _dbContext.Events.Single(e => e.Id == eventid);
            if (ev == null || ev.Status != EventStatus.Draft)
            {
                return null;
            }
            ev.Status = EventStatus.Live;
            _dbContext.SaveChanges();

            return RedirectToAction("MyEvents");
        }

        public ActionResult DeleteEvent(Guid eventid)
        {
            var result = false;
            var ev = _dbContext.Events.Single(e => e.Id == eventid);
            if (ev == null || ev.Status != EventStatus.Draft)
            {
                return null;
            }

            _dbContext.Events.Remove(ev);
            _dbContext.SaveChanges();
            return RedirectToAction("MyEvents");
        }

        [HttpPost]
        public ActionResult CreateEventConfirmed([Bind(Include = "Name, Description, TotalSeats, TicketPrice, EventDate")] Event newEvent)
        {
            var isCreated = _eventService.CreateNewEvent(newEvent);
            return isCreated ?
             (ActionResult)RedirectToAction("MyEvents") :
                                     View("CreateEvent");
        }
    }
}