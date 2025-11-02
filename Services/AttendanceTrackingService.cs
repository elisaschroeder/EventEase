using EventEase.Models;
using EventEase.Models.Attendance;

namespace EventEase.Services
{
    public interface IAttendanceTrackingService
    {
        Task<List<AttendeeRecord>> GetEventAttendeesAsync(int eventId);
        Task<AttendeeRecord?> GetAttendeeAsync(int attendeeId);
        Task<AttendeeRecord> RegisterAttendeeAsync(AttendeeRecord attendee);
        Task<AttendeeRecord> UpdateAttendeeAsync(AttendeeRecord attendee);
        Task<bool> DeleteAttendeeAsync(int attendeeId);
        Task<AttendeeRecord> CheckInAttendeeAsync(CheckInRequest request);
        Task<AttendeeRecord> CheckOutAttendeeAsync(CheckOutRequest request);
        Task<AttendanceStats> GetEventAttendanceStatsAsync(int eventId);
        Task<AttendanceReport> GenerateAttendanceReportAsync(DateRange? dateRange = null);
        Task<List<AttendeeRecord>> SearchAttendeesAsync(string searchTerm);
        Task<List<AttendeeRecord>> GetAttendeesByStatusAsync(AttendanceStatus status);
        Task<bool> BulkCheckInAsync(List<int> attendeeIds, int eventId);
        Task<bool> MarkAsNoShowAsync(int attendeeId);
        Task<List<AttendeeRecord>> GetVipAttendeesAsync(int eventId);
        Task<Dictionary<string, object>> GetAttendanceDashboardDataAsync();
    }

    public class AttendanceTrackingService : IAttendanceTrackingService
    {
        private static List<AttendeeRecord> _attendees = new();
        private static int _nextId = 1;
        private readonly IEventService _eventService;

        public AttendanceTrackingService(IEventService eventService)
        {
            _eventService = eventService;
            
            // Generate sample attendance data if empty
            if (!_attendees.Any())
            {
                GenerateSampleAttendanceData();
            }
        }

        public async Task<List<AttendeeRecord>> GetEventAttendeesAsync(int eventId)
        {
            await Task.Delay(50); // Simulate async operation
            return _attendees.Where(a => a.EventId == eventId).OrderBy(a => a.Name).ToList();
        }

        public async Task<AttendeeRecord?> GetAttendeeAsync(int attendeeId)
        {
            await Task.Delay(10);
            return _attendees.FirstOrDefault(a => a.Id == attendeeId);
        }

        public async Task<AttendeeRecord> RegisterAttendeeAsync(AttendeeRecord attendee)
        {
            await Task.Delay(50);
            
            attendee.Id = _nextId++;
            attendee.RegistrationDate = DateTime.UtcNow;
            attendee.Status = AttendanceStatus.Registered;
            
            _attendees.Add(attendee);
            
            return attendee;
        }

        public async Task<AttendeeRecord> UpdateAttendeeAsync(AttendeeRecord attendee)
        {
            await Task.Delay(30);
            
            var existingAttendee = _attendees.FirstOrDefault(a => a.Id == attendee.Id);
            if (existingAttendee != null)
            {
                var index = _attendees.IndexOf(existingAttendee);
                _attendees[index] = attendee;
            }
            
            return attendee;
        }

        public async Task<bool> DeleteAttendeeAsync(int attendeeId)
        {
            await Task.Delay(30);
            
            var attendee = _attendees.FirstOrDefault(a => a.Id == attendeeId);
            if (attendee != null)
            {
                _attendees.Remove(attendee);
                return true;
            }
            
            return false;
        }

        public async Task<AttendeeRecord> CheckInAttendeeAsync(CheckInRequest request)
        {
            await Task.Delay(30);
            
            var attendee = _attendees.FirstOrDefault(a => a.Id == request.AttendeeId && a.EventId == request.EventId);
            if (attendee != null)
            {
                attendee.CheckInTime = request.CheckInTime;
                attendee.Status = AttendanceStatus.CheckedIn;
                if (!string.IsNullOrEmpty(request.Notes))
                {
                    attendee.Notes = request.Notes;
                }
            }
            
            return attendee ?? throw new InvalidOperationException("Attendee not found");
        }

