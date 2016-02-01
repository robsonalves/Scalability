using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.RetryPolicies;
using Microsoft.WindowsAzure.Storage.Table;
using TicketOnline.Models;
using TicketOnline.Models.Storage;

namespace TicketOnline.Data.Cloud
{
    public class CloudContext
    {
        private CloudStorageAccount _storageAccount;
        private CloudTableClient _tableClient;
        private CloudTable _tableTicktes;
        private CloudTable _tableEvents;
        private CloudTable _tableMyEvents;
        private Cache _cache;
        private CloudQueueClient _queueClient;
        private CloudQueue _queue;

        public CloudContext(Cache cacheService)
        {
            _storageAccount = CloudStorageAccount.Parse(ConfigurationManager.ConnectionStrings["StorageConnectionString"].ConnectionString);
            _tableClient = _storageAccount.CreateCloudTableClient();

            _tableClient.DefaultRequestOptions.RetryPolicy = new ExponentialRetry(TimeSpan.FromMilliseconds(2000), 3);

            _tableTicktes = _tableClient.GetTableReference("TicketsRead");
            _tableEvents = _tableClient.GetTableReference("EventsRead");
            _tableMyEvents = _tableClient.GetTableReference("MyEventsRead");

            _tableTicktes.CreateIfNotExists();
            _tableEvents.CreateIfNotExists();
            _tableMyEvents.CreateIfNotExists();


            _queueClient = _storageAccount.CreateCloudQueueClient();
            _queue = _queueClient.GetQueueReference("ticket-online");
            _queue.CreateIfNotExists();

            _cache = cacheService;
        }

