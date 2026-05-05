using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using StoreG5G11.models.ef.entities;
using StoreG5G11.models.ef.processes;
using StoreG5G11.src.models.ef.entities;

namespace StoreG5G11.api.ef;

public sealed class ApplicationContext : DbContext
{
    private static readonly Lazy<ApplicationContext> _lazyInstance =
        new Lazy<ApplicationContext>(() => new ApplicationContext());

    public static ApplicationContext Instance => _lazyInstance.Value;

    public DbSet<Instrument> Instruments { get; set; } = null!;
    public DbSet<Employee> Employees { get; set; } = null!;
    public DbSet<Sale> Sales { get; set; } = null!;
    public DbSet<Order> Orders { get; set; } = null!;

    internal ApplicationContext()
    {
        ChangeTracker.LazyLoadingEnabled = false;
        ChangeTracker.AutoDetectChangesEnabled = true;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlServer(
                @"Server=DESKTOP-PK24H1S;Database=MusicStoreDB;Trusted_Connection=True;TrustServerCertificate=True;",
                options => options.EnableRetryOnFailure());

            optionsBuilder.EnableSensitiveDataLogging();
            optionsBuilder.LogTo(
                message => Console.WriteLine($"[EF] {message}"),
                Microsoft.Extensions.Logging.LogLevel.Information);
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        Console.WriteLine("Начинаем конфигурацию модели...");

        modelBuilder.Entity<Instrument>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name)
                  .HasMaxLength(50)
                  .IsRequired();

            entity.Property(e => e.Brand)
                  .HasMaxLength(50)
                  .IsRequired();

            entity.Property(e => e.Description)
                  .HasMaxLength(200)
                  .IsRequired();

            entity.Property(e => e.Price)
                  .HasPrecision(18, 2)
                  .IsRequired();

            entity.Property(e => e.StockQuantity)
                  .IsRequired()
                  .HasDefaultValue(0);

            entity.Property(e => e.InstrumentType)
                  .IsRequired()
                  .HasConversion<string>()
                  .HasMaxLength(20);

