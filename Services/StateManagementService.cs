using EventEase.Models;
using EventEase.Models.Session;
using System.ComponentModel;

namespace EventEase.Services
{
    public interface IStateManagementService : INotifyPropertyChanged
    {
        // Current State Properties
        UserSession CurrentSession { get; }
        List<Event> CurrentEvents { get; set; }
        Event? SelectedEvent { get; set; }
        string CurrentSearchTerm { get; set; }
        string CurrentEventType { get; set; }
        bool IsLoading { get; set; }
        string? ErrorMessage { get; set; }
        ShoppingCart Cart { get; }
        UserPreferences Preferences { get; }

        // State Management Methods
        Task InitializeAsync();
        Task UpdateCurrentEventsAsync(List<Event> events);
        Task SelectEventAsync(Event eventItem);
        Task UpdateSearchTermAsync(string searchTerm);
        Task UpdateEventTypeAsync(string eventType);
        Task SetLoadingStateAsync(bool isLoading);
        Task SetErrorAsync(string? errorMessage);
        Task ClearErrorAsync();

        // Cart Management
        Task AddToCartAsync(Event eventItem);
        Task RemoveFromCartAsync(int eventId);
        Task ClearCartAsync();

        // Preferences Management
        Task UpdatePreferencesAsync(UserPreferences preferences);
        Task SavePreferenceAsync(string key, object value);
        T? GetPreference<T>(string key, T? defaultValue = default);

        // Session Events
        event Action<List<Event>>? EventsChanged;
        event Action<Event?>? SelectedEventChanged;
        event Action<string>? SearchTermChanged;
        event Action<string>? EventTypeChanged;
        event Action<bool>? LoadingStateChanged;
        event Action<string?>? ErrorStateChanged;
        event Action<ShoppingCart>? CartChanged;
        event Action<UserPreferences>? PreferencesChanged;
    }

    public class StateManagementService : IStateManagementService, INotifyPropertyChanged
    {
        private readonly ISessionTrackingService _sessionTracking;
        private UserSession _currentSession = new();
        private List<Event> _currentEvents = new();
        private Event? _selectedEvent;
        private string _currentSearchTerm = string.Empty;
        private string _currentEventType = string.Empty;
        private bool _isLoading = false;
        private string? _errorMessage;

        public event PropertyChangedEventHandler? PropertyChanged;
        public event Action<List<Event>>? EventsChanged;
        public event Action<Event?>? SelectedEventChanged;
        public event Action<string>? SearchTermChanged;
        public event Action<string>? EventTypeChanged;
        public event Action<bool>? LoadingStateChanged;
        public event Action<string?>? ErrorStateChanged;
        public event Action<ShoppingCart>? CartChanged;
        public event Action<UserPreferences>? PreferencesChanged;

        public StateManagementService(ISessionTrackingService sessionTracking)
        {
            _sessionTracking = sessionTracking;
            
            // Subscribe to session updates
            _sessionTracking.SessionUpdated += OnSessionUpdated;
        }

        // Properties
        public UserSession CurrentSession => _currentSession;

        public List<Event> CurrentEvents
        {
            get => _currentEvents;
            set
            {
                if (_currentEvents != value)
                {
                    _currentEvents = value;
                    OnPropertyChanged();
                    EventsChanged?.Invoke(value);
                }
            }
        }

        public Event? SelectedEvent
        {
            get => _selectedEvent;
            set
            {
                if (_selectedEvent != value)
                {
                    _selectedEvent = value;
                    OnPropertyChanged();
                    SelectedEventChanged?.Invoke(value);
                }
            }
        }

        public string CurrentSearchTerm
        {
            get => _currentSearchTerm;
            set
            {
                if (_currentSearchTerm != value)
                {
                    _currentSearchTerm = value;
                    OnPropertyChanged();
                    SearchTermChanged?.Invoke(value);
                }
            }
        }

        public string CurrentEventType
        {
            get => _currentEventType;
            set
            {
                if (_currentEventType != value)
                {
                    _currentEventType = value;
                    OnPropertyChanged();
                    EventTypeChanged?.Invoke(value);
                }
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (_isLoading != value)
                {
                    _isLoading = value;
                    OnPropertyChanged();
                    LoadingStateChanged?.Invoke(value);
                }
            }
        }

        public string? ErrorMessage
        {
            get => _errorMessage;
            set
            {
                if (_errorMessage != value)
                {
                    _errorMessage = value;
                    OnPropertyChanged();
                    ErrorStateChanged?.Invoke(value);
                }
            }
        }

        public ShoppingCart Cart => _currentSession.Cart;
        public UserPreferences Preferences => _currentSession.Preferences;