        public async Task<AttendeeRecord> CheckOutAttendeeAsync(CheckOutRequest request)
        {
            await Task.Delay(30);
            
            var attendee = _attendees.FirstOrDefault(a => a.Id == request.AttendeeId && a.EventId == request.EventId);
            if (attendee != null)
            {
                attendee.CheckOutTime = request.CheckOutTime;
                attendee.Status = AttendanceStatus.CheckedOut;
                if (!string.IsNullOrEmpty(request.Feedback))
                {
                    attendee.Notes = (attendee.Notes ?? "") + $"\nFeedback: {request.Feedback}";
                    if (request.Rating.HasValue)
                    {
                        attendee.Notes += $" (Rating: {request.Rating}/5)";
                    }
                }
            }
            
            return attendee ?? throw new InvalidOperationException("Attendee not found");
        }

        public async Task<AttendanceStats> GetEventAttendanceStatsAsync(int eventId)
        {
            await Task.Delay(100);
            
            var eventAttendees = _attendees.Where(a => a.EventId == eventId).ToList();
            var eventItem = await _eventService.GetEventByIdAsync(eventId);
            
            var checkedInAttendees = eventAttendees.Where(a => a.Status == AttendanceStatus.CheckedIn || a.Status == AttendanceStatus.CheckedOut).ToList();
            var avgStayDuration = CalculateAverageStayDuration(checkedInAttendees);
            
            return new AttendanceStats
            {
                EventId = eventId,
                EventName = eventItem?.Name ?? "Unknown Event",
                TotalRegistered = eventAttendees.Count,
                CheckedIn = eventAttendees.Count(a => a.Status == AttendanceStatus.CheckedIn || a.Status == AttendanceStatus.CheckedOut),
                CheckedOut = eventAttendees.Count(a => a.Status == AttendanceStatus.CheckedOut),
                NoShows = eventAttendees.Count(a => a.Status == AttendanceStatus.NoShow),
                Cancelled = eventAttendees.Count(a => a.Status == AttendanceStatus.Cancelled),
                AverageStayDuration = avgStayDuration
            };
        }

        public async Task<AttendanceReport> GenerateAttendanceReportAsync(DateRange? dateRange = null)
        {
            await Task.Delay(200);
            
            var events = await _eventService.GetEventsAsync();
            var reportPeriod = dateRange ?? new DateRange 
            { 
                StartDate = DateTime.UtcNow.AddMonths(-1), 
                EndDate = DateTime.UtcNow 
            };
            
            var relevantEvents = events.Where(e => e.Date >= reportPeriod.StartDate && e.Date <= reportPeriod.EndDate).ToList();
            var eventStats = new List<AttendanceStats>();
            
            foreach (var eventItem in relevantEvents)
            {
                var stats = await GetEventAttendanceStatsAsync(eventItem.Id);
                eventStats.Add(stats);
            }
            
            var allAttendees = _attendees.Where(a => relevantEvents.Any(e => e.Id == a.EventId)).ToList();
            
            return new AttendanceReport
            {
                ReportPeriod = reportPeriod,
                EventStats = eventStats,
                TotalEvents = relevantEvents.Count,
                TotalAttendees = allAttendees.Count,
                OverallAttendanceRate = eventStats.Any() ? eventStats.Average(s => s.AttendanceRate) : 0,
                TopAttendees = GetTopAttendees(allAttendees),
                AttendanceByCompany = GetAttendanceByCompany(allAttendees),
                AttendanceByDayOfWeek = GetAttendanceByDayOfWeek(allAttendees, relevantEvents)
            };
        }

        public async Task<List<AttendeeRecord>> SearchAttendeesAsync(string searchTerm)
        {
            await Task.Delay(50);
            
            if (string.IsNullOrWhiteSpace(searchTerm))
                return new List<AttendeeRecord>();
            
            var term = searchTerm.ToLower();
            return _attendees.Where(a => 
                a.Name.ToLower().Contains(term) ||
                a.Email.ToLower().Contains(term) ||
                a.Company.ToLower().Contains(term) ||
                a.JobTitle.ToLower().Contains(term)
            ).OrderBy(a => a.Name).ToList();
        }

