using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AirlineTicketSystem.Models
{
    public class Airline
    {
        public int Id { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;
        
        [MaxLength(10)]
        public string Code { get; set; } = string.Empty;
        
        [MaxLength(200)]
        public string? Description { get; set; }
        
        public List<Flight> Flights { get; set; } = new();
    }

    public class Flight
    {
        public int Id { get; set; }
        
        [Required]
        [MaxLength(20)]
        public string FlightNumber { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(100)]
        public string DepartureCity { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(100)]
        public string ArrivalCity { get; set; } = string.Empty;
        
        public DateTime DepartureTime { get; set; }
        public DateTime ArrivalTime { get; set; }
        
        public int TotalSeats { get; set; }
        public int AvailableSeats { get; set; }
        
        [Column(TypeName = "decimal(10,2)")]
        public decimal BasePrice { get; set; }
        
        public int AirlineId { get; set; }
        public Airline Airline { get; set; } = null!;
        
        public List<Ticket> Tickets { get; set; } = new();
    }

    public class Passenger
    {
        public int Id { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(100)]
        public string LastName { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(20)]
        public string PassportNumber { get; set; } = string.Empty;
        
        [MaxLength(200)]
        public string? Email { get; set; }
        
        [MaxLength(20)]
        public string? PhoneNumber { get; set; }
        
        public DateTime DateOfBirth { get; set; }
        
        public List<Ticket> Tickets { get; set; } = new();
    }

    public class Ticket
    {
        public int Id { get; set; }
        
        [Required]
        [MaxLength(20)]
        public string TicketNumber { get; set; } = string.Empty;
        
        [Column(TypeName = "decimal(10,2)")]
        public decimal Price { get; set; }
        
        public TicketClass Class { get; set; }
        public TicketStatus Status { get; set; }
        
        public DateTime BookingDate { get; set; }
        public DateTime? CancellationDate { get; set; }
        
        public int FlightId { get; set; }
        public Flight Flight { get; set; } = null!;
        
        public int PassengerId { get; set; }
        public Passenger Passenger { get; set; } = null!;
        
        public int? BookingId { get; set; }
        public Booking? Booking { get; set; }
    }

    public class Booking
    {
        public int Id { get; set; }
        
        [Required]
        [MaxLength(20)]
        public string BookingNumber { get; set; } = string.Empty;
        
        public BookingStatus Status { get; set; }
        
        public DateTime BookingDate { get; set; }
        public DateTime? CancellationDate { get; set; }
        
        [Column(TypeName = "decimal(10,2)")]
        public decimal TotalAmount { get; set; }
        
        public List<Ticket> Tickets { get; set; } = new();
    }

    public enum TicketClass
    {
        Economy = 0,
        Business = 1,
        First = 2
    }

    public enum TicketStatus
    {
        Active = 0,
        Cancelled = 1,
        Used = 2
    }

    public enum BookingStatus
    {
        Confirmed = 0,
        Cancelled = 1,
        Completed = 2
    }
}
