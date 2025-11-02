using EventEase.Models.Session;
using Microsoft.JSInterop;
using System.Text.Json;

namespace EventEase.Services
{
    public interface ISessionTrackingService
    {
        Task InitializeSessionAsync();
        Task<UserSession> GetCurrentSessionAsync();
        Task UpdateSessionAsync(Action<UserSession> updateAction);
        Task TrackEventAsync(SessionEventType eventType, string page, string action, object? data = null);
        Task TrackPageViewAsync(string page);
        Task TrackEventViewAsync(int eventId, string eventName);
        Task TrackSearchAsync(string searchTerm, int resultCount);
        Task TrackRegistrationAsync(int eventId, bool successful);
        Task AddToCartAsync(int eventId, string eventName, decimal price);
        Task RemoveFromCartAsync(int eventId);
        Task UpdatePreferencesAsync(UserPreferences preferences);
        Task<SessionAnalytics> GetSessionAnalyticsAsync();
        Task<List<SessionEvent>> GetSessionEventsAsync();
        Task ClearSessionAsync();
        Task ExtendSessionAsync();
        event Action<UserSession>? SessionUpdated;
        event Action<SessionEvent>? EventTracked;
    }

    public class SessionTrackingService : ISessionTrackingService
    {
        private readonly IJSRuntime _jsRuntime;
        private UserSession _currentSession;
        private readonly List<SessionEvent> _sessionEvents;
        private readonly Timer _sessionTimer;
        private const string SessionStorageKey = "eventease_session";
        private const string EventsStorageKey = "eventease_session_events";

        public event Action<UserSession>? SessionUpdated;
        public event Action<SessionEvent>? EventTracked;