        public async Task<List<AttendeeRecord>> GetAttendeesByStatusAsync(AttendanceStatus status)
        {
            await Task.Delay(30);
            return _attendees.Where(a => a.Status == status).OrderBy(a => a.Name).ToList();
        }

        public async Task<bool> BulkCheckInAsync(List<int> attendeeIds, int eventId)
        {
            await Task.Delay(100);
            
            var checkInTime = DateTime.UtcNow;
            var updated = 0;
            
            foreach (var attendeeId in attendeeIds)
            {
                var attendee = _attendees.FirstOrDefault(a => a.Id == attendeeId && a.EventId == eventId);
                if (attendee != null && attendee.Status == AttendanceStatus.Registered)
                {
                    attendee.CheckInTime = checkInTime;
                    attendee.Status = AttendanceStatus.CheckedIn;
                    updated++;
                }
            }
            
            return updated > 0;
        }

        public async Task<bool> MarkAsNoShowAsync(int attendeeId)
        {
            await Task.Delay(20);
            
            var attendee = _attendees.FirstOrDefault(a => a.Id == attendeeId);
            if (attendee != null && attendee.Status == AttendanceStatus.Registered)
            {
                attendee.Status = AttendanceStatus.NoShow;
                return true;
            }
            
            return false;
        }

        public async Task<List<AttendeeRecord>> GetVipAttendeesAsync(int eventId)
        {
            await Task.Delay(30);
            return _attendees.Where(a => a.EventId == eventId && a.IsVip).OrderBy(a => a.Name).ToList();
        }

        public async Task<Dictionary<string, object>> GetAttendanceDashboardDataAsync()
        {
            await Task.Delay(150);
            
            var today = DateTime.Today;
            var thisWeek = today.AddDays(-(int)today.DayOfWeek);
            var thisMonth = new DateTime(today.Year, today.Month, 1);
            
            var todayAttendees = _attendees.Where(a => a.CheckInTime?.Date == today).ToList();
            var weekAttendees = _attendees.Where(a => a.CheckInTime >= thisWeek).ToList();
            var monthAttendees = _attendees.Where(a => a.CheckInTime >= thisMonth).ToList();
            
            var events = await _eventService.GetEventsAsync();
            var upcomingEvents = events.Where(e => e.Date >= DateTime.UtcNow && e.Date <= DateTime.UtcNow.AddDays(7)).ToList();
            
            return new Dictionary<string, object>
            {
                ["TotalAttendees"] = _attendees.Count,
                ["TodayCheckIns"] = todayAttendees.Count,
                ["WeeklyCheckIns"] = weekAttendees.Count,
                ["MonthlyCheckIns"] = monthAttendees.Count,
                ["AverageAttendanceRate"] = CalculateOverallAttendanceRate(),
                ["UpcomingEvents"] = upcomingEvents.Count,
                ["VipAttendees"] = _attendees.Count(a => a.IsVip),
                ["RecentCheckIns"] = _attendees.Where(a => a.CheckInTime >= DateTime.UtcNow.AddHours(-24))
                                              .OrderByDescending(a => a.CheckInTime)
                                              .Take(10)
                                              .ToList()
            };
        }

