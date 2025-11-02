using System.ComponentModel.DataAnnotations;

namespace EventEase.Models.Attendance
{
    public class AttendeeRecord
    {
        public int Id { get; set; }
        
        [Required]
        public int EventId { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;
        
        [Required]
        [EmailAddress]
        [StringLength(150)]
        public string Email { get; set; } = string.Empty;
        
        [StringLength(50)]
        public string Phone { get; set; } = string.Empty;
        
        [StringLength(100)]
        public string Company { get; set; } = string.Empty;
        
        [StringLength(100)]
        public string JobTitle { get; set; } = string.Empty;
        
        public DateTime RegistrationDate { get; set; } = DateTime.UtcNow;
        
        public DateTime? CheckInTime { get; set; }
        
        public DateTime? CheckOutTime { get; set; }
        
        public AttendanceStatus Status { get; set; } = AttendanceStatus.Registered;
        
        public bool IsVip { get; set; } = false;
        
        public string? SpecialRequirements { get; set; }
        
        public string? Notes { get; set; }
        
        // Navigation property
        public Event? Event { get; set; }
    }

    public enum AttendanceStatus
    {
        Registered,
        CheckedIn,
        CheckedOut,
        NoShow,
        Cancelled
    }

    public class AttendanceStats
    {
        public int EventId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public int TotalRegistered { get; set; }
        public int CheckedIn { get; set; }
        public int CheckedOut { get; set; }
        public int NoShows { get; set; }
        public int Cancelled { get; set; }
        public double AttendanceRate => TotalRegistered > 0 ? (double)CheckedIn / TotalRegistered * 100 : 0;
        public double CompletionRate => CheckedIn > 0 ? (double)CheckedOut / CheckedIn * 100 : 0;
        public TimeSpan? AverageStayDuration { get; set; }
    }

    public class AttendanceReport
    {
        public DateTime GeneratedDate { get; set; } = DateTime.UtcNow;
        public DateRange ReportPeriod { get; set; } = new();
        public List<AttendanceStats> EventStats { get; set; } = new();
        public int TotalEvents { get; set; }
        public int TotalAttendees { get; set; }
        public double OverallAttendanceRate { get; set; }
        public List<AttendeeRecord> TopAttendees { get; set; } = new();
        public Dictionary<string, int> AttendanceByCompany { get; set; } = new();
        public Dictionary<DayOfWeek, int> AttendanceByDayOfWeek { get; set; } = new();
    }

    public class DateRange
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }

    public class CheckInRequest
    {
        [Required]
        public int AttendeeId { get; set; }
        
        [Required]
        public int EventId { get; set; }
        
        public DateTime CheckInTime { get; set; } = DateTime.UtcNow;
        
        public string? Notes { get; set; }
    }

    public class CheckOutRequest
    {
        [Required]
        public int AttendeeId { get; set; }
        
        [Required]
        public int EventId { get; set; }
        
        public DateTime CheckOutTime { get; set; } = DateTime.UtcNow;
        
        public string? Feedback { get; set; }
        
        [Range(1, 5)]
        public int? Rating { get; set; }
    }
}