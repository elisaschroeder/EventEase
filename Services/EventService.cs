using EventEase.Models;

namespace EventEase.Services
{
    public interface IEventService
    {
        // Existing methods (for backwards compatibility)
        Task<List<Event>> GetEventsAsync();
        Task<Event?> GetEventByIdAsync(int id);
        Task<List<Event>> GetEventsByTypeAsync(EventType type);
        Task<List<Event>> GetCorporateEventsAsync();
        Task<List<Event>> GetSocialEventsAsync();
        Task<List<Event>> SearchEventsAsync(string searchTerm);
        Task<bool> RegisterForEventAsync(Registration registration);
        Task<List<Registration>> GetEventRegistrationsAsync(int eventId);

        // New paginated methods
        Task<PagedResult<Event>> GetEventsPagedAsync(PaginationRequest request);
        Task<PagedResult<Event>> GetEventsByTypePagedAsync(EventType type, PaginationRequest request);
        Task<PagedResult<Event>> GetCorporateEventsPagedAsync(PaginationRequest request);
        Task<PagedResult<Event>> GetSocialEventsPagedAsync(PaginationRequest request);
        Task<PagedResult<Event>> SearchEventsPagedAsync(string searchTerm, PaginationRequest request);
    }

    public class EventService : IEventService
    {
        // Configuration constant for testing purposes - easy to change for testing different dataset sizes
        // Change this value to test pagination with different amounts of data:
        // - Use 6 for basic testing (similar to original)
        // - Use 25 for medium dataset testing
        // - Use 100+ for large dataset testing
        private const int DEFAULT_SAMPLE_EVENTS_COUNT = 50;
        
        private readonly List<Event> _events;
        private readonly List<Registration> _registrations;

        public EventService()
        {
            _events = GenerateSampleEvents(DEFAULT_SAMPLE_EVENTS_COUNT);
            _registrations = new List<Registration>();
        }

        public async Task<List<Event>> GetEventsAsync()
        {
            await Task.Delay(100); // Simulate async operation
            return _events.Where(e => e.IsActive).OrderBy(e => e.Date).ToList();
        }

        public async Task<Event?> GetEventByIdAsync(int id)
        {
            await Task.Delay(50);
            return _events.FirstOrDefault(e => e.Id == id && e.IsActive);
        }

        public async Task<List<Event>> GetEventsByTypeAsync(EventType type)
        {
            await Task.Delay(100);
            return _events.Where(e => e.Type == type && e.IsActive).OrderBy(e => e.Date).ToList();
        }

        public async Task<List<Event>> GetCorporateEventsAsync()
        {
            await Task.Delay(100);
            var corporateTypes = new[] { EventType.Corporate, EventType.Conference, EventType.Workshop, EventType.Networking, EventType.TeamBuilding };
            return _events.Where(e => corporateTypes.Contains(e.Type) && e.IsActive).OrderBy(e => e.Date).ToList();
        }

        public async Task<List<Event>> GetSocialEventsAsync()
        {
            await Task.Delay(100);
            var socialTypes = new[] { EventType.Social, EventType.Gala, EventType.Wedding };
            return _events.Where(e => socialTypes.Contains(e.Type) && e.IsActive).OrderBy(e => e.Date).ToList();
        }

        public async Task<List<Event>> SearchEventsAsync(string searchTerm)
        {
            await Task.Delay(150);
            if (string.IsNullOrWhiteSpace(searchTerm))
                return await GetEventsAsync();

            var term = searchTerm.ToLower();
            return _events.Where(e => e.IsActive && 
                (e.Name.ToLower().Contains(term) || 
                 e.Description.ToLower().Contains(term) ||
                 e.Location.ToLower().Contains(term) ||
                 e.Tags.Any(tag => tag.ToLower().Contains(term))))
                .OrderBy(e => e.Date)
                .ToList();
        }

        public async Task<bool> RegisterForEventAsync(Registration registration)
        {
            await Task.Delay(200);
            
            var eventItem = await GetEventByIdAsync(registration.EventId);
            if (eventItem == null || eventItem.CurrentAttendees >= eventItem.MaxAttendees)
                return false;

            registration.Id = _registrations.Count + 1;
            _registrations.Add(registration);
            
            // Update attendee count
            eventItem.CurrentAttendees++;
            
            return true;
        }