        public async Task<Ticket> GetTicket(string userId, Guid ticketId)
        {
            var myTickets = await GetMyTickets(userId);
            return myTickets.SingleOrDefault(e => e.Id == ticketId);
        }
        public async Task<List<Ticket>> GetMyTickets(string userId)
        {
            var key = GenerateMyTicketsKey(userId);

            return await _cache.GetFromCacheAsync(key, async () =>
            {
                var tickets = new List<Ticket>();
                string partitionkey = userId;

                TableQuery<TicketRead> query = new TableQuery<TicketRead>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, userId));
                TableQuerySegment<TicketRead> currentSegment = null;
                while (currentSegment == null || currentSegment.ContinuationToken != null)
                {
                    currentSegment = await _tableTicktes.ExecuteQuerySegmentedAsync(query, currentSegment?.ContinuationToken);

                    foreach (TicketRead nosqlTicket in currentSegment.Results)
                    {
                        var ticket = nosqlTicket.ToTicket();
                        tickets.Add(ticket);
                    }
                }
                return tickets;
            });
        }

        private static string GenerateMyTicketsKey(string userId)
        {
            return $"MyTickets-{userId}";
        }


        public async Task<List<Event>> GetMyEvents(string userId)
        {
            var key = GenerateMyEventsKey(userId);
            return await _cache.GetFromCacheAsync(key, async () =>
            {

                List<Event> events = new List<Event>();

                TableQuery<EventRead> query = new TableQuery<EventRead>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, userId));
                TableQuerySegment<EventRead> currentSegment = null;
                while (currentSegment == null || currentSegment.ContinuationToken != null)
                {
                    currentSegment = await _tableMyEvents.ExecuteQuerySegmentedAsync(query, currentSegment?.ContinuationToken);
                    foreach (EventRead nosqlEvent in currentSegment.Results)
                    {
                        var eventObj = nosqlEvent.ToEvent(true);
                        events.Add(eventObj);
                    }
                }
                return events;
            });
        }

        private static string GenerateMyEventsKey(string userId)
        {
            return $"MyEvents-{userId}";
        }

        public async Task<List<Event>> GetLiveEvents(DateTime currentDate)
        {
            string year = currentDate.Year.ToString();
            var key = GenerateLiveEventsKey(year);
            var yearEvents = await _cache.GetFromCacheAsync(key, async () =>
            {
                List<Event> events = new List<Event>();
                string partitionKey = year;
                TableQuery<EventRead> query = new TableQuery<EventRead>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partitionKey));
                var result = _tableEvents.ExecuteQuery(query);

                foreach (EventRead nosqlEvent in result)
                {
                    var eventObj = nosqlEvent.ToEvent(false);
                    events.Add(eventObj);
                }
                return events;
            });
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
            return yearEvents.Where(e => e.EventDate >= currentDate).ToList();
        }

        private static string GenerateLiveEventsKey(string year)
        {
            return $"LiveEvents-{year}";
        }

        public void ConfirmTicket(Ticket ticket)
        {
            string partitionKey = ticket.Attendee;
            string rowKey = ticket.Id.ToString();
            var ticketToUpdate = new DynamicTableEntity { PartitionKey = partitionKey, RowKey = rowKey, ETag = "*" };
            Dictionary<string, EntityProperty> newProperties = new Dictionary<string, EntityProperty>();
            newProperties.Add("TicketStatus", new EntityProperty("Paid"));
            TableOperation updateOperation = TableOperation.Merge(ticketToUpdate);
            _tableTicktes.Execute(updateOperation);

            _cache.InvalidateCache(GenerateMyTicketsKey(ticket.Attendee));
        }

        public void MakeEventLive(Event eventObj)
        {
            string partitionKey = eventObj.Organizer;
            string rowKey = eventObj.Id.ToString();
            var eventToUpdate = new DynamicTableEntity() { PartitionKey = partitionKey, RowKey = rowKey, ETag = "*" };
            Dictionary<string, EntityProperty> newProperties = new Dictionary<string, EntityProperty>();
            newProperties.Add("Status", new EntityProperty("Live"));
            eventToUpdate.Properties = newProperties;
            TableOperation updateOperation = TableOperation.Merge(eventToUpdate);
            _tableMyEvents.Execute(updateOperation);

            // Add the new live event to All Events table
            var eventToAdd = eventObj.ToEventRead(false);
            eventToAdd.Status = "Live";
            TableOperation addOperation = TableOperation.InsertOrReplace(eventToAdd);
            _tableEvents.Execute(addOperation);

            // Invalidate cache
            _cache.InvalidateCache(GenerateLiveEventsKey(eventToAdd.PartitionKey));
        }

        public void UpdateEventSeats(Event eventObj)
        {
            string partitionKey = eventObj.EventDate.Year.ToString();
            string rowKey = eventObj.Id.ToString();
            var eventToUpdate = new DynamicTableEntity() { PartitionKey = partitionKey, RowKey = rowKey, ETag = "*" };
            Dictionary<string, EntityProperty> newProperties = new Dictionary<string, EntityProperty>();
            newProperties.Add("AvailableSeats", new EntityProperty(eventObj.AvailableSeats));
            eventToUpdate.Properties = newProperties;
            TableOperation updateOperation = TableOperation.Merge(eventToUpdate);
            _tableEvents.Execute(updateOperation);

            // Invalidate cache
            _cache.InvalidateCache(GenerateLiveEventsKey(partitionKey));
        }

        public void DeleteTicket(Ticket ticket)
        {
            string partitionKey = ticket.Attendee;
            string rowKey = ticket.Id.ToString();
            var ticketToDelete = new TicketRead() { PartitionKey = partitionKey, RowKey = rowKey, ETag = "*" };

            TableOperation deleteOperation = TableOperation.Delete(ticketToDelete);
            _tableTicktes.Execute(deleteOperation);

            // Invalidate cache
            _cache.InvalidateCache(GenerateMyTicketsKey(partitionKey));
        }

        public void DeleteEvent(Event eventObj)
        {
            string partitionKey = eventObj.Organizer;
            string rowKey = eventObj.Id.ToString();
            var eventToDelete = new TicketRead() { PartitionKey = partitionKey, RowKey = rowKey, ETag = "*" };

            TableOperation deleteOperation = TableOperation.Delete(eventToDelete);
            _tableEvents.Execute(deleteOperation);

            // Invalidate cache
            _cache.InvalidateCache(GenerateMyEventsKey(partitionKey));
        }

        public async Task<Guid> PlaceOrderInQueue(Guid eventId, string userId)
        {
            try
            {
                var ticketId = Guid.NewGuid();
                var messageContent = $"Order;{eventId};{userId};{ticketId}";
                var orderMessage = new CloudQueueMessage(messageContent);
                await _queue.AddMessageAsync(orderMessage);
                return ticketId;
            }
            catch (Exception ex)
            {
                // Log the exception somewhere
                return Guid.Empty;
            }
        }

        public async Task<OrderDetails> GetPendingOrderFromQueue()
        {
            OrderDetails returnMessage = null;
            try
            {
                var message = await _queue.GetMessageAsync();
                if (message != null)
                {
                    if (message.DequeueCount > 5)
                    {
                        // Poisoned message, delete it
                        await _queue.DeleteMessageAsync(message);
                        return null;
                    }
                    var messageContent = message.AsString;
                    string[] segments = messageContent.Split(';');
                    if (segments[0] == "Order")
                    {
                        var eventId = segments[1];
                        var userId = segments[2];
                        var ticketId = segments[3];

                        returnMessage = new OrderDetails()
                        {
                            EventId = eventId,
                            UserId = userId,
                            TicketId = ticketId,
                            MessageId = message.Id,
                            PopReceipt = message.PopReceipt
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the exception somewhere
            }

            return returnMessage;
        }

        public async Task DeletePendingOrderFromQueue(string messageId, string popReceipt)
        {
            await _queue.DeleteMessageAsync(messageId, popReceipt);
        }

        public void AddTicket(Ticket ticket)
        {
            var ticketToAdd = ticket.ToTicketRead();
            TableOperation addOperation = TableOperation.InsertOrReplace(ticketToAdd);
            _tableTicktes.Execute(addOperation);

            _cache.InvalidateCache(GenerateMyTicketsKey(ticketToAdd.PartitionKey));
        }

        public void AddEvent(Event eventObj)
        {
            var eventToAdd = eventObj.ToEventRead(true);
            TableOperation addOperation = TableOperation.InsertOrReplace(eventToAdd);
            _tableMyEvents.Execute(addOperation);

            _cache.InvalidateCache(GenerateMyEventsKey(eventToAdd.PartitionKey));
        }


    }
}

