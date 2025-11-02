using System.ComponentModel.DataAnnotations;

namespace EventEase.Models.Session
{
    public class UserSession
    {
        public string SessionId { get; set; } = Guid.NewGuid().ToString();
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime LastActivity { get; set; } = DateTime.Now;
        public string? UserId { get; set; }
        public string? UserEmail { get; set; }
        public string? UserName { get; set; }
        public bool IsAuthenticated { get; set; } = false;
        public Dictionary<string, object> SessionData { get; set; } = new();
        public List<string> ViewedEvents { get; set; } = new();
        public List<string> SearchHistory { get; set; } = new();
        public string? CurrentPage { get; set; }
        public string? PreviousPage { get; set; }
        public UserPreferences Preferences { get; set; } = new();
        public ShoppingCart Cart { get; set; } = new();
        public int PageViews { get; set; } = 0;
        public TimeSpan SessionDuration => DateTime.Now - CreatedAt;
        public bool IsExpired => DateTime.Now - LastActivity > TimeSpan.FromMinutes(30);
    }

    public class UserPreferences
    {
        public string PreferredEventType { get; set; } = string.Empty;
        public string PreferredLocation { get; set; } = string.Empty;
        public decimal MaxPrice { get; set; } = 1000;
        public int PreferredPageSize { get; set; } = 10;
        public bool EmailNotifications { get; set; } = true;
        public string Theme { get; set; } = "light";
        public List<string> FavoriteEventTypes { get; set; } = new();
        public List<string> FavoriteLocations { get; set; } = new();
    }

    public class ShoppingCart
    {
        public List<CartItem> Items { get; set; } = new();
        public decimal TotalAmount => Items.Sum(i => i.Price * i.Quantity);
        public int TotalItems => Items.Sum(i => i.Quantity);
        public DateTime LastUpdated { get; set; } = DateTime.Now;
    }

    public class CartItem
    {
        public int EventId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Quantity { get; set; } = 1;
        public DateTime DateAdded { get; set; } = DateTime.Now;
        public Dictionary<string, string> Metadata { get; set; } = new();
    }

    public class SessionEvent
    {
        public string EventId { get; set; } = Guid.NewGuid().ToString();
        public string SessionId { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public SessionEventType EventType { get; set; }
        public string Page { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public Dictionary<string, object> Data { get; set; } = new();
        public string? UserId { get; set; }
        public string UserAgent { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
    }

    public enum SessionEventType
    {
        PageView,
        EventView,
        Search,
        Registration,
        AddToCart,
        RemoveFromCart,
        FilterApplied,
        PreferenceChanged,
        Login,
        Logout,
        Error,
        Download,
        Share,
        Navigation
    }

    public class SessionAnalytics
    {
        public string SessionId { get; set; } = string.Empty;
        public int TotalPageViews { get; set; }
        public int UniqueEventsViewed { get; set; }
        public int SearchQueries { get; set; }
        public int RegistrationAttempts { get; set; }
        public int CompletedRegistrations { get; set; }
        public TimeSpan AveragePageTime { get; set; }
        public string MostViewedEventType { get; set; } = string.Empty;
        public List<string> PopularSearchTerms { get; set; } = new();
        public string EntryPage { get; set; } = string.Empty;
        public string ExitPage { get; set; } = string.Empty;
        public DateTime FirstActivity { get; set; }
        public DateTime LastActivity { get; set; }
        public bool ConvertedToRegistration { get; set; }
        public decimal CartValue { get; set; }
    }
}