        public async Task<List<Registration>> GetEventRegistrationsAsync(int eventId)
        {
            await Task.Delay(100);
            return _registrations.Where(r => r.EventId == eventId).ToList();
        }

        // Paginated methods implementation
        public async Task<PagedResult<Event>> GetEventsPagedAsync(PaginationRequest request)
        {
            await Task.Delay(50); // Simulate async operation
            
            request.Validate();
            var allEvents = _events.AsQueryable();
            
            // Apply sorting
            if (!string.IsNullOrEmpty(request.SortBy))
            {
                allEvents = request.SortBy.ToLower() switch
                {
                    "name" => request.SortDirection == SortDirection.Descending ? allEvents.OrderByDescending(e => e.Name) : allEvents.OrderBy(e => e.Name),
                    "date" => request.SortDirection == SortDirection.Descending ? allEvents.OrderByDescending(e => e.Date) : allEvents.OrderBy(e => e.Date),
                    "price" => request.SortDirection == SortDirection.Descending ? allEvents.OrderByDescending(e => e.Price) : allEvents.OrderBy(e => e.Price),
                    _ => allEvents.OrderBy(e => e.Date)
                };
            }
            else
            {
                allEvents = allEvents.OrderBy(e => e.Date);
            }

            var totalCount = allEvents.Count();
            var skipCount = (request.Page - 1) * request.PageSize;
            var pagedEvents = allEvents
                .Skip(skipCount)
                .Take(request.PageSize)
                .ToList();

            return new PagedResult<Event>
            {
                Items = pagedEvents,
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize
            };
        }

        public async Task<PagedResult<Event>> GetEventsByTypePagedAsync(EventType type, PaginationRequest request)
        {
            await Task.Delay(50); // Simulate async operation
            
            request.Validate();
            var filteredEvents = _events.Where(e => e.Type == type).AsQueryable();
            
            // Apply sorting
            if (!string.IsNullOrEmpty(request.SortBy))
            {
                filteredEvents = request.SortBy.ToLower() switch
                {
                    "name" => request.SortDirection == SortDirection.Descending ? filteredEvents.OrderByDescending(e => e.Name) : filteredEvents.OrderBy(e => e.Name),
                    "date" => request.SortDirection == SortDirection.Descending ? filteredEvents.OrderByDescending(e => e.Date) : filteredEvents.OrderBy(e => e.Date),
                    "price" => request.SortDirection == SortDirection.Descending ? filteredEvents.OrderByDescending(e => e.Price) : filteredEvents.OrderBy(e => e.Price),
                    _ => filteredEvents.OrderBy(e => e.Date)
                };
            }
            else
            {
                filteredEvents = filteredEvents.OrderBy(e => e.Date);
            }

            var totalCount = filteredEvents.Count();
            var skipCount = (request.Page - 1) * request.PageSize;
            var pagedEvents = filteredEvents
                .Skip(skipCount)
                .Take(request.PageSize)
                .ToList();

            return new PagedResult<Event>
            {
                Items = pagedEvents,
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize
            };
        }

        public async Task<PagedResult<Event>> GetCorporateEventsPagedAsync(PaginationRequest request)
        {
            return await GetEventsByTypePagedAsync(EventType.Corporate, request);
        }

        public async Task<PagedResult<Event>> GetSocialEventsPagedAsync(PaginationRequest request)
        {
            var socialTypes = new[] { EventType.Social, EventType.Wedding, EventType.Gala };
            
            await Task.Delay(50); // Simulate async operation
            
            request.Validate();
            var filteredEvents = _events.Where(e => socialTypes.Contains(e.Type)).AsQueryable();
            
            // Apply sorting
            if (!string.IsNullOrEmpty(request.SortBy))
            {
                filteredEvents = request.SortBy.ToLower() switch
                {
                    "name" => request.SortDirection == SortDirection.Descending ? filteredEvents.OrderByDescending(e => e.Name) : filteredEvents.OrderBy(e => e.Name),
                    "date" => request.SortDirection == SortDirection.Descending ? filteredEvents.OrderByDescending(e => e.Date) : filteredEvents.OrderBy(e => e.Date),
                    "price" => request.SortDirection == SortDirection.Descending ? filteredEvents.OrderByDescending(e => e.Price) : filteredEvents.OrderBy(e => e.Price),
                    _ => filteredEvents.OrderBy(e => e.Date)
                };
            }
            else
            {
                filteredEvents = filteredEvents.OrderBy(e => e.Date);
            }

            var totalCount = filteredEvents.Count();
            var skipCount = (request.Page - 1) * request.PageSize;
            var pagedEvents = filteredEvents
                .Skip(skipCount)
                .Take(request.PageSize)
                .ToList();

            return new PagedResult<Event>
            {
                Items = pagedEvents,
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize
            };
        }

