using Microsoft.EntityFrameworkCore;
using AirlineTicketSystem.Data;
using AirlineTicketSystem.Models;

namespace AirlineTicketSystem.Services
{
    public interface IAirlineService
    {
        Task<List<Airline>> GetAllAirlinesAsync();
        Task<Airline?> GetAirlineByIdAsync(int id);
        Task<Airline> CreateAirlineAsync(Airline airline);
    }

    public class AirlineService : IAirlineService
    {
        private readonly AirlineDbContext _context;

        public AirlineService(AirlineDbContext context)
        {
            _context = context;
        }

        public async Task<List<Airline>> GetAllAirlinesAsync()
        {
            return await _context.Airlines.ToListAsync();
        }

        public async Task<Airline?> GetAirlineByIdAsync(int id)
        {
            return await _context.Airlines.FindAsync(id);
        }

        public async Task<Airline> CreateAirlineAsync(Airline airline)
        {
            _context.Airlines.Add(airline);
            await _context.SaveChangesAsync();
            return airline;
        }
    }

    public interface IFlightService
    {
        Task<List<Flight>> GetAllFlightsAsync();
        Task<List<Flight>> SearchFlightsAsync(string departureCity, string arrivalCity, DateTime? departureDate);
        Task<Flight?> GetFlightByIdAsync(int id);
        Task<Flight> CreateFlightAsync(Flight flight);
        Task<bool> UpdateAvailableSeatsAsync(int flightId, int seatsToReserve);
    }

    public class FlightService : IFlightService
    {
        private readonly AirlineDbContext _context;

        public FlightService(AirlineDbContext context)
        {
            _context = context;
        }

        public async Task<List<Flight>> GetAllFlightsAsync()
        {
            return await _context.Flights
                .Include(f => f.Airline)
                .OrderBy(f => f.DepartureTime)
                .ToListAsync();
        }

        public async Task<List<Flight>> SearchFlightsAsync(string departureCity, string arrivalCity, DateTime? departureDate)
        {
            var query = _context.Flights
                .Include(f => f.Airline)
                .Where(f => f.DepartureCity.Contains(departureCity) && 
                           f.ArrivalCity.Contains(arrivalCity) &&
                           f.AvailableSeats > 0);

            if (departureDate.HasValue)
            {
                var startOfDay = departureDate.Value.Date;
                var endOfDay = startOfDay.AddDays(1);
                query = query.Where(f => f.DepartureTime >= startOfDay && f.DepartureTime < endOfDay);
            }

            return await query.OrderBy(f => f.DepartureTime).ToListAsync();
        }

        public async Task<Flight?> GetFlightByIdAsync(int id)
        {
            return await _context.Flights
                .Include(f => f.Airline)
                .FirstOrDefaultAsync(f => f.Id == id);
        }

        public async Task<Flight> CreateFlightAsync(Flight flight)
        {
            _context.Flights.Add(flight);
            await _context.SaveChangesAsync();
            return flight;
        }

        public async Task<bool> UpdateAvailableSeatsAsync(int flightId, int seatsToReserve)
        {
            var flight = await _context.Flights.FindAsync(flightId);
            if (flight == null || flight.AvailableSeats < seatsToReserve)
                return false;

            flight.AvailableSeats -= seatsToReserve;
            await _context.SaveChangesAsync();
            return true;
        }
    }

    public interface IPassengerService
    {
        Task<List<Passenger>> GetAllPassengersAsync();
        Task<Passenger?> GetPassengerByIdAsync(int id);
        Task<Passenger?> GetPassengerByPassportAsync(string passportNumber);
        Task<Passenger> CreatePassengerAsync(Passenger passenger);
        Task<bool> UpdatePassengerAsync(Passenger passenger);
    }

    public class PassengerService : IPassengerService
    {
        private readonly AirlineDbContext _context;

        public PassengerService(AirlineDbContext context)
        {
            _context = context;
        }

        public async Task<List<Passenger>> GetAllPassengersAsync()
        {
            return await _context.Passengers.ToListAsync();
        }

        public async Task<Passenger?> GetPassengerByIdAsync(int id)
        {
            return await _context.Passengers.FindAsync(id);
        }

