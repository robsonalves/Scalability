using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TicketOnline.Models
{
    public class OrderDetails
    {
        public string UserId { get; set; }
        public string EventId { get; set; }
        public string TicketId { get; set; }
        public string MessageId { get; set; }
        public string PopReceipt { get; set; }
    }
}