        public async Task<PagedResult<Event>> SearchEventsPagedAsync(string searchTerm, PaginationRequest request)
        {
            await Task.Delay(100); // Simulate async operation
            
            request.Validate();
            var filteredEvents = _events.AsQueryable();
            
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.ToLower();
                filteredEvents = filteredEvents.Where(e => 
                    e.Name.ToLower().Contains(term) || 
                    e.Description.ToLower().Contains(term) || 
                    e.Location.ToLower().Contains(term));
            }
            
            // Apply sorting
            if (!string.IsNullOrEmpty(request.SortBy))
            {
                filteredEvents = request.SortBy.ToLower() switch
                {
                    "name" => request.SortDirection == SortDirection.Descending ? filteredEvents.OrderByDescending(e => e.Name) : filteredEvents.OrderBy(e => e.Name),
                    "date" => request.SortDirection == SortDirection.Descending ? filteredEvents.OrderByDescending(e => e.Date) : filteredEvents.OrderBy(e => e.Date),
                    "price" => request.SortDirection == SortDirection.Descending ? filteredEvents.OrderByDescending(e => e.Price) : filteredEvents.OrderBy(e => e.Price),
                    _ => filteredEvents.OrderBy(e => e.Date)
                };
            }
            else
            {
                filteredEvents = filteredEvents.OrderBy(e => e.Date);
            }

            var totalCount = filteredEvents.Count();
            var skipCount = (request.Page - 1) * request.PageSize;
            var pagedEvents = filteredEvents
                .Skip(skipCount)
                .Take(request.PageSize)
                .ToList();

            return new PagedResult<Event>
            {
                Items = pagedEvents,
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize
            };
        }

