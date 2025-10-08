using Spectre.Console;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using AirlineTicketSystem.Data;
using AirlineTicketSystem.Services;
using AirlineTicketSystem.Models;
using System.Globalization;

namespace AirlineTicketSystem.Demo
{
    public class DemoProgram
    {
        public static async Task Main(string[] args)
        {
            // Настройка культуры для отображения цен в рублях
            var culture = new CultureInfo("ru-RU");
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;
            
            // Настройка DI контейнера
            var host = Host.CreateDefaultBuilder(args)
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders(); // Отключаем все логирование
                    logging.SetMinimumLevel(LogLevel.Warning); // Показываем только предупреждения и ошибки
                })
                .ConfigureServices((context, services) =>
                {
                    services.AddDbContext<AirlineDbContext>(options =>
                        options.UseSqlite("Data Source=airline.db")
                               .EnableSensitiveDataLogging(false)
                               .LogTo(Console.WriteLine, LogLevel.Warning)); // Логируем только предупреждения
                    
                    services.AddScoped<IAirlineService, AirlineService>();
                    services.AddScoped<IFlightService, FlightService>();
                    services.AddScoped<IPassengerService, PassengerService>();
                    services.AddScoped<IBookingService, BookingService>();
                })
                .Build();

            // Создание базы данных
            using (var scope = host.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AirlineDbContext>();
                await context.Database.EnsureCreatedAsync();
                await SeedDataAsync(context);
            }

