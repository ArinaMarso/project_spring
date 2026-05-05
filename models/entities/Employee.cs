using System.ComponentModel.DataAnnotations;
using StoreG5G11.models.ef.processes;
using StoreG5G11.utils;

namespace StoreG5G11.models.ef.entities;

public class Employee : AModel
{
    private string _name;
    [MaxLength(25)]
    public string Name
    {
        get => _name;
        set => SetField(ref _name, value, [
            (name, value) => ValidationService.ValidStringLength(name, value, 2, 25)
        ]);
    }

    private string _firstName;
    [MaxLength(25)]
    public string FirstName
    {
        get => _firstName;
        set => SetField(ref _firstName, value, [
            (name, value) => ValidationService.ValidStringLength(name, value, 2, 25)
        ]);
    }

    private string? _secondName;
    [MaxLength(25)]
    public string? SecondName
    {
        get => _secondName;
        set => SetField(ref _secondName, value, [
            (name, value) =>
            {
                if (value != null) ValidationService.ValidStringLength(name, value, 2, 25);
            }
        ]);
    }

    private string _position = "Консультант";
    [MaxLength(50)]
    public string Position
    {
        get => _position;
        set => SetField(ref _position, value, [
            (name, value) => ValidationService.ValidStringLength(name, value, 3, 50)
        ]);
    }

    private DateOnly _dateOfBirth;
    public DateOnly BirthDate
    {
        get => _dateOfBirth;
        set => SetField(ref _dateOfBirth, value);
    }

    private decimal _salary;
    public decimal Salary
    {
        get => _salary;
        set => SetField(ref _salary, value, [
            (name, val) => ValidationService.ValidPositiveDecimal(name, val)
        ]);
    }

    public string FullName => $"{Name} {FirstName} {SecondName ?? ""}".Trim();

    public List<Sale> Sales { get; init; } = [];

    public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(_name))
            yield return new ValidationResult("Фамилия обязательна", [nameof(Name)]);

        if (_name.Length is < 2 or > 25)
            yield return new ValidationResult("Фамилия должна содержать от 2 до 25 символов", [nameof(Name)]);

        if (string.IsNullOrWhiteSpace(_firstName))
            yield return new ValidationResult("Имя обязательно", [nameof(FirstName)]);

        if (_firstName.Length is < 2 or > 25)
            yield return new ValidationResult("Имя должно содержать от 2 до 25 символов", [nameof(FirstName)]);

        if (string.IsNullOrWhiteSpace(_position))
            yield return new ValidationResult("Должность обязательна", [nameof(Position)]);

        if (_position.Length is < 3 or > 50)
            yield return new ValidationResult("Должность должна содержать от 3 до 50 символов", [nameof(Position)]);

        if (_secondName != null && _secondName.Length is < 2 or > 25)
            yield return new ValidationResult("Отчество должно содержать от 2 до 25 символов", [nameof(SecondName)]);

        if (_salary < 1)
            yield return new ValidationResult("Зарплата должна быть положительной", [nameof(Salary)]);

        var age = DateTime.Now.Year - _dateOfBirth.Year;
        if (_dateOfBirth > DateOnly.FromDateTime(DateTime.Now.AddYears(-age)))
            age--;

        if (age < 18)
            yield return new ValidationResult("Сотрудник должен быть старше 18 лет", [nameof(BirthDate)]);
    }

    public override AModel GetCopy() => new Employee
    {
        Name = _name,
        FirstName = _firstName,
        SecondName = _secondName,
        Position = _position,
        BirthDate = _dateOfBirth,
        Salary = _salary
    };

    public override void CopyFrom(AModel other)
    {
        if (other is not Employee inst)
            throw new ArgumentException("CopyFrom может копировать только из типа Employee");

        Name = inst.Name;
        FirstName = inst.FirstName;
        SecondName = inst.SecondName;
        Position = inst.Position;
        BirthDate = inst.BirthDate;
        Salary = inst.Salary;
    }

    public override bool Equals(AModel other)
    {
        if (other is not Employee inst)
            throw new ArgumentException("Equals может сравнивать только с типом Employee");
        return Name == inst.Name && FirstName == inst.FirstName && BirthDate == inst.BirthDate;
    }
}