        private List<Event> GenerateSampleEvents(int count)
        {
            var events = new List<Event>();
            
            // Base event templates for variety
            var eventTemplates = new[]
            {
                new { Name = "Tech Conference", Type = EventType.Conference, BasePrice = 299.00m, MaxAttendees = 500, Location = "Seattle Convention Center, Seattle, WA", Color = "007bff" },
                new { Name = "Team Building Retreat", Type = EventType.TeamBuilding, BasePrice = 450.00m, MaxAttendees = 100, Location = "Mountain View Resort, Colorado", Color = "28a745" },
                new { Name = "Spring Gala", Type = EventType.Gala, BasePrice = 200.00m, MaxAttendees = 250, Location = "Grand Ballroom, Ritz Carlton Downtown", Color = "dc3545" },
                new { Name = "Workshop Series", Type = EventType.Workshop, BasePrice = 150.00m, MaxAttendees = 50, Location = "Innovation Hub, Austin, TX", Color = "ffc107" },
                new { Name = "Networking Mixer", Type = EventType.Networking, BasePrice = 75.00m, MaxAttendees = 150, Location = "Rooftop Lounge, Metropolitan Hotel", Color = "17a2b8" },
                new { Name = "Wedding Showcase", Type = EventType.Wedding, BasePrice = 25.00m, MaxAttendees = 300, Location = "Crystal Gardens Event Center", Color = "e83e8c" },
                new { Name = "Corporate Meeting", Type = EventType.Corporate, BasePrice = 500.00m, MaxAttendees = 200, Location = "Business District Conference Center", Color = "6c757d" },
                new { Name = "Social Gathering", Type = EventType.Social, BasePrice = 50.00m, MaxAttendees = 120, Location = "Community Center, Downtown", Color = "20c997" }
            };

            var organizers = new[] { "TechEvents Inc.", "EventEase Solutions", "Premium Events Co.", "Corporate Gatherings Ltd.", "Social Connections", "Elite Event Planners", "Innovation Events", "Luxury Occasions" };
            var adjectives = new[] { "Annual", "Exclusive", "Premium", "Elite", "Grand", "Professional", "Innovative", "Spectacular", "Ultimate", "Advanced" };
            var years = new[] { "2025", "2026" };

            var random = new Random(42); // Fixed seed for consistent results

            for (int i = 1; i <= count; i++)
            {
                var template = eventTemplates[i % eventTemplates.Length];
                var adjective = adjectives[random.Next(adjectives.Length)];
                var year = years[random.Next(years.Length)];
                var organizer = organizers[random.Next(organizers.Length)];
                
                // Add some price variation
                var priceVariation = (decimal)(random.NextDouble() * 0.4 - 0.2); // ±20% variation
                var finalPrice = Math.Round(template.BasePrice * (1 + priceVariation), 2);
                
                // Add attendance variation
                var attendancePercentage = 0.3 + (random.NextDouble() * 0.5); // 30-80% capacity
                var currentAttendees = (int)(template.MaxAttendees * attendancePercentage);

                var eventItem = new Event
                {
                    Id = i,
                    Name = $"{adjective} {template.Name} {year}",
                    Description = GenerateEventDescription(template.Type, template.Name),
                    Date = DateTime.Now.AddDays(random.Next(1, 365)), // Events within next year
                    Location = template.Location,
                    ImageUrl = $"https://via.placeholder.com/400x250/{template.Color}/ffffff?text={template.Name.Replace(" ", "+")}",
                    Price = finalPrice,
                    MaxAttendees = template.MaxAttendees,
                    CurrentAttendees = currentAttendees,
                    Type = template.Type,
                    Organizer = organizer,
                    Tags = GenerateEventTags(template.Type),
                    IsActive = true
                };

                events.Add(eventItem);
            }

            return events.OrderBy(e => e.Date).ToList();
        }

        private string GenerateEventDescription(EventType eventType, string baseName)
        {
            return eventType switch
            {
                EventType.Conference => "Join industry leaders for cutting-edge discussions, networking opportunities, and keynote presentations from top innovators in the field.",
                EventType.TeamBuilding => "A comprehensive team building experience featuring activities, leadership workshops, and collaborative challenges designed to strengthen team bonds.",
                EventType.Gala => "An exclusive black-tie event featuring fine dining, live entertainment, and networking in a luxurious setting.",
                EventType.Workshop => "Hands-on learning experience with expert instructors, practical exercises, and valuable takeaways for professional development.",
                EventType.Networking => "Connect with like-minded professionals across various industries in a relaxed atmosphere with refreshments and meaningful conversations.",
                EventType.Wedding => "Discover the latest trends and vendor showcases with expert consultations and planning resources for your perfect day.",
                EventType.Corporate => "Professional business gathering focused on strategic planning, team alignment, and organizational objectives.",
                EventType.Social => "Community-focused social event bringing people together for fun, entertainment, and relationship building.",
                _ => $"Experience an exceptional {baseName.ToLower()} with carefully curated activities and memorable moments."
            };
        }

        private List<string> GenerateEventTags(EventType eventType)
        {
            return eventType switch
            {
                EventType.Conference => new List<string> { "Technology", "Innovation", "Networking", "Professional", "Learning" },
                EventType.TeamBuilding => new List<string> { "Team Building", "Leadership", "Corporate", "Collaboration", "Development" },
                EventType.Gala => new List<string> { "Formal", "Elegant", "Networking", "Entertainment", "Luxury" },
                EventType.Workshop => new List<string> { "Education", "Hands-on", "Skills", "Training", "Professional" },
                EventType.Networking => new List<string> { "Networking", "Professional", "Business", "Connections", "Industry" },
                EventType.Wedding => new List<string> { "Wedding", "Planning", "Vendors", "Luxury", "Trends" },
                EventType.Corporate => new List<string> { "Corporate", "Business", "Strategy", "Professional", "Meeting" },
                EventType.Social => new List<string> { "Social", "Community", "Fun", "Entertainment", "Casual" },
                _ => new List<string> { "Event", "Gathering", "Experience" }
            };
        }
    }
}