        private void GenerateSampleAttendanceData()
        {
            var random = new Random();
            var companies = new[] { "TechCorp", "Innovate Ltd", "Digital Solutions", "Future Systems", "Creative Agency", "Global Enterprises" };
            var jobTitles = new[] { "Software Developer", "Project Manager", "Marketing Director", "Sales Manager", "CEO", "CTO", "Designer", "Analyst" };
            var firstNames = new[] { "John", "Jane", "Mike", "Sarah", "David", "Lisa", "Tom", "Emily", "Chris", "Anna" };
            var lastNames = new[] { "Smith", "Johnson", "Williams", "Brown", "Jones", "Garcia", "Miller", "Davis", "Rodriguez", "Martinez" };
            
            for (int eventId = 1; eventId <= 10; eventId++)
            {
                var attendeeCount = random.Next(15, 40);
                
                for (int i = 0; i < attendeeCount; i++)
                {
                    var firstName = firstNames[random.Next(firstNames.Length)];
                    var lastName = lastNames[random.Next(lastNames.Length)];
                    var company = companies[random.Next(companies.Length)];
                    
                    var attendee = new AttendeeRecord
                    {
                        Id = _nextId++,
                        EventId = eventId,
                        Name = $"{firstName} {lastName}",
                        Email = $"{firstName.ToLower()}.{lastName.ToLower()}@{company.ToLower().Replace(" ", "")}.com",
                        Phone = $"({random.Next(100, 999)}) {random.Next(100, 999)}-{random.Next(1000, 9999)}",
                        Company = company,
                        JobTitle = jobTitles[random.Next(jobTitles.Length)],
                        RegistrationDate = DateTime.UtcNow.AddDays(-random.Next(1, 30)),
                        IsVip = random.Next(1, 10) == 1, // 10% VIP
                        Status = AttendanceStatus.Registered
                    };
                    
                    // Simulate check-ins for past events
                    if (eventId <= 5) // Assume first 5 events are past events
                    {
                        var shouldCheckIn = random.Next(1, 10) <= 8; // 80% check-in rate
                        if (shouldCheckIn)
                        {
                            attendee.CheckInTime = DateTime.UtcNow.AddDays(-random.Next(1, 14)).AddHours(random.Next(8, 10));
                            attendee.Status = AttendanceStatus.CheckedIn;
                            
                            var shouldCheckOut = random.Next(1, 10) <= 7; // 70% check-out rate
                            if (shouldCheckOut)
                            {
                                attendee.CheckOutTime = attendee.CheckInTime?.AddHours(random.Next(2, 8));
                                attendee.Status = AttendanceStatus.CheckedOut;
                            }
                        }
                        else if (random.Next(1, 10) <= 2) // 2% no-show rate
                        {
                            attendee.Status = AttendanceStatus.NoShow;
                        }
                    }
                    
                    _attendees.Add(attendee);
                }
            }
        }

        private TimeSpan? CalculateAverageStayDuration(List<AttendeeRecord> attendees)
        {
            var durations = attendees
                .Where(a => a.CheckInTime.HasValue && a.CheckOutTime.HasValue)
                .Select(a => a.CheckOutTime!.Value - a.CheckInTime!.Value)
                .ToList();
            
            if (!durations.Any())
                return null;
            
            var averageTicks = (long)durations.Average(d => d.Ticks);
            return new TimeSpan(averageTicks);
        }

        private List<AttendeeRecord> GetTopAttendees(List<AttendeeRecord> attendees)
        {
            return attendees
                .GroupBy(a => new { a.Name, a.Email })
                .Select(g => new { Attendee = g.First(), Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(10)
                .Select(x => x.Attendee)
                .ToList();
        }

        private Dictionary<string, int> GetAttendanceByCompany(List<AttendeeRecord> attendees)
        {
            return attendees
                .Where(a => !string.IsNullOrEmpty(a.Company))
                .GroupBy(a => a.Company)
                .ToDictionary(g => g.Key, g => g.Count())
                .OrderByDescending(kvp => kvp.Value)
                .Take(10)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        private Dictionary<DayOfWeek, int> GetAttendanceByDayOfWeek(List<AttendeeRecord> attendees, List<Event> events)
        {
            var eventDays = events.ToDictionary(e => e.Id, e => e.Date.DayOfWeek);
            
            return attendees
                .Where(a => a.Status == AttendanceStatus.CheckedIn || a.Status == AttendanceStatus.CheckedOut)
                .Where(a => eventDays.ContainsKey(a.EventId))
                .GroupBy(a => eventDays[a.EventId])
                .ToDictionary(g => g.Key, g => g.Count());
        }

        private double CalculateOverallAttendanceRate()
        {
            if (!_attendees.Any())
                return 0;
            
            var totalRegistered = _attendees.Count;
            var totalCheckedIn = _attendees.Count(a => a.Status == AttendanceStatus.CheckedIn || a.Status == AttendanceStatus.CheckedOut);
            
            return totalRegistered > 0 ? (double)totalCheckedIn / totalRegistered * 100 : 0;
        }
    }
}