            // Демонстрация возможностей
            var demo = new AirlineTicketDemo(host.Services);
            await demo.RunDemoAsync();
        }

        private static async Task SeedDataAsync(AirlineDbContext context)
        {
            if (await context.Airlines.AnyAsync())
                return;

            var airlines = new List<Airline>
            {
                new() { Name = "Аэрофлот", Code = "SU", Description = "Российская авиакомпания" },
                new() { Name = "S7 Airlines", Code = "S7", Description = "Сибирские авиалинии" },
                new() { Name = "Уральские авиалинии", Code = "U6", Description = "Уральские авиалинии" },
                new() { Name = "Победа", Code = "DP", Description = "Бюджетная авиакомпания" }
            };

            context.Airlines.AddRange(airlines);
            await context.SaveChangesAsync();

            var flights = new List<Flight>
            {
                new()
                {
                    FlightNumber = "SU123",
                    DepartureCity = "Москва",
                    ArrivalCity = "Санкт-Петербург",
                    DepartureTime = DateTime.Now.AddDays(1).AddHours(10),
                    ArrivalTime = DateTime.Now.AddDays(1).AddHours(12),
                    TotalSeats = 150,
                    AvailableSeats = 150,
                    BasePrice = 5000,
                    AirlineId = 1
                },
                new()
                {
                    FlightNumber = "S7456",
                    DepartureCity = "Москва",
                    ArrivalCity = "Екатеринбург",
                    DepartureTime = DateTime.Now.AddDays(2).AddHours(14),
                    ArrivalTime = DateTime.Now.AddDays(2).AddHours(18),
                    TotalSeats = 120,
                    AvailableSeats = 120,
                    BasePrice = 8000,
                    AirlineId = 2
                },
                new()
                {
                    FlightNumber = "U6789",
                    DepartureCity = "Санкт-Петербург",
                    ArrivalCity = "Сочи",
                    DepartureTime = DateTime.Now.AddDays(3).AddHours(8),
                    ArrivalTime = DateTime.Now.AddDays(3).AddHours(12),
                    TotalSeats = 180,
                    AvailableSeats = 180,
                    BasePrice = 12000,
                    AirlineId = 3
                }
            };

            context.Flights.AddRange(flights);
            await context.SaveChangesAsync();
        }
    }

    public class AirlineTicketDemo
    {
        private readonly IServiceProvider _serviceProvider;

        public AirlineTicketDemo(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task RunDemoAsync()
        {
            AnsiConsole.Write(new FigletText("Авиабилеты").Color(Color.Blue));
            AnsiConsole.Write(new Markup("[bold green]Система продажи авиабилетов - ДЕМО[/]").Centered());
            AnsiConsole.WriteLine();

            using var scope = _serviceProvider.CreateScope();
            var flightService = scope.ServiceProvider.GetRequiredService<IFlightService>();
            var passengerService = scope.ServiceProvider.GetRequiredService<IPassengerService>();
            var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

            // 1. Показать все рейсы
            AnsiConsole.Write(new Markup("[bold blue]✈️ Все доступные рейсы:[/]"));
            AnsiConsole.WriteLine();

            var flights = await flightService.GetAllFlightsAsync();
            var table = new Table();
            table.AddColumn("№");
            table.AddColumn("Рейс");
            table.AddColumn("Авиакомпания");
            table.AddColumn("Откуда");
            table.AddColumn("Куда");
            table.AddColumn("Время отправления");
            table.AddColumn("Время прибытия");
            table.AddColumn("Свободных мест");
            table.AddColumn("Цена");

            for (int i = 0; i < flights.Count; i++)
            {
                var flight = flights[i];
                table.AddRow(
                    (i + 1).ToString(),
                    flight.FlightNumber,
                    flight.Airline.Name,
                    flight.DepartureCity,
                    flight.ArrivalCity,
                    flight.DepartureTime.ToString("dd.MM.yyyy HH:mm"),
                    flight.ArrivalTime.ToString("dd.MM.yyyy HH:mm"),
                    flight.AvailableSeats.ToString(),
                    $"{flight.BasePrice:C}"
                );
            }

            AnsiConsole.Write(table);
            AnsiConsole.WriteLine();

            // 2. Демонстрация поиска
            AnsiConsole.Write(new Markup("[bold blue]🔍 Поиск рейсов Москва → Санкт-Петербург:[/]"));
            AnsiConsole.WriteLine();

            var searchResults = await flightService.SearchFlightsAsync("Москва", "Санкт-Петербург", null);
            var searchTable = new Table();
            searchTable.AddColumn("Рейс");
            searchTable.AddColumn("Авиакомпания");
            searchTable.AddColumn("Время отправления");
            searchTable.AddColumn("Цена");

            foreach (var flight in searchResults)
            {
                searchTable.AddRow(
                    flight.FlightNumber,
                    flight.Airline.Name,
                    flight.DepartureTime.ToString("dd.MM.yyyy HH:mm"),
                    $"{flight.BasePrice:C}"
                );
            }

            AnsiConsole.Write(searchTable);
            AnsiConsole.WriteLine();

            // 3. Создание или поиск пассажира
            AnsiConsole.Write(new Markup("[bold blue]👤 Создание пассажира:[/]"));
            AnsiConsole.WriteLine();

            var passportNumber = "1234567890";
            var passenger = await passengerService.GetPassengerByPassportAsync(passportNumber);
            
            if (passenger == null)
            {
                passenger = new Passenger
                {
                    FirstName = "Иван",
                    LastName = "Петров",
                    PassportNumber = passportNumber,
                    Email = "ivan.petrov@example.com",
                    PhoneNumber = "+7-999-123-45-67",
                    DateOfBirth = new DateTime(1990, 5, 15)
                };

                passenger = await passengerService.CreatePassengerAsync(passenger);
                AnsiConsole.Write(new Markup($"[green]✅ Пассажир создан: {passenger.FirstName} {passenger.LastName} (ID: {passenger.Id})[/]"));
            }
            else
            {
                AnsiConsole.Write(new Markup($"[green]✅ Пассажир найден: {passenger.FirstName} {passenger.LastName} (ID: {passenger.Id})[/]"));
            }
            AnsiConsole.WriteLine();

            // 4. Бронирование билета
            AnsiConsole.Write(new Markup("[bold blue]🎫 Бронирование билета:[/]"));
            AnsiConsole.WriteLine();

            var selectedFlight = flights.First();
            
            // Проверяем, есть ли уже бронирования для этого пассажира на этот рейс
            using var contextScope = _serviceProvider.CreateScope();
            var context = contextScope.ServiceProvider.GetRequiredService<AirlineDbContext>();
            var existingTicket = await context.Tickets
                .FirstOrDefaultAsync(t => t.PassengerId == passenger.Id && t.FlightId == selectedFlight.Id);

            if (existingTicket == null)
            {
                var ticket = new Ticket
                {
                    FlightId = selectedFlight.Id,
                    PassengerId = passenger.Id,
                    Price = selectedFlight.BasePrice * 2.0m, // Business класс
                    Class = TicketClass.Business,
                    Status = TicketStatus.Active,
                    BookingDate = DateTime.Now
                };

                var booking = await bookingService.CreateBookingAsync(new List<Ticket> { ticket });
                await flightService.UpdateAvailableSeatsAsync(selectedFlight.Id, 1);

                AnsiConsole.Write(new Markup($"[green]✅ Бронирование создано![/]"));
                AnsiConsole.Write(new Markup($"[green]Номер бронирования: {booking.BookingNumber}[/]"));
                AnsiConsole.Write(new Markup($"[green]Номер билета: {ticket.TicketNumber}[/]"));
                AnsiConsole.Write(new Markup($"[green]Класс: {ticket.Class}[/]"));
                AnsiConsole.Write(new Markup($"[green]Цена: {ticket.Price:C}[/]"));
            }
            else
            {
                AnsiConsole.Write(new Markup($"[green]✅ Бронирование уже существует![/]"));
                AnsiConsole.Write(new Markup($"[green]Номер билета: {existingTicket.TicketNumber}[/]"));
                AnsiConsole.Write(new Markup($"[green]Класс: {existingTicket.Class}[/]"));
                AnsiConsole.Write(new Markup($"[green]Цена: {existingTicket.Price:C}[/]"));
            }
            AnsiConsole.WriteLine();

            // 5. Статистика
            AnsiConsole.Write(new Markup("[bold blue]📊 Статистика системы:[/]"));
            AnsiConsole.WriteLine();

            var totalFlights = await context.Flights.CountAsync();
            var totalPassengers = await context.Passengers.CountAsync();
            var totalBookings = await context.Bookings.CountAsync();
            var totalRevenue = await context.Bookings
                .Where(b => b.Status != BookingStatus.Cancelled)
                .SumAsync(b => b.TotalAmount);

            var statsTable = new Table();
            statsTable.AddColumn("Показатель");
            statsTable.AddColumn("Значение");

            statsTable.AddRow("Всего рейсов", totalFlights.ToString());
            statsTable.AddRow("Всего пассажиров", totalPassengers.ToString());
            statsTable.AddRow("Всего бронирований", totalBookings.ToString());
            statsTable.AddRow("Общая выручка", $"{totalRevenue:C}");

            AnsiConsole.Write(statsTable);
            AnsiConsole.WriteLine();

            // 6. Показ бронирований
            AnsiConsole.Write(new Markup("[bold blue]📋 Все бронирования:[/]"));
            AnsiConsole.WriteLine();

            var bookings = await bookingService.GetAllBookingsAsync();
            foreach (var b in bookings)
            {
                var panel = new Panel($"[bold]Номер бронирования:[/] {b.BookingNumber}\n" +
                                    $"[bold]Статус:[/] {GetStatusText(b.Status)}\n" +
                                    $"[bold]Дата бронирования:[/] {b.BookingDate:dd.MM.yyyy HH:mm}\n" +
                                    $"[bold]Общая сумма:[/] {b.TotalAmount:C}")
                {
                    Header = new PanelHeader($"[bold blue]Бронирование #{b.Id}[/]")
                };

                AnsiConsole.Write(panel);
            }

            AnsiConsole.WriteLine();
            AnsiConsole.Write(new Markup("[bold green]🎉 Демонстрация завершена! Система работает отлично![/]"));
        }

        private string GetStatusText(BookingStatus status)
        {
            return status switch
            {
                BookingStatus.Confirmed => "[green]Подтверждено[/]",
                BookingStatus.Cancelled => "[red]Отменено[/]",
                BookingStatus.Completed => "[blue]Завершено[/]",
                _ => status.ToString()
            };
        }
    }
}