        // Implementation Methods
        public async Task InitializeAsync()
        {
            await _sessionTracking.InitializeSessionAsync();
            _currentSession = await _sessionTracking.GetCurrentSessionAsync();
            
            // Trigger initial state notifications
            OnPropertyChanged(nameof(CurrentSession));
            OnPropertyChanged(nameof(Cart));
            OnPropertyChanged(nameof(Preferences));
            CartChanged?.Invoke(Cart);
            PreferencesChanged?.Invoke(Preferences);
        }

        public async Task UpdateCurrentEventsAsync(List<Event> events)
        {
            CurrentEvents = events;
            
            await _sessionTracking.TrackEventAsync(
                SessionEventType.Navigation, 
                "events", 
                "events_loaded", 
                new { eventCount = events.Count, eventTypes = events.GroupBy(e => e.Type).Select(g => new { Type = g.Key.ToString(), Count = g.Count() }) }
            );
        }

        public async Task SelectEventAsync(Event eventItem)
        {
            SelectedEvent = eventItem;
            await _sessionTracking.TrackEventViewAsync(eventItem.Id, eventItem.Name);
        }

        public async Task UpdateSearchTermAsync(string searchTerm)
        {
            CurrentSearchTerm = searchTerm;
            
            if (!string.IsNullOrEmpty(searchTerm))
            {
                await _sessionTracking.TrackSearchAsync(searchTerm, CurrentEvents.Count);
            }
        }

        public async Task UpdateEventTypeAsync(string eventType)
        {
            CurrentEventType = eventType;
            
            await _sessionTracking.TrackEventAsync(
                SessionEventType.FilterApplied, 
                "events", 
                "filter_event_type", 
                new { eventType }
            );
        }

        public async Task SetLoadingStateAsync(bool isLoading)
        {
            IsLoading = isLoading;
            
            if (isLoading)
            {
                await ClearErrorAsync();
            }
        }

        public async Task SetErrorAsync(string? errorMessage)
        {
            ErrorMessage = errorMessage;
            
            if (!string.IsNullOrEmpty(errorMessage))
            {
                await _sessionTracking.TrackEventAsync(
                    SessionEventType.Error, 
                    _currentSession.CurrentPage ?? "unknown", 
                    "error_occurred", 
                    new { errorMessage }
                );
            }
        }

        public async Task ClearErrorAsync()
        {
            ErrorMessage = null;
        }

        public async Task AddToCartAsync(Event eventItem)
        {
            await _sessionTracking.AddToCartAsync(eventItem.Id, eventItem.Name, eventItem.Price);
            
            // Update the current session reference
            _currentSession = await _sessionTracking.GetCurrentSessionAsync();
            OnPropertyChanged(nameof(Cart));
            CartChanged?.Invoke(Cart);
        }

        public async Task RemoveFromCartAsync(int eventId)
        {
            await _sessionTracking.RemoveFromCartAsync(eventId);
            
            // Update the current session reference
            _currentSession = await _sessionTracking.GetCurrentSessionAsync();
            OnPropertyChanged(nameof(Cart));
            CartChanged?.Invoke(Cart);
        }

        public async Task ClearCartAsync()
        {
            await _sessionTracking.UpdateSessionAsync(session =>
            {
                session.Cart.Items.Clear();
                session.Cart.LastUpdated = DateTime.Now;
            });
            
            _currentSession = await _sessionTracking.GetCurrentSessionAsync();
            OnPropertyChanged(nameof(Cart));
            CartChanged?.Invoke(Cart);
            
            await _sessionTracking.TrackEventAsync(SessionEventType.RemoveFromCart, "cart", "clear_cart");
        }

        public async Task UpdatePreferencesAsync(UserPreferences preferences)
        {
            await _sessionTracking.UpdatePreferencesAsync(preferences);
            
            _currentSession = await _sessionTracking.GetCurrentSessionAsync();
            OnPropertyChanged(nameof(Preferences));
            PreferencesChanged?.Invoke(Preferences);
        }

        public async Task SavePreferenceAsync(string key, object value)
        {
            await _sessionTracking.UpdateSessionAsync(session =>
            {
                session.SessionData[key] = value;
            });
            
            _currentSession = await _sessionTracking.GetCurrentSessionAsync();
            OnPropertyChanged(nameof(Preferences));
        }

        public T? GetPreference<T>(string key, T? defaultValue = default)
        {
            if (_currentSession.SessionData.TryGetValue(key, out var value))
            {
                try
                {
                    if (value is T directValue)
                        return directValue;
                    
                    return (T?)Convert.ChangeType(value, typeof(T));
                }
                catch
                {
                    return defaultValue;
                }
            }
            
            return defaultValue;
        }

        private void OnSessionUpdated(UserSession session)
        {
            _currentSession = session;
            OnPropertyChanged(nameof(CurrentSession));
            OnPropertyChanged(nameof(Cart));
            OnPropertyChanged(nameof(Preferences));
        }

        protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}