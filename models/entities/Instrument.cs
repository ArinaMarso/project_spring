using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using StoreG5G11.models.ef.processes;
using StoreG5G11.services;
using StoreG5G11.src.models.ef.entities;
using StoreG5G11.utils;

namespace StoreG5G11.models.ef.entities;

public partial class Instrument : AModel
{

    private string _name = string.Empty;
    private string _description = string.Empty;
    private decimal _price;
    private InstrumentType _instrumentType;
    private string _brand = string.Empty;
    private int _stockQuantity;

    public InstrumentType InstrumentType
    {
        get => _instrumentType;
        set => SetField(ref _instrumentType, value);
    }

    [Required(ErrorMessage = "Бренд обязателен")]
    [MaxLength(50, ErrorMessage = "Максимум 50 символов")]
    public string Brand
    {
        get => _brand;
        set => SetField(ref _brand, value, [
            (name, value) => ValidationService.ValidStringLength(name, value, 2, 50)
        ]);
    }

    [Required(ErrorMessage = "Название обязательно")]
    [MaxLength(50, ErrorMessage = "Максимум 50 символов")]
    public string Name
    {
        get => _name;
        set => SetField(ref _name, value, [
            (name, value) => ValidationService.ValidStringLength(name, value, 2, 50)
        ]);
    }

    [Required(ErrorMessage = "Описание обязательно")]
    [MaxLength(200, ErrorMessage = "Максимум 200 символов")]
    public string Description
    {
        get => _description;
        set => SetField(ref _description, value, [
            (name, value) => ValidationService.ValidStringLength(name, value, 10, 200)
        ]);
    }

    [Required(ErrorMessage = "Цена обязательна")]
    [Range(0.01, 1000000, ErrorMessage = "Цена должна быть от 0.01 до 1 000 000")]
    public decimal Price
    {
        get => _price;
        set => SetField(ref _price, value, [
            (name, val) => ValidationService.ValidPositiveDecimal(name, val)
        ]);
    }

    [Required(ErrorMessage = "Количество на складе обязательно")]
    [Range(0, 1000, ErrorMessage = "Количество должно быть от 0 до 1000")]
    public int StockQuantity
    {
        get => _stockQuantity;
        set => SetField(ref _stockQuantity, value, [
            (name, val) => ValidationService.ValidPositiveInt(name, val, allowZero: true)
        ]);
    }

    public List<Order> Orders { get; init; } = [];

    public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(_name))
            yield return new ValidationResult("Название обязательно", [nameof(Name)]);

        if (_name.Length is < 2 or > 50)
            yield return new ValidationResult("Длина названия должна быть от 2 до 50 символов", [nameof(Name)]);

        if (string.IsNullOrWhiteSpace(_description))
            yield return new ValidationResult("Описание обязательно", [nameof(Description)]);

        if (_description.Length is < 10 or > 200)
            yield return new ValidationResult("Длина описания должна быть от 10 до 200 символов", [nameof(Description)]);

        if (string.IsNullOrWhiteSpace(_brand))
            yield return new ValidationResult("Бренд обязателен", [nameof(Brand)]);

        if (_brand.Length is < 2 or > 50)
            yield return new ValidationResult("Длина бренда должна быть от 2 до 50 символов", [nameof(Brand)]);

        if (_price < 0.01m)
            yield return new ValidationResult("Цена должна быть не менее 0.01", [nameof(Price)]);

        if (_stockQuantity < 0)
            yield return new ValidationResult("Количество не может быть отрицательным", [nameof(StockQuantity)]);
    }

    public override AModel GetCopy()
    {
        return new Instrument
        {
            Id = this.Id,
            Name = _name,
            Description = _description,
            Price = _price,
            InstrumentType = _instrumentType,
            Brand = _brand,
            StockQuantity = _stockQuantity
        };
    }

    public override void CopyFrom(AModel other)
    {
        if (other is not Instrument inst)
            throw new ArgumentException("CopyFrom может копировать только из типа Instrument");

        var originalId = this.Id;

        Name = inst.Name;
        Description = inst.Description;
        Price = inst.Price;
        InstrumentType = inst.InstrumentType;
        Brand = inst.Brand;
        StockQuantity = inst.StockQuantity;

        this.Id = originalId;
    }

    public override bool Equals(AModel? other)
    {
        if (other is not Instrument inst) return false;

        if (this.Id != 0 && inst.Id != 0)
            return this.Id == inst.Id;

        return Name == inst.Name &&
               Brand == inst.Brand &&
               InstrumentType == inst.InstrumentType;
    }

    public override int GetHashCode()
    {
        if (Id != 0)
            return Id.GetHashCode();

        return HashCode.Combine(Name, Brand, InstrumentType);
    }

    public override bool Equals(object? obj)
    {
        if (obj is AModel amodel)
            return Equals(amodel);
        return false;
    }
}