using Spectre.Console;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using AirlineTicketSystem.Data;
using AirlineTicketSystem.Services;
using AirlineTicketSystem.Models;
using System.Globalization;

namespace AirlineTicketSystem
{
    public class Program
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
            while (true)
            {
                AnsiConsole.Write(new Markup("[bold blue]🔍 Поиск рейсов[/]"));
                AnsiConsole.WriteLine();

                var departureCity = AnsiConsole.Prompt(
                    new TextPrompt<string>("[green]Город отправления (или 'назад' для выхода):[/]")
                        .AllowEmpty());

                if (string.IsNullOrWhiteSpace(departureCity) || departureCity.ToLower() == "назад")
                {
                    return;
                }

                var arrivalCity = AnsiConsole.Prompt(
                    new TextPrompt<string>("[green]Город прибытия (или 'назад' для выхода):[/]")
                        .AllowEmpty());

                if (string.IsNullOrWhiteSpace(arrivalCity) || arrivalCity.ToLower() == "назад")
                {
                    return;
                }

                var departureDateStr = AnsiConsole.Ask<string>("[green]Дата отправления (dd.MM.yyyy) или Enter для любой:[/]", "");

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
                }
                else
                {
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

                AnsiConsole.WriteLine();
                var continueSearch = AnsiConsole.Confirm("[yellow]Выполнить новый поиск?[/]", false);
                if (!continueSearch)
                {
                    return;
                }
                AnsiConsole.Clear();
            }
        }

        private async Task ShowAllFlightsAsync()
        {
            while (true)
            {
                AnsiConsole.Write(new Markup("[bold blue]✈️ Все рейсы[/]"));
                AnsiConsole.WriteLine();

                using var scope = _serviceProvider.CreateScope();
                var flightService = scope.ServiceProvider.GetRequiredService<IFlightService>();

                var flights = await flightService.GetAllFlightsAsync();

                if (!flights.Any())
                {
                    AnsiConsole.Write(new Markup("[red]Рейсы не найдены.[/]"));
                    AnsiConsole.WriteLine();
                    AnsiConsole.Write(new Markup("[dim]Нажмите любую клавишу для возврата...[/]"));
                    Console.ReadKey();
                    return;
                }

                var table = new Table();
                table.Border = TableBorder.Rounded;
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
                    var seatsColor = flight.AvailableSeats > 50 ? "green" : 
                                    flight.AvailableSeats > 10 ? "yellow" : "red";
                    
                    table.AddRow(
                        (i + 1).ToString(),
                        flight.FlightNumber,
                        flight.Airline.Name,
                        flight.DepartureCity,
                        flight.ArrivalCity,
                        flight.DepartureTime.ToString("dd.MM.yyyy HH:mm"),
                        flight.ArrivalTime.ToString("dd.MM.yyyy HH:mm"),
                        $"[{seatsColor}]{flight.AvailableSeats}[/]",
                        $"{flight.BasePrice:C}"
                    );
                }

                AnsiConsole.Write(table);
                AnsiConsole.WriteLine();

                var action = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("[green]Что дальше?[/]")
                        .AddChoices(new[]
                        {
                            "🔄 Обновить список",
                            "🎫 Забронировать билет",
                            "🔙 Назад"
                        }));

                if (action == "🔙 Назад")
                {
                    return;
                }
                else if (action == "🎫 Забронировать билет")
                {
                    AnsiConsole.Clear();
                    await BookTicketsAsync();
                    return;
                }
                
                AnsiConsole.Clear();
            }
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
            if (!flights.Any())
            {
                AnsiConsole.Write(new Markup("[red]Нет доступных рейсов.[/]"));
                return;
            }

            var flightChoices = flights.Select(f => $"{f.FlightNumber} - {f.DepartureCity} → {f.ArrivalCity} ({f.DepartureTime:dd.MM.yyyy HH:mm})").ToList();
            flightChoices.Add("🔙 Назад");
            
            var selectedFlightStr = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[green]Выберите рейс:[/]")
                    .AddChoices(flightChoices));

            if (selectedFlightStr == "🔙 Назад")
            {
                return;
            }

            var selectedFlight = flights[flightChoices.IndexOf(selectedFlightStr)];

            // Проверка доступности мест
            if (selectedFlight.AvailableSeats <= 0)
            {
                AnsiConsole.Write(new Markup("[red]На выбранном рейсе нет свободных мест.[/]"));
                return;
            }

            // Ввод данных пассажира
            AnsiConsole.WriteLine();
            AnsiConsole.Write(new Markup("[bold]Данные пассажира:[/]"));
            AnsiConsole.WriteLine();
            
            var firstName = AnsiConsole.Prompt(
                new TextPrompt<string>("[green]Имя (или 'назад' для отмены):[/]")
                    .AllowEmpty());
            
            if (string.IsNullOrWhiteSpace(firstName) || firstName.ToLower() == "назад")
            {
                AnsiConsole.Write(new Markup("[yellow]Бронирование отменено.[/]"));
                return;
            }

            var lastName = AnsiConsole.Prompt(
                new TextPrompt<string>("[green]Фамилия (или 'назад' для отмены):[/]")
                    .AllowEmpty());
            
            if (string.IsNullOrWhiteSpace(lastName) || lastName.ToLower() == "назад")
            {
                AnsiConsole.Write(new Markup("[yellow]Бронирование отменено.[/]"));
                return;
            }

            var passportNumber = AnsiConsole.Prompt(
                new TextPrompt<string>("[green]Номер паспорта (или 'назад' для отмены):[/]")
                    .AllowEmpty());
            
            if (string.IsNullOrWhiteSpace(passportNumber) || passportNumber.ToLower() == "назад")
            {
                AnsiConsole.Write(new Markup("[yellow]Бронирование отменено.[/]"));
                return;
            }

            var email = AnsiConsole.Prompt(
                new TextPrompt<string>("[green]Email (или 'назад' для отмены):[/]")
                    .AllowEmpty());
            
            if (string.IsNullOrWhiteSpace(email) || email.ToLower() == "назад")
            {
                AnsiConsole.Write(new Markup("[yellow]Бронирование отменено.[/]"));
                return;
            }

            var phoneNumber = AnsiConsole.Prompt(
                new TextPrompt<string>("[green]Телефон (или 'назад' для отмены):[/]")
                    .AllowEmpty());
            
            if (string.IsNullOrWhiteSpace(phoneNumber) || phoneNumber.ToLower() == "назад")
            {
                AnsiConsole.Write(new Markup("[yellow]Бронирование отменено.[/]"));
                return;
            }

            var dateOfBirthStr = AnsiConsole.Prompt(
                new TextPrompt<string>("[green]Дата рождения (dd.MM.yyyy) или 'назад' для отмены:[/]")
                    .AllowEmpty());

            if (string.IsNullOrWhiteSpace(dateOfBirthStr) || dateOfBirthStr.ToLower() == "назад")
            {
                AnsiConsole.Write(new Markup("[yellow]Бронирование отменено.[/]"));
                return;
            }

            if (!DateTime.TryParseExact(dateOfBirthStr, "dd.MM.yyyy", null, System.Globalization.DateTimeStyles.None, out var dateOfBirth))
            {
                AnsiConsole.Write(new Markup("[red]Неверный формат даты. Бронирование отменено.[/]"));
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
                AnsiConsole.Write(new Markup("[green]✅ Новый пассажир добавлен в систему.[/]"));
                AnsiConsole.WriteLine();
            }
            else
            {
                AnsiConsole.Write(new Markup("[green]✅ Пассажир найден в системе.[/]"));
                AnsiConsole.WriteLine();
            }

            // Выбор класса
            var classChoices = new List<string>
            {
                "Economy",
                "Business",
                "First",
                "🔙 Назад"
            };

            var selectedClass = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[green]Выберите класс:[/]")
                    .AddChoices(classChoices));

            if (selectedClass == "🔙 Назад")
            {
                AnsiConsole.Write(new Markup("[yellow]Бронирование отменено.[/]"));
                return;
            }

            var ticketClass = Enum.Parse<TicketClass>(selectedClass);

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
            AnsiConsole.WriteLine();
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

            AnsiConsole.WriteLine();
            AnsiConsole.Write(new Markup($"[bold green]✅ Бронирование успешно создано![/]"));
            AnsiConsole.Write(new Markup($"[green]Номер бронирования: {booking.BookingNumber}[/]"));
            AnsiConsole.Write(new Markup($"[green]Номер билета: {ticket.TicketNumber}[/]"));
        }

        private async Task ManagePassengersAsync()
        {
            while (true)
            {
                AnsiConsole.Write(new Markup("[bold blue]👤 Управление пассажирами[/]"));
                AnsiConsole.WriteLine();

                using var scope = _serviceProvider.CreateScope();
                var passengerService = scope.ServiceProvider.GetRequiredService<IPassengerService>();

                var action = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("[green]Выберите действие:[/]")
                        .AddChoices(new[]
                        {
                            "📋 Просмотр всех пассажиров",
                            "✏️ Редактирование пассажира",
                            "🔙 Назад"
                        }));

                if (action == "🔙 Назад")
                {
                    return;
                }

                if (action == "📋 Просмотр всех пассажиров")
                {
                    await ShowAllPassengersAsync();
                }
                else if (action == "✏️ Редактирование пассажира")
                {
                    await EditPassengerAsync();
                }

                AnsiConsole.WriteLine();
                AnsiConsole.Write(new Markup("[dim]Нажмите любую клавишу для продолжения...[/]"));
                Console.ReadKey();
                AnsiConsole.Clear();
            }
        }

        private async Task ShowAllPassengersAsync()
        {
            AnsiConsole.Write(new Markup("[bold blue]📋 Все пассажиры[/]"));
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

        private async Task EditPassengerAsync()
        {
            AnsiConsole.Write(new Markup("[bold blue]✏️ Редактирование пассажира[/]"));
            AnsiConsole.WriteLine();

            using var scope = _serviceProvider.CreateScope();
            var passengerService = scope.ServiceProvider.GetRequiredService<IPassengerService>();

            var passengers = await passengerService.GetAllPassengersAsync();

            if (!passengers.Any())
            {
                AnsiConsole.Write(new Markup("[red]Пассажиры не найдены.[/]"));
                return;
            }

            // Добавляем опцию "Назад" в список выбора
            var passengerChoices = passengers.Select(p => 
                $"{p.FirstName} {p.LastName} - {p.PassportNumber}").ToList();
            passengerChoices.Add("🔙 Назад");

            var selectedPassengerStr = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[green]Выберите пассажира для редактирования:[/]")
                    .AddChoices(passengerChoices));

            if (selectedPassengerStr == "🔙 Назад")
            {
                return;
            }

            var selectedPassenger = passengers[passengerChoices.IndexOf(selectedPassengerStr)];

            // Показываем текущие данные
            var panel = new Panel(
                $"[bold]Имя:[/] {selectedPassenger.FirstName}\n" +
                $"[bold]Фамилия:[/] {selectedPassenger.LastName}\n" +
                $"[bold]Паспорт:[/] {selectedPassenger.PassportNumber}\n" +
                $"[bold]Email:[/] {selectedPassenger.Email ?? "не указан"} [dim](не редактируется)[/]\n" +
                $"[bold]Телефон:[/] {selectedPassenger.PhoneNumber ?? "не указан"}\n" +
                $"[bold]Дата рождения:[/] {selectedPassenger.DateOfBirth:dd.MM.yyyy}")
            {
                Header = new PanelHeader("[bold blue]Текущие данные пассажира[/]")
            };

            AnsiConsole.Write(panel);
            AnsiConsole.WriteLine();

            // Запрашиваем новые данные
            AnsiConsole.Write(new Markup("[bold]Введите новые данные (нажмите Enter, чтобы оставить текущее значение):[/]"));
            AnsiConsole.WriteLine();

            var firstName = AnsiConsole.Ask<string>("[green]Имя:[/]", selectedPassenger.FirstName);
            var lastName = AnsiConsole.Ask<string>("[green]Фамилия:[/]", selectedPassenger.LastName);
            var passportNumber = AnsiConsole.Ask<string>("[green]Номер паспорта:[/]", selectedPassenger.PassportNumber);
            var phoneNumber = AnsiConsole.Ask<string>("[green]Телефон:[/]", selectedPassenger.PhoneNumber ?? "");
            
            var dateOfBirthStr = AnsiConsole.Ask<string>(
                "[green]Дата рождения (dd.MM.yyyy):[/]", 
                selectedPassenger.DateOfBirth.ToString("dd.MM.yyyy"));

            DateTime dateOfBirth = selectedPassenger.DateOfBirth;
            if (!string.IsNullOrEmpty(dateOfBirthStr) && dateOfBirthStr != selectedPassenger.DateOfBirth.ToString("dd.MM.yyyy"))
            {
                if (!DateTime.TryParseExact(dateOfBirthStr, "dd.MM.yyyy", null, System.Globalization.DateTimeStyles.None, out dateOfBirth))
                {
                    AnsiConsole.Write(new Markup("[red]Неверный формат даты. Изменения не применены.[/]"));
                    return;
                }
            }

            // Подтверждение изменений
            var confirm = AnsiConsole.Confirm("[yellow]Сохранить изменения?[/]");
            if (!confirm)
            {
                AnsiConsole.Write(new Markup("[yellow]Изменения отменены.[/]"));
                return;
            }

            // Обновляем данные
            selectedPassenger.FirstName = firstName;
            selectedPassenger.LastName = lastName;
            selectedPassenger.PassportNumber = passportNumber;
            selectedPassenger.PhoneNumber = string.IsNullOrWhiteSpace(phoneNumber) ? null : phoneNumber;
            selectedPassenger.DateOfBirth = dateOfBirth;

            var success = await passengerService.UpdatePassengerAsync(selectedPassenger);

            if (success)
            {
                AnsiConsole.Write(new Markup("[bold green]✅ Данные пассажира успешно обновлены![/]"));
            }
            else
            {
                AnsiConsole.Write(new Markup("[red]Ошибка при обновлении данных пассажира.[/]"));
            }
        }

        private async Task ShowBookingsAsync()
        {
            while (true)
            {
                AnsiConsole.Write(new Markup("[bold blue]📋 Мои бронирования[/]"));
                AnsiConsole.WriteLine();

                using var scope = _serviceProvider.CreateScope();
                var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

                var bookings = await bookingService.GetAllBookingsAsync();

                if (!bookings.Any())
                {
                    AnsiConsole.Write(new Markup("[red]Бронирования не найдены.[/]"));
                    AnsiConsole.WriteLine();
                    AnsiConsole.Write(new Markup("[dim]Нажмите любую клавишу для возврата...[/]"));
                    Console.ReadKey();
                    return;
                }

                foreach (var booking in bookings)
                {
                    var statusColor = booking.Status == BookingStatus.Confirmed ? "green" : 
                                     booking.Status == BookingStatus.Cancelled ? "red" : "blue";
                    
                    var panel = new Panel($"[bold]Номер бронирования:[/] {booking.BookingNumber}\n" +
                                        $"[bold]Статус:[/] {GetStatusText(booking.Status)}\n" +
                                        $"[bold]Дата бронирования:[/] {booking.BookingDate:dd.MM.yyyy HH:mm}\n" +
                                        $"[bold]Общая сумма:[/] {booking.TotalAmount:C}")
                    {
                        Header = new PanelHeader($"[bold {statusColor}]Бронирование #{booking.Id}[/]"),
                        Border = BoxBorder.Rounded
                    };

                    AnsiConsole.Write(panel);

                    var table = new Table();
                    table.Border = TableBorder.Rounded;
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

                var action = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("[green]Что дальше?[/]")
                        .AddChoices(new[]
                        {
                            "🔄 Обновить список",
                            "❌ Отменить бронирование",
                            "🔙 Назад"
                        }));

                if (action == "🔙 Назад")
                {
                    return;
                }
                else if (action == "❌ Отменить бронирование")
                {
                    AnsiConsole.Clear();
                    await CancelBookingAsync();
                    return;
                }
                
                AnsiConsole.Clear();
            }
        }

        private async Task CancelBookingAsync()
        {
            while (true)
            {
                AnsiConsole.Write(new Markup("[bold blue]❌ Отмена бронирования[/]"));
                AnsiConsole.WriteLine();

                using var scope = _serviceProvider.CreateScope();
                var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

                var bookingNumber = AnsiConsole.Prompt(
                    new TextPrompt<string>("[green]Введите номер бронирования (или 'назад' для выхода):[/]")
                        .AllowEmpty());

                if (string.IsNullOrWhiteSpace(bookingNumber) || bookingNumber.ToLower() == "назад")
                {
                    return;
                }

                var booking = await bookingService.GetBookingByNumberAsync(bookingNumber);
                if (booking == null)
                {
                    AnsiConsole.Write(new Markup("[red]Бронирование не найдено.[/]"));
                    AnsiConsole.WriteLine();
                    var retry = AnsiConsole.Confirm("[yellow]Попробовать снова?[/]");
                    if (!retry)
                    {
                        return;
                    }
                    AnsiConsole.Clear();
                    continue;
                }

                if (booking.Status == BookingStatus.Cancelled)
                {
                    AnsiConsole.Write(new Markup("[red]Бронирование уже отменено.[/]"));
                    AnsiConsole.WriteLine();
                    var retry = AnsiConsole.Confirm("[yellow]Попробовать с другим номером?[/]");
                    if (!retry)
                    {
                        return;
                    }
                    AnsiConsole.Clear();
                    continue;
                }

                // Показываем детали бронирования
                var panel = new Panel(
                    $"[bold]Номер бронирования:[/] {booking.BookingNumber}\n" +
                    $"[bold]Статус:[/] {GetStatusText(booking.Status)}\n" +
                    $"[bold]Дата бронирования:[/] {booking.BookingDate:dd.MM.yyyy HH:mm}\n" +
                    $"[bold]Общая сумма:[/] {booking.TotalAmount:C}\n" +
                    $"[bold]Количество билетов:[/] {booking.Tickets.Count}")
                {
                    Header = new PanelHeader("[bold blue]Детали бронирования[/]")
                };

                AnsiConsole.Write(panel);
                AnsiConsole.WriteLine();

                var confirm = AnsiConsole.Confirm($"[red]Вы уверены, что хотите отменить это бронирование?[/]");
                if (!confirm)
                {
                    AnsiConsole.Write(new Markup("[yellow]Отмена бронирования отменена.[/]"));
                    return;
                }

                var success = await bookingService.CancelBookingAsync(booking.Id);
                if (success)
                {
                    // Восстанавливаем места на рейсах
                    var flightService = scope.ServiceProvider.GetRequiredService<IFlightService>();
                    foreach (var ticket in booking.Tickets)
                    {
                        var flight = await flightService.GetFlightByIdAsync(ticket.FlightId);
                        if (flight != null)
                        {
                            flight.AvailableSeats++;
                            var context = scope.ServiceProvider.GetRequiredService<AirlineDbContext>();
                            await context.SaveChangesAsync();
                        }
                    }

                    AnsiConsole.Write(new Markup("[bold green]✅ Бронирование успешно отменено![/]"));
                }
                else
                {
                    AnsiConsole.Write(new Markup("[red]Ошибка при отмене бронирования.[/]"));
                }

                AnsiConsole.WriteLine();
                var another = AnsiConsole.Confirm("[yellow]Отменить еще одно бронирование?[/]", false);
                if (!another)
                {
                    return;
                }
                AnsiConsole.Clear();
            }
        }

        private async Task ShowStatisticsAsync()
        {
            while (true)
            {
                AnsiConsole.Write(new Markup("[bold blue]📊 Статистика системы[/]"));
                AnsiConsole.WriteLine();

                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AirlineDbContext>();

                var totalFlights = await context.Flights.CountAsync();
                var totalPassengers = await context.Passengers.CountAsync();
                var totalBookings = await context.Bookings.CountAsync();
                var activeBookings = await context.Bookings.CountAsync(b => b.Status == BookingStatus.Confirmed);
                var cancelledBookings = await context.Bookings.CountAsync(b => b.Status == BookingStatus.Cancelled);
                var totalRevenue = await context.Bookings
                    .Where(b => b.Status != BookingStatus.Cancelled)
                    .SumAsync(b => b.TotalAmount);
                var totalSeats = await context.Flights.SumAsync(f => f.TotalSeats);
                var availableSeats = await context.Flights.SumAsync(f => f.AvailableSeats);
                var occupiedSeats = totalSeats - availableSeats;
                var occupancyRate = totalSeats > 0 ? (double)occupiedSeats / totalSeats * 100 : 0;

                var stats = new Table();
                stats.Border = TableBorder.Rounded;
                stats.AddColumn(new TableColumn("Показатель").Centered());
                stats.AddColumn(new TableColumn("Значение").Centered());

                stats.AddRow("[bold]Всего рейсов[/]", $"[green]{totalFlights}[/]");
                stats.AddRow("[bold]Всего пассажиров[/]", $"[green]{totalPassengers}[/]");
                stats.AddRow("[bold]Всего бронирований[/]", $"[green]{totalBookings}[/]");
                stats.AddRow("  - Активных", $"[green]{activeBookings}[/]");
                stats.AddRow("  - Отменённых", $"[red]{cancelledBookings}[/]");
                stats.AddRow("[bold]Общая выручка[/]", $"[blue]{totalRevenue:C}[/]");
                stats.AddRow("[bold]Всего мест[/]", $"[yellow]{totalSeats}[/]");
                stats.AddRow("  - Занято", $"[red]{occupiedSeats}[/]");
                stats.AddRow("  - Свободно", $"[green]{availableSeats}[/]");
                stats.AddRow("[bold]Заполняемость[/]", $"[cyan]{occupancyRate:F2}%[/]");

                AnsiConsole.Write(stats);
                AnsiConsole.WriteLine();

                // Топ популярных направлений
                var topRoutes = await context.Tickets
                    .Include(t => t.Flight)
                    .Where(t => t.Status != TicketStatus.Cancelled)
                    .GroupBy(t => new { t.Flight.DepartureCity, t.Flight.ArrivalCity })
                    .Select(g => new 
                    { 
                        Route = $"{g.Key.DepartureCity} → {g.Key.ArrivalCity}",
                        Count = g.Count(),
                        Revenue = g.Sum(t => t.Price)
                    })
                    .OrderByDescending(r => r.Count)
                    .Take(5)
                    .ToListAsync();

                if (topRoutes.Any())
                {
                    AnsiConsole.Write(new Markup("[bold blue]🔥 Топ-5 популярных направлений:[/]"));
                    AnsiConsole.WriteLine();

                    var routesTable = new Table();
                    routesTable.Border = TableBorder.Rounded;
                    routesTable.AddColumn("Место");
                    routesTable.AddColumn("Направление");
                    routesTable.AddColumn("Билетов продано");
                    routesTable.AddColumn("Выручка");

                    for (int i = 0; i < topRoutes.Count; i++)
                    {
                        var medal = i == 0 ? "🥇" : i == 1 ? "🥈" : i == 2 ? "🥉" : $"{i + 1}.";
                        routesTable.AddRow(
                            medal,
                            topRoutes[i].Route,
                            topRoutes[i].Count.ToString(),
                            $"{topRoutes[i].Revenue:C}"
                        );
                    }

                    AnsiConsole.Write(routesTable);
                    AnsiConsole.WriteLine();
                }

                var action = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("[green]Что дальше?[/]")
                        .AddChoices(new[]
                        {
                            "🔄 Обновить статистику",
                            "🔙 Назад"
                        }));

                if (action == "🔙 Назад")
                {
                    return;
                }
                
                AnsiConsole.Clear();
            }
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
