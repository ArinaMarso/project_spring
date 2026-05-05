using System.IO;
using System.Text;
using Microsoft.EntityFrameworkCore;
using StoreG5G11.api.ef;
using StoreG5G11.models.ef.entities;
using StoreG5G11.models.ef.processes;
using StoreG5G11.modelViews;
using StoreG5G11.src.models.ef.entities;
using StoreG5G11.src.modelViews;

namespace StoreG5G11.services;

public class MusicStoreService
{
    private readonly ApplicationContext _context;

    public MusicStoreService()
    {
        _context = ApplicationContext.Instance;
    }

    // === 1. РАБОТА С МУЗЫКАЛЬНЫМИ ИНСТРУМЕНТАМИ ===

    // Получить все инструменты с фильтрацией
    public List<Instrument> GetInstruments(InstrumentType? instrumentType = null, string? search = null)
    {
        var query = _context.Instruments.AsQueryable();

        if (instrumentType.HasValue)
        {
            query = query.Where(i => i.InstrumentType == instrumentType.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(i =>
                i.Name.Contains(search) ||
                i.Brand.Contains(search) ||
                i.Description.Contains(search));
        }

        return query.OrderBy(i => i.Brand).ThenBy(i => i.Name).ToList();
    }

    // Получить инструменты по типу
    public List<Instrument> GetStringInstruments() => GetInstruments(InstrumentType.String);
    public List<Instrument> GetWindInstruments() => GetInstruments(InstrumentType.Wind);
    public List<Instrument> GetPercussionInstruments() => GetInstruments(InstrumentType.Percussion);
    public List<Instrument> GetKeyboardInstruments() => GetInstruments(InstrumentType.Keyboard);
    public List<Instrument> GetElectronicInstruments() => GetInstruments(InstrumentType.Electronic);

    // Обновить цену инструмента
    public void UpdateInstrumentPrice(int instrumentId, decimal newPrice)
    {
        var instrument = _context.Instruments.Find(instrumentId);
        if (instrument == null)
            throw new ArgumentException($"Инструмент с ID {instrumentId} не найден");

        if (newPrice <= 0)
            throw new ArgumentException("Цена должна быть положительной");

        instrument.Price = newPrice;
        _context.SaveChanges();
    }

    // Обновить количество на складе
    public void UpdateStockQuantity(int instrumentId, int quantity)
    {
        var instrument = _context.Instruments.Find(instrumentId);
        if (instrument == null)
            throw new ArgumentException($"Инструмент с ID {instrumentId} не найден");

        if (quantity < 0)
            throw new ArgumentException("Количество не может быть отрицательным");

        instrument.StockQuantity = quantity;
        _context.SaveChanges();
    }

    // Проверить наличие инструмента
    public bool IsInstrumentAvailable(int instrumentId, int quantity = 1)
    {
        var instrument = _context.Instruments.Find(instrumentId);
        return instrument != null && instrument.StockQuantity >= quantity;
    }

    // === 2. РАБОТА С СОТРУДНИКАМИ ===

    // Получить всех сотрудников
    public List<Employee> GetAllEmployees() =>
        _context.Employees.OrderBy(e => e.Name).ThenBy(e => e.FirstName).ToList();

    // Найти сотрудника по ФИО
    public Employee? FindEmployeeByName(string name, string firstName)
    {
        return _context.Employees
            .FirstOrDefault(e =>
                e.Name.Contains(name) &&
                e.FirstName.Contains(firstName));
    }

    // Получить сотрудника по ID
    public Employee? GetEmployee(int employeeId) =>
        _context.Employees.Find(employeeId);

    // === 3. ПРОЦЕСС ПРОДАЖИ ===

    // 3.1 Создать новую продажу
    public Sale StartNewSale(int employeeId)
    {
        // Проверяем сотрудника
        var employee = _context.Employees.Find(employeeId);
        if (employee == null)
            throw new InvalidOperationException($"Сотрудник с ID {employeeId} не найден");

        // Создаем новую продажу
        var sale = new Sale
        {
            EmployeeId = employeeId,
            Date = DateTime.Now,
            Total = 0,
            PaymentMethod = (src.modelViews.PaymentMethod)PaymentMethod.Cash
        };

        _context.Sales.Add(sale);
        _context.SaveChanges();

        return sale;
    }

    // 3.2 Добавить инструмент в продажу
    public Order AddInstrumentToSale(int saleId, int instrumentId, int quantity = 1)
    {
        // Проверяем продажу
        var sale = _context.Sales
            .Include(s => s.Orders)
            .ThenInclude(o => o.Instrument)
            .FirstOrDefault(s => s.Id == saleId);

        if (sale == null)
            throw new InvalidOperationException($"Продажа с ID {saleId} не найдена");

        // Проверяем инструмент
        var instrument = _context.Instruments.Find(instrumentId);
        if (instrument == null)
            throw new InvalidOperationException($"Инструмент с ID {instrumentId} не найден");

        if (quantity <= 0)
            throw new ArgumentException("Количество должно быть больше 0");

        if (quantity > instrument.StockQuantity)
            throw new InvalidOperationException($"Недостаточно товара на складе. Доступно: {instrument.StockQuantity}");

        // Ищем, есть ли уже такой инструмент в продаже
        var existingOrder = sale.Orders.FirstOrDefault(o => o.InstrumentId == instrumentId);

        if (existingOrder != null)
        {
            // Увеличиваем количество существующего товара
            existingOrder.Count += quantity;
        }
        else
        {
            // Создаем новую позицию
            var order = new Order
            {
                SaleId = saleId,
                InstrumentId = instrumentId,
                Price = instrument.Price, // Фиксируем текущую цену инструмента
                Count = quantity
            };

            sale.Orders.Add(order);
            existingOrder = order;
        }

        // Пересчитываем общую сумму чека
        sale.Total = sale.Orders.Sum(o => o.Price * o.Count);
        _context.SaveChanges();

        return existingOrder;
    }

    // 3.3 Удалить инструмент из продажи
    public void RemoveInstrumentFromSale(int saleId, int instrumentId, int quantity = 1)
    {
        var sale = _context.Sales
            .Include(s => s.Orders)
            .FirstOrDefault(s => s.Id == saleId);

        if (sale == null)
            throw new InvalidOperationException($"Продажа с ID {saleId} не найдена");

        var order = sale.Orders.FirstOrDefault(o => o.InstrumentId == instrumentId);
        if (order == null)
            throw new InvalidOperationException($"Инструмент с ID {instrumentId} не найден в продаже");

        if (quantity <= 0)
            throw new ArgumentException("Количество должно быть больше 0");

        if (quantity >= order.Count)
        {
            // Удаляем инструмент полностью
            sale.Orders.Remove(order);
            _context.Orders.Remove(order);
        }
        else
        {
            // Уменьшаем количество
            order.Count -= quantity;
        }

        // Пересчитываем общую сумму
        sale.Total = sale.Orders.Sum(o => o.Price * o.Count);
        _context.SaveChanges();
    }

    // 3.4 Завершить продажу (оплата)
    public Receipt CompleteSale(int saleId, decimal paymentAmount, PaymentMethod paymentMethod)
    {
        var sale = _context.Sales
            .Include(s => s.Orders)
            .ThenInclude(o => o.Instrument)
            .Include(s => s.Employee)
            .FirstOrDefault(s => s.Id == saleId);

        if (sale == null)
            throw new InvalidOperationException($"Продажа с ID {saleId} не найдена");

        if (sale.IsCompleted)
            throw new InvalidOperationException("Продажа уже завершена");

        if (!sale.Orders.Any())
            throw new InvalidOperationException("Нельзя завершить пустую продажу");

        // Проверяем оплату
        if (paymentAmount < sale.Total)
            throw new InvalidOperationException($"Недостаточно средств. К оплате: {sale.Total:C}");

        // Обновляем количество на складе
        foreach (var order in sale.Orders)
        {
            var instrument = _context.Instruments.Find(order.InstrumentId);
            if (instrument != null)
            {
                instrument.StockQuantity -= order.Count;
                if (instrument.StockQuantity < 0)
                    throw new InvalidOperationException($"Недостаточно товара на складе: {instrument.Name}");
            }
        }

        // Устанавливаем метод оплаты и дату завершения
        sale.PaymentMethod = (src.modelViews.PaymentMethod)paymentMethod;
        sale.Date = DateTime.Now;

        _context.SaveChanges();

        // Создаем чек
        var receipt = new Receipt
        {
            Sale = sale,
            PaymentAmount = paymentAmount,
            Change = paymentAmount - sale.Total,
            IssueTime = DateTime.Now
        };

        return receipt;
    }

    // 3.5 Отменить продажу
    public void CancelSale(int saleId)
    {
        var sale = _context.Sales
            .Include(s => s.Orders)
            .FirstOrDefault(s => s.Id == saleId);

        if (sale == null)
            throw new InvalidOperationException($"Продажа с ID {saleId} не найдена");

        if (sale.IsCompleted)
            throw new InvalidOperationException("Завершенную продажу нельзя отменить");

        // Удаляем все позиции заказа
        _context.Orders.RemoveRange(sale.Orders);

        // Удаляем продажу
        _context.Sales.Remove(sale);
        _context.SaveChanges();
    }

    // === 4. ОТЧЕТЫ И СТАТИСТИКА ===

    // 4.1 Получить продажи за период
    public List<Sale> GetSalesByPeriod(DateTime startDate, DateTime endDate)
    {
        return _context.Sales
            .Include(s => s.Orders)
            .ThenInclude(o => o.Instrument)
            .Include(s => s.Employee)
            .Where(s => s.Date >= startDate && s.Date <= endDate && s.IsCompleted)
            .OrderByDescending(s => s.Date)
            .ToList();
    }

    // 4.2 Получить дневную выручку
    public decimal GetDailyRevenue(DateTime date)
    {
        var startOfDay = date.Date;
        var endOfDay = date.Date.AddDays(1).AddTicks(-1);

        return _context.Sales
            .Where(s => s.Date >= startOfDay && s.Date <= endOfDay && s.IsCompleted)
            .Sum(s => s.Total);
    }

    // 4.3 Статистика по сотрудникам
    public List<EmployeeStatistic> GetEmployeeStatistics(DateTime startDate, DateTime endDate)
    {
        return _context.Sales
            .Include(s => s.Employee)
            .Where(s => s.Date >= startDate && s.Date <= endDate && s.IsCompleted)
            .GroupBy(s => s.EmployeeId)
            .Select(g => new EmployeeStatistic
            {
                EmployeeId = g.Key,
                EmployeeName = g.First().Employee!.FullName,
                SalesCount = g.Count(),
                TotalRevenue = g.Sum(s => s.Total),
                AverageCheck = g.Average(s => s.Total)
            })
            .OrderByDescending(e => e.TotalRevenue)
            .ToList();
    }

    // 4.4 Популярные инструменты
    public List<PopularInstrument> GetPopularInstruments(DateTime startDate, DateTime endDate, int topN = 5)
    {
        return _context.Orders
            .Include(o => o.Instrument)
            .Include(o => o.Sale)
            .Where(o => o.Sale!.Date >= startDate && o.Sale.Date <= endDate && o.Sale.IsCompleted)
            .GroupBy(o => o.InstrumentId)
            .Select(g => new PopularInstrument
            {
                InstrumentId = g.Key,
                InstrumentName = g.First().Instrument!.Name,
                Brand = g.First().Instrument!.Brand,
                InstrumentType = g.First().Instrument!.InstrumentType,
                QuantitySold = g.Sum(o => o.Count),
                Revenue = g.Sum(o => o.Price * o.Count)
            })
            .OrderByDescending(p => p.QuantitySold)
            .Take(topN)
            .ToList();
    }

    // 4.5 Продажи по категориям инструментов
    public List<SalesByCategory> GetSalesByCategory(DateTime startDate, DateTime endDate)
    {
        return _context.Orders
            .Include(o => o.Instrument)
            .Include(o => o.Sale)
            .Where(o => o.Sale!.Date >= startDate && o.Sale.Date <= endDate && o.Sale.IsCompleted)
            .GroupBy(o => o.Instrument!.InstrumentType)
            .Select(g => new SalesByCategory
            {
                Category = g.Key,
                QuantitySold = g.Sum(o => o.Count),
                Revenue = g.Sum(o => o.Price * o.Count),
                Percentage = (decimal)g.Sum(o => o.Price * o.Count) /
                           _context.Orders
                               .Include(o => o.Sale)
                               .Where(o => o.Sale!.Date >= startDate && o.Sale.Date <= endDate && o.Sale.IsCompleted)
                               .Sum(o => o.Price * o.Count) * 100
            })
            .OrderByDescending(s => s.Revenue)
            .ToList();
    }

    // === 5. СИСТЕМНЫЕ МЕТОДЫ ===

    // 5.1 Инициализация тестовых данных
    public void InitializeTestData()
    {
        // Убедимся, что БД создана
        _context.Database.EnsureCreated();

        // Добавляем инструменты, если их нет
        if (!_context.Instruments.Any())
        {
            var instruments = new List<Instrument>
            {
                // Струнные инструменты
                new Instrument {
                    Name = "Fender Stratocaster",
                    Brand = "Fender",
                    Description = "Электрогитара, серия American Professional II",
                    Price = 85000.00m,
                    InstrumentType = InstrumentType.String,
                    StockQuantity = 5
                },
                new Instrument {
                    Name = "Yamaha C40",
                    Brand = "Yamaha",
                    Description = "Классическая гитара для начинающих",
                    Price = 15000.00m,
                    InstrumentType = InstrumentType.String,
                    StockQuantity = 10
                },
                new Instrument {
                    Name = "Gibson Les Paul",
                    Brand = "Gibson",
                    Description = "Легендарная электрогитара",
                    Price = 120000.00m,
                    InstrumentType = InstrumentType.String,
                    StockQuantity = 3
                },
                
                // Духовые инструменты
                new Instrument {
                    Name = "Yamaha YAS-280",
                    Brand = "Yamaha",
                    Description = "Альт-саксофон для студентов",
                    Price = 75000.00m,
                    InstrumentType = InstrumentType.Wind,
                    StockQuantity = 4
                },
                new Instrument {
                    Name = "Bach TR300",
                    Brand = "Bach",
                    Description = "Тромбон для начинающих",
                    Price = 45000.00m,
                    InstrumentType = InstrumentType.Wind,
                    StockQuantity = 6
                },
                
                // Ударные инструменты
                new Instrument {
                    Name = "Pearl Export",
                    Brand = "Pearl",
                    Description = "Барабанная установка 5-ти частей",
                    Price = 65000.00m,
                    InstrumentType = InstrumentType.Percussion,
                    StockQuantity = 3
                },
                new Instrument {
                    Name = "Ludwig Accent",
                    Brand = "Ludwig",
                    Description = "Барабанная установка для начинающих",
                    Price = 35000.00m,
                    InstrumentType = InstrumentType.Percussion,
                    StockQuantity = 7
                },
                
                // Клавишные инструменты
                new Instrument {
                    Name = "Yamaha P-125",
                    Brand = "Yamaha",
                    Description = "Цифровое пианино, 88 взвешенных клавиш",
                    Price = 55000.00m,
                    InstrumentType = InstrumentType.Keyboard,
                    StockQuantity = 7
                },
                new Instrument {
                    Name = "Casio CT-S1",
                    Brand = "Casio",
                    Description = "Портативная клавиатура",
                    Price = 12000.00m,
                    InstrumentType = InstrumentType.Keyboard,
                    StockQuantity = 15
                },
                
                // Электронные инструменты
                new Instrument {
                    Name = "Roland TD-17KVX",
                    Brand = "Roland",
                    Description = "Электронная барабанная установка",
                    Price = 120000.00m,
                    InstrumentType = InstrumentType.Electronic,
                    StockQuantity = 2
                },
                new Instrument {
                    Name = "Korg Minilogue",
                    Brand = "Korg",
                    Description = "Аналоговый синтезатор",
                    Price = 45000.00m,
                    InstrumentType = InstrumentType.Electronic,
                    StockQuantity = 4
                }
            };

            _context.Instruments.AddRange(instruments);
            _context.SaveChanges();
        }

        // Добавляем сотрудников, если их нет
        if (!_context.Employees.Any())
        {
            var employees = new List<Employee>
            {
                new Employee {
                    Name = "Иванов",
                    FirstName = "Алексей",
                    SecondName = "Петрович",
                    Position = "Менеджер по продажам",
                    BirthDate = new DateOnly(1985, 3, 15),
                    Salary = 65000.00m
                },
                new Employee {
                    Name = "Смирнова",
                    FirstName = "Екатерина",
                    SecondName = "Владимировна",
                    Position = "Консультант",
                    BirthDate = new DateOnly(1990, 7, 22),
                    Salary = 55000.00m
                },
                new Employee {
                    Name = "Петров",
                    FirstName = "Дмитрий",
                    SecondName = "Александрович",
                    Position = "Главный консультант",
                    BirthDate = new DateOnly(1992, 11, 5),
                    Salary = 60000.00m
                },
                new Employee {
                    Name = "Козлова",
                    FirstName = "Анна",
                    SecondName = "Игоревна",
                    Position = "Консультант",
                    BirthDate = new DateOnly(1995, 4, 18),
                    Salary = 52000.00m
                },
                new Employee {
                    Name = "Морозов",
                    FirstName = "Сергей",
                    SecondName = "Викторович",
                    Position = "Старший продавец",
                    BirthDate = new DateOnly(1988, 9, 30),
                    Salary = 58000.00m
                }
            };

            _context.Employees.AddRange(employees);
            _context.SaveChanges();
        }
    }

    // 5.2 Резервное копирование данных
    public void BackupDatabase(string backupPath)
    {
        var backupInfo = new
        {
            BackupDate = DateTime.Now,
            InstrumentsCount = _context.Instruments.Count(),
            EmployeesCount = _context.Employees.Count(),
            SalesCount = _context.Sales.Count(),
            OrdersCount = _context.Orders.Count(),
            TotalRevenue = _context.Sales.Where(s => s.IsCompleted).Sum(s => s.Total)
        };

        var json = System.Text.Json.JsonSerializer.Serialize(backupInfo);
        File.WriteAllText(backupPath, json);
    }

    // 5.3 Получить общую статистику магазина
    public StoreStatistics GetStoreStatistics()
    {
        return new StoreStatistics
        {
            TotalInstruments = _context.Instruments.Count(),
            TotalEmployees = _context.Employees.Count(),
            TotalSales = _context.Sales.Count(s => s.IsCompleted),
            TotalRevenue = _context.Sales.Where(s => s.IsCompleted).Sum(s => s.Total),
            AverageSale = _context.Sales.Where(s => s.IsCompleted).Average(s => s.Total),
            MostExpensiveInstrument = _context.Instruments.OrderByDescending(i => i.Price).FirstOrDefault(),
            MostPopularInstrument = _context.Orders
                .Include(o => o.Instrument)
                .GroupBy(o => o.InstrumentId)
                .Select(g => new
                {
                    Instrument = g.First().Instrument,
                    Count = g.Sum(o => o.Count)
                })
                .OrderByDescending(x => x.Count)
                .FirstOrDefault()?.Instrument
        };
    }
}

// === ВСПОМОГАТЕЛЬНЫЕ КЛАССЫ ===

public class Receipt
{
    public Sale Sale { get; set; } = null!;
    public decimal PaymentAmount { get; set; }
    public decimal Change { get; set; }
    public DateTime IssueTime { get; set; }