        public SessionTrackingService(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
            _currentSession = new UserSession();
            _sessionEvents = new List<SessionEvent>();
            
            // Timer to update session every minute
            _sessionTimer = new Timer(UpdateSessionActivity, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
        }

        public async Task InitializeSessionAsync()
        {
            try
            {
                // Try to load existing session from localStorage
                var sessionJson = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", SessionStorageKey);
                
                if (!string.IsNullOrEmpty(sessionJson))
                {
                    var existingSession = JsonSerializer.Deserialize<UserSession>(sessionJson);
                    if (existingSession != null && !existingSession.IsExpired)
                    {
                        _currentSession = existingSession;
                        _currentSession.LastActivity = DateTime.Now;
                        await SaveSessionToStorageAsync();
                        
                        await TrackEventAsync(SessionEventType.Navigation, "session", "resumed");
                        return;
                    }
                }

                // Create new session
                _currentSession = new UserSession();
                await SaveSessionToStorageAsync();
                await TrackEventAsync(SessionEventType.Navigation, "session", "started");
            }
            catch (Exception ex)
            {
                // Fallback to new session on any error
                _currentSession = new UserSession();
                await TrackEventAsync(SessionEventType.Error, "session", "initialization_failed", new { error = ex.Message });
            }
        }

        public async Task<UserSession> GetCurrentSessionAsync()
        {
            if (_currentSession.IsExpired)
            {
                await ClearSessionAsync();
                await InitializeSessionAsync();
            }
            
            return _currentSession;
        }

        public async Task UpdateSessionAsync(Action<UserSession> updateAction)
        {
            updateAction(_currentSession);
            _currentSession.LastActivity = DateTime.Now;
            
            await SaveSessionToStorageAsync();
            SessionUpdated?.Invoke(_currentSession);
        }

        public async Task TrackEventAsync(SessionEventType eventType, string page, string action, object? data = null)
        {
            var sessionEvent = new SessionEvent
            {
                SessionId = _currentSession.SessionId,
                EventType = eventType,
                Page = page,
                Action = action,
                Data = data != null ? JsonSerializer.Deserialize<Dictionary<string, object>>(JsonSerializer.Serialize(data)) ?? new() : new(),
                UserId = _currentSession.UserId,
                UserAgent = await GetUserAgentAsync(),
                IpAddress = await GetIpAddressAsync()
            };

            _sessionEvents.Add(sessionEvent);
            await SaveEventsToStorageAsync();
            
            // Update session activity
            _currentSession.LastActivity = DateTime.Now;
            await SaveSessionToStorageAsync();
            
            EventTracked?.Invoke(sessionEvent);
        }

        public async Task TrackPageViewAsync(string page)
        {
            await UpdateSessionAsync(session =>
            {
                session.PreviousPage = session.CurrentPage;
                session.CurrentPage = page;
                session.PageViews++;
            });

            await TrackEventAsync(SessionEventType.PageView, page, "view");
        }

        public async Task TrackEventViewAsync(int eventId, string eventName)
        {
            await UpdateSessionAsync(session =>
            {
                var eventIdStr = eventId.ToString();
                if (!session.ViewedEvents.Contains(eventIdStr))
                {
                    session.ViewedEvents.Add(eventIdStr);
                }
            });

            await TrackEventAsync(SessionEventType.EventView, "event_details", "view", new { eventId, eventName });
        }

        public async Task TrackSearchAsync(string searchTerm, int resultCount)
        {
            await UpdateSessionAsync(session =>
            {
                if (!string.IsNullOrEmpty(searchTerm) && !session.SearchHistory.Contains(searchTerm))
                {
                    session.SearchHistory.Add(searchTerm);
                    
                    // Keep only last 20 searches
                    if (session.SearchHistory.Count > 20)
                    {
                        session.SearchHistory.RemoveAt(0);
                    }
                }
            });

            await TrackEventAsync(SessionEventType.Search, "events", "search", new { 
                searchTerm, 
                resultCount,
                timestamp = DateTime.Now 
            });
        }

        public async Task TrackRegistrationAsync(int eventId, bool successful)
        {
            await TrackEventAsync(SessionEventType.Registration, "event_details", 
                successful ? "registration_success" : "registration_failed", 
                new { eventId, successful });
        }

        public async Task AddToCartAsync(int eventId, string eventName, decimal price)
        {
            await UpdateSessionAsync(session =>
            {
                var existingItem = session.Cart.Items.FirstOrDefault(i => i.EventId == eventId);
                if (existingItem != null)
                {
                    existingItem.Quantity++;
                }
                else
                {
                    session.Cart.Items.Add(new CartItem
                    {
                        EventId = eventId,
                        EventName = eventName,
                        Price = price
                    });
                }
                session.Cart.LastUpdated = DateTime.Now;
            });

            await TrackEventAsync(SessionEventType.AddToCart, "event_details", "add_to_cart", 
                new { eventId, eventName, price });
        }

        public async Task RemoveFromCartAsync(int eventId)
        {
            await UpdateSessionAsync(session =>
            {
                var item = session.Cart.Items.FirstOrDefault(i => i.EventId == eventId);
                if (item != null)
                {
                    session.Cart.Items.Remove(item);
                    session.Cart.LastUpdated = DateTime.Now;
                }
            });

            await TrackEventAsync(SessionEventType.RemoveFromCart, "cart", "remove_from_cart", 
                new { eventId });
        }

        public async Task UpdatePreferencesAsync(UserPreferences preferences)
        {
            await UpdateSessionAsync(session =>
            {
                session.Preferences = preferences;
            });

            await TrackEventAsync(SessionEventType.PreferenceChanged, "preferences", "update", 
                new { preferences });
        }

        public async Task<SessionAnalytics> GetSessionAnalyticsAsync()
        {
            var events = _sessionEvents.Where(e => e.SessionId == _currentSession.SessionId).ToList();
            
            var analytics = new SessionAnalytics
            {
                SessionId = _currentSession.SessionId,
                TotalPageViews = events.Count(e => e.EventType == SessionEventType.PageView),
                UniqueEventsViewed = _currentSession.ViewedEvents.Count,
                SearchQueries = events.Count(e => e.EventType == SessionEventType.Search),
                RegistrationAttempts = events.Count(e => e.EventType == SessionEventType.Registration),
                CompletedRegistrations = events.Count(e => e.EventType == SessionEventType.Registration && 
                    e.Data.ContainsKey("successful") && (bool)e.Data["successful"]),
                FirstActivity = _currentSession.CreatedAt,
                LastActivity = _currentSession.LastActivity,
                ConvertedToRegistration = events.Any(e => e.EventType == SessionEventType.Registration && 
                    e.Data.ContainsKey("successful") && (bool)e.Data["successful"]),
                CartValue = _currentSession.Cart.TotalAmount,
                EntryPage = events.FirstOrDefault(e => e.EventType == SessionEventType.PageView)?.Page ?? "",
                ExitPage = events.LastOrDefault(e => e.EventType == SessionEventType.PageView)?.Page ?? ""
            };

            // Calculate most viewed event type
            var eventViews = events.Where(e => e.EventType == SessionEventType.EventView);
            if (eventViews.Any())
            {
                // This would require cross-referencing with event data
                analytics.MostViewedEventType = "Conference"; // Placeholder
            }

            // Popular search terms
            analytics.PopularSearchTerms = _currentSession.SearchHistory.Take(5).ToList();

            return analytics;
        }

        public async Task<List<SessionEvent>> GetSessionEventsAsync()
        {
            return _sessionEvents.Where(e => e.SessionId == _currentSession.SessionId).ToList();
        }

        public async Task ClearSessionAsync()
        {
            await TrackEventAsync(SessionEventType.Logout, "session", "cleared");
            
            _currentSession = new UserSession();
            _sessionEvents.Clear();
            
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", SessionStorageKey);
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", EventsStorageKey);
        }

        public async Task ExtendSessionAsync()
        {
            _currentSession.LastActivity = DateTime.Now;
            await SaveSessionToStorageAsync();
        }

        private async Task SaveSessionToStorageAsync()
        {
            try
            {
                var sessionJson = JsonSerializer.Serialize(_currentSession);
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", SessionStorageKey, sessionJson);
            }
            catch (Exception ex)
            {
                await TrackEventAsync(SessionEventType.Error, "session", "save_failed", new { error = ex.Message });
            }
        }

        private async Task SaveEventsToStorageAsync()
        {
            try
            {
                // Keep only recent events to prevent storage bloat
                var recentEvents = _sessionEvents.TakeLast(100).ToList();
                var eventsJson = JsonSerializer.Serialize(recentEvents);
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", EventsStorageKey, eventsJson);
            }
            catch (Exception ex)
            {
                await TrackEventAsync(SessionEventType.Error, "session", "events_save_failed", new { error = ex.Message });
            }
        }

        private async Task<string> GetUserAgentAsync()
        {
            try
            {
                return await _jsRuntime.InvokeAsync<string>("eval", "navigator.userAgent");
            }
            catch
            {
                return "Unknown";
            }
        }

        private async Task<string> GetIpAddressAsync()
        {
            try
            {
                // In a real application, you might call an API to get the IP
                return await _jsRuntime.InvokeAsync<string>("eval", "''"); // Placeholder
            }
            catch
            {
                return "Unknown";
            }
        }

        private void UpdateSessionActivity(object? state)
        {
            if (!_currentSession.IsExpired)
            {
                _ = Task.Run(async () => await ExtendSessionAsync());
            }
        }

        public void Dispose()
        {
            _sessionTimer?.Dispose();
        }
    }
}