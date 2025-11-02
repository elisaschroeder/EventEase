namespace EventEase.Models
{
    public class Event
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string Location { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int MaxAttendees { get; set; }
        public int CurrentAttendees { get; set; }
        public EventType Type { get; set; }
        public string Organizer { get; set; } = string.Empty;
        public List<string> Tags { get; set; } = new();
        public bool IsActive { get; set; } = true;
    }

    public enum EventType
    {
        Corporate,
        Social,
        Wedding,
        Conference,
        Workshop,
        Networking,
        Gala,
        TeamBuilding
    }
}