    public string Print()
    {
        var receipt = $"=== МАГАЗИН МУЗЫКАЛЬНЫХ ИНСТРУМЕНТОВ ===\n";
        receipt += $"Дата: {Sale.Date:dd.MM.yyyy HH:mm}\n";
        receipt += $"Кассир: {Sale.Employee!.FullName}\n";
        receipt += $"Номер чека: {Sale.Id}\n";
        receipt += $"Способ оплаты: {Sale.PaymentMethod}\n";
        receipt += $"------------------------\n";

        foreach (var order in Sale.Orders)
        {
            receipt += $"{order.Instrument!.Name} ({order.Instrument.Brand})\n";
            receipt += $"  {order.Price:C} x {order.Count} = {order.Total:C}\n";
        }

        receipt += $"------------------------\n";
        receipt += $"ИТОГО: {Sale.Total:C}\n";
        receipt += $"Оплата: {PaymentAmount:C}\n";
        receipt += $"Сдача: {Change:C}\n";
        receipt += $"------------------------\n";
        receipt += $"Чек выдан: {IssueTime:HH:mm:ss}\n";
        receipt += $"=== СПАСИБО ЗА ПОКУПКУ! ===\n";

        return receipt;
    }
}

public class EmployeeStatistic
{
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public int SalesCount { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal AverageCheck { get; set; }
}

public class PopularInstrument
{
    public int InstrumentId { get; set; }
    public string InstrumentName { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public InstrumentType InstrumentType { get; set; }
    public int QuantitySold { get; set; }
    public decimal Revenue { get; set; }
}

public class SalesByCategory
{
    public InstrumentType Category { get; set; }
    public int QuantitySold { get; set; }
    public decimal Revenue { get; set; }
    public decimal Percentage { get; set; }
}

public class StoreStatistics
{
    public int TotalInstruments { get; set; }
    public int TotalEmployees { get; set; }
    public int TotalSales { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal AverageSale { get; set; }
    public Instrument? MostExpensiveInstrument { get; set; }
    public Instrument? MostPopularInstrument { get; set; }
}

public static class EntityExtensions
{
    public static string FullName(this Employee employee)
    {
        return $"{employee.Name} {employee.FirstName} {employee.SecondName ?? ""}".Trim();
    }

    public static decimal Total(this Order order)
    {
        return order.Price * order.Count;
    }

    public static bool IsCompleted(this Sale sale)
    {
        return sale.Total > 0 && sale.Date != default && sale.Date <= DateTime.Now;
    }
}