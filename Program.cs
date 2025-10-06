using Spectre.Console;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using AirlineTicketSystem.Data;
using AirlineTicketSystem.Services;
using AirlineTicketSystem.Models;

namespace AirlineTicketSystem
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            // Настройка DI контейнера
            var host = Host.CreateDefaultBuilder(args)
                .ConfigureServices((context, services) =>
                {
                    services.AddDbContext<AirlineDbContext>(options =>
                        options.UseSqlite("Data Source=airline.db"));
                    
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

            // Запуск приложения
            var app = new AirlineTicketApp(host.Services);
            await app.RunAsync();
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

    public class AirlineTicketApp
    {
        private readonly IServiceProvider _serviceProvider;

        public AirlineTicketApp(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task RunAsync()
        {
            AnsiConsole.Write(new FigletText("Авиабилеты").Color(Color.Blue));
            AnsiConsole.Write(new Markup("[bold green]Система продажи авиабилетов[/]").Centered());
            AnsiConsole.WriteLine();

            while (true)
            {
                var choice = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("[bold blue]Выберите действие:[/]")
                        .AddChoices(new[]
                        {
                            "🔍 Поиск рейсов",
                            "✈️ Просмотр всех рейсов",
                            "🎫 Бронирование билетов",
                            "👤 Управление пассажирами",
                            "📋 Мои бронирования",
                            "❌ Отмена бронирования",
                            "📊 Статистика",
                            "🚪 Выход"
                        }));

                try
                {
                    switch (choice)
                    {
                        case "🔍 Поиск рейсов":
                            await SearchFlightsAsync();
                            break;
                        case "✈️ Просмотр всех рейсов":
                            await ShowAllFlightsAsync();
                            break;
                        case "🎫 Бронирование билетов":
                            await BookTicketsAsync();
                            break;
                        case "👤 Управление пассажирами":
                            await ManagePassengersAsync();
                            break;
                        case "📋 Мои бронирования":
                            await ShowBookingsAsync();
                            break;
                        case "❌ Отмена бронирования":
                            await CancelBookingAsync();
                            break;
                        case "📊 Статистика":
                            await ShowStatisticsAsync();
                            break;
                        case "🚪 Выход":
                            AnsiConsole.Write(new Markup("[bold red]До свидания![/]"));
                            return;
                    }
                }
                catch (Exception ex)
                {
                    AnsiConsole.WriteException(ex);
                }

                AnsiConsole.WriteLine();
                AnsiConsole.Write(new Markup("[dim]Нажмите любую клавишу для продолжения...[/]"));
                Console.ReadKey();
                AnsiConsole.Clear();
            }
        }

        private async Task SearchFlightsAsync()
        {
            AnsiConsole.Write(new Markup("[bold blue]🔍 Поиск рейсов[/]"));
            AnsiConsole.WriteLine();

            var departureCity = AnsiConsole.Ask<string>("[green]Город отправления:[/]");
            var arrivalCity = AnsiConsole.Ask<string>("[green]Город прибытия:[/]");
            var departureDateStr = AnsiConsole.Ask<string>("[green]Дата отправления (dd.MM.yyyy) или Enter для любой:[/]");

            DateTime? departureDate = null;
            if (!string.IsNullOrEmpty(departureDateStr))
            {
                if (DateTime.TryParseExact(departureDateStr, "dd.MM.yyyy", null, System.Globalization.DateTimeStyles.None, out var parsedDate))
                {
                    departureDate = parsedDate;
                }
            }

            using var scope = _serviceProvider.CreateScope();
            var flightService = scope.ServiceProvider.GetRequiredService<IFlightService>();

            var flights = await flightService.SearchFlightsAsync(departureCity, arrivalCity, departureDate);

            if (!flights.Any())
            {
                AnsiConsole.Write(new Markup("[red]Рейсы не найдены.[/]"));
                return;
            }

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
        }

        private async Task ShowAllFlightsAsync()
        {
            AnsiConsole.Write(new Markup("[bold blue]✈️ Все рейсы[/]"));
            AnsiConsole.WriteLine();

            using var scope = _serviceProvider.CreateScope();
            var flightService = scope.ServiceProvider.GetRequiredService<IFlightService>();

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
        }

        private async Task BookTicketsAsync()
        {
            AnsiConsole.Write(new Markup("[bold blue]🎫 Бронирование билетов[/]"));
            AnsiConsole.WriteLine();

            using var scope = _serviceProvider.CreateScope();
            var flightService = scope.ServiceProvider.GetRequiredService<IFlightService>();
            var passengerService = scope.ServiceProvider.GetRequiredService<IPassengerService>();
            var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

            // Выбор рейса
            var flights = await flightService.GetAllFlightsAsync();
            var flightChoices = flights.Select(f => $"{f.FlightNumber} - {f.DepartureCity} → {f.ArrivalCity} ({f.DepartureTime:dd.MM.yyyy HH:mm})").ToList();
            
            var selectedFlightStr = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[green]Выберите рейс:[/]")
                    .AddChoices(flightChoices));

            var selectedFlight = flights[flightChoices.IndexOf(selectedFlightStr)];

            // Ввод данных пассажира
            AnsiConsole.Write(new Markup("[bold]Данные пассажира:[/]"));
            var firstName = AnsiConsole.Ask<string>("[green]Имя:[/]");
            var lastName = AnsiConsole.Ask<string>("[green]Фамилия:[/]");
            var passportNumber = AnsiConsole.Ask<string>("[green]Номер паспорта:[/]");
            var email = AnsiConsole.Ask<string>("[green]Email:[/]");
            var phoneNumber = AnsiConsole.Ask<string>("[green]Телефон:[/]");
            var dateOfBirthStr = AnsiConsole.Ask<string>("[green]Дата рождения (dd.MM.yyyy):[/]");

            if (!DateTime.TryParseExact(dateOfBirthStr, "dd.MM.yyyy", null, System.Globalization.DateTimeStyles.None, out var dateOfBirth))
            {
                AnsiConsole.Write(new Markup("[red]Неверный формат даты.[/]"));
                return;
            }

            // Поиск или создание пассажира
            var passenger = await passengerService.GetPassengerByPassportAsync(passportNumber);
            if (passenger == null)
            {
                passenger = new Passenger
                {
                    FirstName = firstName,
                    LastName = lastName,
                    PassportNumber = passportNumber,
                    Email = email,
                    PhoneNumber = phoneNumber,
                    DateOfBirth = dateOfBirth
                };
                passenger = await passengerService.CreatePassengerAsync(passenger);
            }

            // Выбор класса
            var ticketClass = AnsiConsole.Prompt(
                new SelectionPrompt<TicketClass>()
                    .Title("[green]Выберите класс:[/]")
                    .AddChoices(Enum.GetValues<TicketClass>()));

            // Расчет цены
            var priceMultiplier = ticketClass switch
            {
                TicketClass.Economy => 1.0m,
                TicketClass.Business => 2.0m,
                TicketClass.First => 3.0m,
                _ => 1.0m
            };

            var ticketPrice = selectedFlight.BasePrice * priceMultiplier;

            // Подтверждение бронирования
            var confirm = AnsiConsole.Confirm($"[green]Подтвердить бронирование за {ticketPrice:C}?[/]");
            if (!confirm)
            {
                AnsiConsole.Write(new Markup("[yellow]Бронирование отменено.[/]"));
                return;
            }

            // Создание билета и бронирования
            var ticket = new Ticket
            {
                FlightId = selectedFlight.Id,
                PassengerId = passenger.Id,
                Price = ticketPrice,
                Class = ticketClass,
                Status = TicketStatus.Active,
                BookingDate = DateTime.Now
            };

            var booking = await bookingService.CreateBookingAsync(new List<Ticket> { ticket });

            // Обновление доступных мест
            await flightService.UpdateAvailableSeatsAsync(selectedFlight.Id, 1);

            AnsiConsole.Write(new Markup($"[bold green]✅ Бронирование успешно создано![/]"));
            AnsiConsole.Write(new Markup($"[green]Номер бронирования: {booking.BookingNumber}[/]"));
            AnsiConsole.Write(new Markup($"[green]Номер билета: {ticket.TicketNumber}[/]"));
        }

        private async Task ManagePassengersAsync()
        {
            AnsiConsole.Write(new Markup("[bold blue]👤 Управление пассажирами[/]"));
            AnsiConsole.WriteLine();

            using var scope = _serviceProvider.CreateScope();
            var passengerService = scope.ServiceProvider.GetRequiredService<IPassengerService>();

            var passengers = await passengerService.GetAllPassengersAsync();

            if (!passengers.Any())
            {
                AnsiConsole.Write(new Markup("[red]Пассажиры не найдены.[/]"));
                return;
            }

            var table = new Table();
            table.AddColumn("№");
            table.AddColumn("Имя");
            table.AddColumn("Фамилия");
            table.AddColumn("Паспорт");
            table.AddColumn("Email");
            table.AddColumn("Телефон");
            table.AddColumn("Дата рождения");

            for (int i = 0; i < passengers.Count; i++)
            {
                var passenger = passengers[i];
                table.AddRow(
                    (i + 1).ToString(),
                    passenger.FirstName,
                    passenger.LastName,
                    passenger.PassportNumber,
                    passenger.Email ?? "",
                    passenger.PhoneNumber ?? "",
                    passenger.DateOfBirth.ToString("dd.MM.yyyy")
                );
            }

            AnsiConsole.Write(table);
        }

        private async Task ShowBookingsAsync()
        {
            AnsiConsole.Write(new Markup("[bold blue]📋 Мои бронирования[/]"));
            AnsiConsole.WriteLine();

            using var scope = _serviceProvider.CreateScope();
            var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

            var bookings = await bookingService.GetAllBookingsAsync();

            if (!bookings.Any())
            {
                AnsiConsole.Write(new Markup("[red]Бронирования не найдены.[/]"));
                return;
            }

            foreach (var booking in bookings)
            {
                var panel = new Panel($"[bold]Номер бронирования:[/] {booking.BookingNumber}\n" +
                                    $"[bold]Статус:[/] {GetStatusText(booking.Status)}\n" +
                                    $"[bold]Дата бронирования:[/] {booking.BookingDate:dd.MM.yyyy HH:mm}\n" +
                                    $"[bold]Общая сумма:[/] {booking.TotalAmount:C}")
                {
                    Header = new PanelHeader($"[bold blue]Бронирование #{booking.Id}[/]")
                };

                AnsiConsole.Write(panel);

                var table = new Table();
                table.AddColumn("Билет");
                table.AddColumn("Пассажир");
                table.AddColumn("Рейс");
                table.AddColumn("Класс");
                table.AddColumn("Цена");

                foreach (var ticket in booking.Tickets)
                {
                    table.AddRow(
                        ticket.TicketNumber,
                        $"{ticket.Passenger.FirstName} {ticket.Passenger.LastName}",
                        $"{ticket.Flight.FlightNumber} ({ticket.Flight.DepartureCity} → {ticket.Flight.ArrivalCity})",
                        ticket.Class.ToString(),
                        $"{ticket.Price:C}"
                    );
                }

                AnsiConsole.Write(table);
                AnsiConsole.WriteLine();
            }
        }

        private async Task CancelBookingAsync()
        {
            AnsiConsole.Write(new Markup("[bold blue]❌ Отмена бронирования[/]"));
            AnsiConsole.WriteLine();

            using var scope = _serviceProvider.CreateScope();
            var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

            var bookingNumber = AnsiConsole.Ask<string>("[green]Введите номер бронирования:[/]");

            var booking = await bookingService.GetBookingByNumberAsync(bookingNumber);
            if (booking == null)
            {
                AnsiConsole.Write(new Markup("[red]Бронирование не найдено.[/]"));
                return;
            }

            if (booking.Status == BookingStatus.Cancelled)
            {
                AnsiConsole.Write(new Markup("[red]Бронирование уже отменено.[/]"));
                return;
            }

            var confirm = AnsiConsole.Confirm($"[red]Вы уверены, что хотите отменить бронирование {bookingNumber}?[/]");
            if (!confirm)
            {
                AnsiConsole.Write(new Markup("[yellow]Отмена бронирования отменена.[/]"));
                return;
            }

            var success = await bookingService.CancelBookingAsync(booking.Id);
            if (success)
            {
                AnsiConsole.Write(new Markup("[bold green]✅ Бронирование успешно отменено![/]"));
            }
            else
            {
                AnsiConsole.Write(new Markup("[red]Ошибка при отмене бронирования.[/]"));
            }
        }

        private async Task ShowStatisticsAsync()
        {
            AnsiConsole.Write(new Markup("[bold blue]📊 Статистика[/]"));
            AnsiConsole.WriteLine();

            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AirlineDbContext>();

            var totalFlights = await context.Flights.CountAsync();
            var totalPassengers = await context.Passengers.CountAsync();
            var totalBookings = await context.Bookings.CountAsync();
            var totalRevenue = await context.Bookings
                .Where(b => b.Status != BookingStatus.Cancelled)
                .SumAsync(b => b.TotalAmount);

            var stats = new Table();
            stats.AddColumn("Показатель");
            stats.AddColumn("Значение");

            stats.AddRow("Всего рейсов", totalFlights.ToString());
            stats.AddRow("Всего пассажиров", totalPassengers.ToString());
            stats.AddRow("Всего бронирований", totalBookings.ToString());
            stats.AddRow("Общая выручка", $"{totalRevenue:C}");

            AnsiConsole.Write(stats);
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