            entity.HasCheckConstraint("CK_Instrument_Name_Length", "LEN(Name) >= 2");
            entity.HasCheckConstraint("CK_Instrument_Brand_Length", "LEN(Brand) >= 2");
            entity.HasCheckConstraint("CK_Instrument_Description_Length", "LEN(Description) >= 10");
            entity.HasCheckConstraint("CK_Instrument_Price_Positive", "Price > 0");
            entity.HasCheckConstraint("CK_Instrument_Stock_NonNegative", "StockQuantity >= 0");

            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.Brand);
            entity.HasIndex(e => new { e.Name, e.Brand }).IsUnique();

            Console.WriteLine("Конфигурация Instrument завершена");
        });

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name)
                  .HasMaxLength(25)
                  .IsRequired();

            entity.Property(e => e.FirstName)
                  .HasMaxLength(25)
                  .IsRequired();

            entity.Property(e => e.SecondName)
                  .HasMaxLength(25);

            entity.Property(e => e.Salary)
                  .HasPrecision(18, 2)
                  .IsRequired();

            entity.Property(e => e.BirthDate)
                  .IsRequired();

            entity.Property(e => e.Position)
                  .HasMaxLength(50)
                  .IsRequired();

            entity.HasCheckConstraint("CK_Employee_Name_Length", "LEN(Name) >= 2");
            entity.HasCheckConstraint("CK_Employee_FirstName_Length", "LEN(FirstName) >= 2");
            entity.HasCheckConstraint("CK_Employee_Salary_Positive", "Salary > 0");
            entity.HasCheckConstraint("CK_Employee_Age", "DATEDIFF(year, BirthDate, GETDATE()) >= 18");

            Console.WriteLine("Конфигурация Employee завершена");
        });

        modelBuilder.Entity<Sale>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Total)
                  .HasPrecision(18, 2)
                  .IsRequired();

            entity.Property(e => e.Date)
                  .IsRequired()
                  .HasDefaultValueSql("GETDATE()");

            entity.Property(e => e.PaymentMethod)
                  .HasMaxLength(20)
                  .IsRequired()
                  .HasConversion<string>();

            entity.HasCheckConstraint("CK_Sale_Total_NonNegative", "Total >= 0");
            entity.HasCheckConstraint("CK_Sale_Date_NotFuture", "Date <= GETDATE()");

            entity.HasOne(s => s.Employee)
                  .WithMany(e => e.Sales)
                  .HasForeignKey(s => s.EmployeeId)
                  .OnDelete(DeleteBehavior.Restrict)
                  .IsRequired();

            entity.HasMany(s => s.Orders)
                  .WithOne(o => o.Sale)
                  .HasForeignKey(o => o.SaleId)
                  .OnDelete(DeleteBehavior.Cascade);

            Console.WriteLine("Конфигурация Sale завершена");
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Price)
                  .HasPrecision(18, 2)
                  .IsRequired();

            entity.Property(e => e.Count)
                  .IsRequired()
                  .HasDefaultValue(1);

            entity.HasCheckConstraint("CK_Order_Price_Positive", "Price > 0");
            entity.HasCheckConstraint("CK_Order_Count_Positive", "Count > 0");

            entity.HasOne(o => o.Sale)
                  .WithMany(s => s.Orders)
                  .HasForeignKey(o => o.SaleId)
                  .OnDelete(DeleteBehavior.Cascade)
                  .IsRequired();

            entity.HasOne(o => o.Instrument)
                  .WithMany(i => i.Orders)
                  .HasForeignKey(o => o.InstrumentId)
                  .OnDelete(DeleteBehavior.Restrict)
                  .IsRequired();

            entity.HasIndex(o => new { o.SaleId, o.InstrumentId });

            Console.WriteLine("Конфигурация Order завершена");
        });

        SeedData(modelBuilder);

        Console.WriteLine("Конфигурация модели завершена");
    }

    private void SeedData(ModelBuilder modelBuilder)
    {
        Console.WriteLine("Начинаем заполнение начальными данными...");

        modelBuilder.Entity<Instrument>().HasData(
            new Instrument
            {
                Id = 1,
                Name = "Fender Stratocaster",
                Brand = "Fender",
                Description = "Электрогитара, серия American Professional II",
                Price = 8500.00m,
                InstrumentType = InstrumentType.String,
                StockQuantity = 5
            },
            new Instrument
            {
                Id = 2,
                Name = "Yamaha C40",
                Brand = "Yamaha",
                Description = "Классическая гитара для начинающих",
                Price = 1500.00m,
                InstrumentType = InstrumentType.String,
                StockQuantity = 10
            },
            new Instrument
            {
                Id = 3,
                Name = "Pearl Export",
                Brand = "Pearl",
                Description = "Барабанная установка 5-ти частей",
                Price = 6500.00m,
                InstrumentType = InstrumentType.Percussion,
                StockQuantity = 3
            },
            new Instrument
            {
                Id = 4,
                Name = "Yamaha P-125",
                Brand = "Yamaha",
                Description = "Цифровое пианино, 88 взвешенных клавиш",
                Price = 5500.00m,
                InstrumentType = InstrumentType.Keyboard,
                StockQuantity = 7
            },
            new Instrument
            {
                Id = 5,
                Name = "Yamaha YAS-280",
                Brand = "Yamaha",
                Description = "Альт-саксофон для студентов",
                Price = 7000.00m,
                InstrumentType = InstrumentType.Wind,
                StockQuantity = 4
            },
            new Instrument
            {
                Id = 6,
                Name = "Roland TD-17KVX",
                Brand = "Roland",
                Description = "Электронная барабанная установка",
                Price = 12000.00m,
                InstrumentType = InstrumentType.Electronic,
                StockQuantity = 2
            }
        );

        modelBuilder.Entity<Employee>().HasData(
            new Employee
            {
                Id = 1,
                Name = "Иванов",
                FirstName = "Алексей",
                SecondName = "Петрович",
                BirthDate = new DateOnly(1985, 3, 15),
                Salary = 65000.00m,
                Position = "Менеджер по продажам"
            },
            new Employee
            {
                Id = 2,
                Name = "Смирнова",
                FirstName = "Екатерина",
                SecondName = "Владимировна",
                BirthDate = new DateOnly(1990, 7, 22),
                Salary = 55000.00m,
                Position = "Консультант"
            },
            new Employee
            {
                Id = 3,
                Name = "Петров",
                FirstName = "Дмитрий",
                SecondName = "Александрович",
                BirthDate = new DateOnly(1992, 11, 5),
                Salary = 60000.00m,
                Position = "Главный консультант"
            }
        );

        Console.WriteLine("Начальные данные добавлены");
    }

    public override int SaveChanges()
    {
        Console.WriteLine($"SaveChanges вызван. Изменений: {ChangeTracker.Entries().Count(e => e.State != EntityState.Unchanged)}");

        try
        {
            foreach (var entry in ChangeTracker.Entries<Sale>()
                         .Where(e => e.State == EntityState.Added))
            {
                if (entry.Entity.Date == default)
                {
                    entry.Entity.Date = DateTime.Now;
                }
            }

            var entries = ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified)
                .Select(e => e.Entity)
                .OfType<IValidatableObject>();

            foreach (var entity in entries)
            {
                var validationContext = new ValidationContext(entity);
                var results = new List<ValidationResult>();

                if (!Validator.TryValidateObject(entity, validationContext, results, true))
                {
                    var errors = string.Join("; ", results.Select(r => r.ErrorMessage));
                    throw new ValidationException($"Ошибка валидации для {entity.GetType().Name}: {errors}");
                }
            }

            return base.SaveChanges();
        }
        catch (DbUpdateException ex)
        {
            Console.WriteLine($"Ошибка сохранения в БД: {ex.Message}");
            Console.WriteLine($"Inner Exception: {ex.InnerException?.Message}");

            var errorMessage = ex.InnerException?.Message ?? ex.Message;
            if (errorMessage.Contains("PRIMARY KEY") || errorMessage.Contains("duplicate key"))
                throw new InvalidOperationException("Ошибка: дублирование ключа или нарушение уникальности данных");
            else if (errorMessage.Contains("FOREIGN KEY"))
                throw new InvalidOperationException("Ошибка: нарушение ссылочной целостности данных");
            else if (errorMessage.Contains("CHECK constraint"))
                throw new InvalidOperationException("Ошибка: нарушение проверочного ограничения в данных");
            else
                throw new InvalidOperationException($"Ошибка сохранения в базе данных: {errorMessage}");
        }
        catch (ValidationException ex)
        {
            Console.WriteLine($"Ошибка валидации: {ex.Message}");
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Неожиданная ошибка: {ex.Message}");
            throw;
        }
    }

    public void InitializeDatabase()
    {
        try
        {
            Console.WriteLine("Инициализация базы данных...");
            Database.EnsureCreated();

            Console.WriteLine($"База данных создана: {Database.GetDbConnection().Database}");
            Console.WriteLine($"Путь к базе: {Database.GetDbConnection().DataSource}");

            if (!Instruments.Any())
            {
                Database.Migrate();
            }
            else
            {
                Console.WriteLine($"В базе уже есть {Instruments.Count()} инструментов и {Employees.Count()} сотрудников");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка инициализации базы данных: {ex.Message}");
            throw;
        }
    }

    public static ApplicationContext GetSafeInstance()
    {
        try
        {
            var instance = Instance;
            instance.InitializeDatabase();
            return instance;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Не удалось получить экземпляр ApplicationContext: {ex.Message}");
            throw;
        }
    }
}