        public async Task<Passenger?> GetPassengerByPassportAsync(string passportNumber)
        {
            return await _context.Passengers
                .FirstOrDefaultAsync(p => p.PassportNumber == passportNumber);
        }

        public async Task<Passenger> CreatePassengerAsync(Passenger passenger)
        {
            _context.Passengers.Add(passenger);
            await _context.SaveChangesAsync();
            return passenger;
        }

        public async Task<bool> UpdatePassengerAsync(Passenger passenger)
        {
            var existingPassenger = await _context.Passengers.FindAsync(passenger.Id);
            if (existingPassenger == null)
                return false;

            // Обновляем все поля кроме Email
            existingPassenger.FirstName = passenger.FirstName;
            existingPassenger.LastName = passenger.LastName;
            existingPassenger.PassportNumber = passenger.PassportNumber;
            existingPassenger.PhoneNumber = passenger.PhoneNumber;
            existingPassenger.DateOfBirth = passenger.DateOfBirth;

            await _context.SaveChangesAsync();
            return true;
        }
    }

    public interface IBookingService
    {
        Task<List<Booking>> GetAllBookingsAsync();
        Task<Booking?> GetBookingByIdAsync(int id);
        Task<Booking?> GetBookingByNumberAsync(string bookingNumber);
        Task<Booking> CreateBookingAsync(List<Ticket> tickets);
        Task<bool> CancelBookingAsync(int bookingId);
    }

    public class BookingService : IBookingService
    {
        private readonly AirlineDbContext _context;

        public BookingService(AirlineDbContext context)
        {
            _context = context;
        }

        public async Task<List<Booking>> GetAllBookingsAsync()
        {
            return await _context.Bookings
                .Include(b => b.Tickets)
                    .ThenInclude(t => t.Flight)
                        .ThenInclude(f => f.Airline)
                .Include(b => b.Tickets)
                    .ThenInclude(t => t.Passenger)
                .OrderByDescending(b => b.BookingDate)
                .ToListAsync();
        }

        public async Task<Booking?> GetBookingByIdAsync(int id)
        {
            return await _context.Bookings
                .Include(b => b.Tickets)
                    .ThenInclude(t => t.Flight)
                        .ThenInclude(f => f.Airline)
                .Include(b => b.Tickets)
                    .ThenInclude(t => t.Passenger)
                .FirstOrDefaultAsync(b => b.Id == id);
        }

        public async Task<Booking?> GetBookingByNumberAsync(string bookingNumber)
        {
            return await _context.Bookings
                .Include(b => b.Tickets)
                    .ThenInclude(t => t.Flight)
                        .ThenInclude(f => f.Airline)
                .Include(b => b.Tickets)
                    .ThenInclude(t => t.Passenger)
                .FirstOrDefaultAsync(b => b.BookingNumber == bookingNumber);
        }

        public async Task<Booking> CreateBookingAsync(List<Ticket> tickets)
        {
            var booking = new Booking
            {
                BookingNumber = GenerateBookingNumber(),
                Status = BookingStatus.Confirmed,
                BookingDate = DateTime.Now,
                TotalAmount = tickets.Sum(t => t.Price)
            };

            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            foreach (var ticket in tickets)
            {
                ticket.BookingId = booking.Id;
                ticket.TicketNumber = GenerateTicketNumber();
            }

            await _context.SaveChangesAsync();
            return booking;
        }

        public async Task<bool> CancelBookingAsync(int bookingId)
        {
            var booking = await _context.Bookings
                .Include(b => b.Tickets)
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null || booking.Status == BookingStatus.Cancelled)
                return false;

            booking.Status = BookingStatus.Cancelled;
            booking.CancellationDate = DateTime.Now;

            foreach (var ticket in booking.Tickets)
            {
                ticket.Status = TicketStatus.Cancelled;
                ticket.CancellationDate = DateTime.Now;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        private string GenerateBookingNumber()
        {
            return $"BK{DateTime.Now:yyyyMMddHHmmss}{Random.Shared.Next(1000, 9999)}";
        }

        private string GenerateTicketNumber()
        {
            return $"TK{DateTime.Now:yyyyMMddHHmmss}{Random.Shared.Next(1000, 9999)}";
        }
    }
}
