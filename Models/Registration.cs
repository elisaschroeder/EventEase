using System.ComponentModel.DataAnnotations;

namespace EventEase.Models
{
    public class Registration
    {
        public int Id { get; set; }
        public int EventId { get; set; }
        
        [Required(ErrorMessage = "First name is required")]
        [StringLength(50, ErrorMessage = "First name cannot exceed 50 characters")]
        public string AttendeeFirstName { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Last name is required")]
        [StringLength(50, ErrorMessage = "Last name cannot exceed 50 characters")]
        public string AttendeeLastName { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Email address is required")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address")]
        [StringLength(100, ErrorMessage = "Email cannot exceed 100 characters")]
        public string AttendeeEmail { get; set; } = string.Empty;
        
        [Phone(ErrorMessage = "Please enter a valid phone number")]
        public string AttendeePhone { get; set; } = string.Empty;
        
        [StringLength(100, ErrorMessage = "Company name cannot exceed 100 characters")]
        public string Company { get; set; } = string.Empty;
        
        [StringLength(100, ErrorMessage = "Position cannot exceed 100 characters")]
        public string Position { get; set; } = string.Empty;
        
        public DateTime RegistrationDate { get; set; } = DateTime.Now;
        
        [StringLength(500, ErrorMessage = "Special requests cannot exceed 500 characters")]
        public string SpecialRequests { get; set; } = string.Empty;
        
        public bool IsConfirmed { get; set; } = false;
    }
}