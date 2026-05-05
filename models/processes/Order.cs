using System.ComponentModel.DataAnnotations;
using StoreG5G11.models.ef.entities;

namespace StoreG5G11.models.ef.processes;

public class Order : AModel
{
    private int _saleId;
    public int SaleId
    {
        get => _saleId;
        set => SetField(ref _saleId, value);
    }

    private Sale? _sale;
    public Sale? Sale
    {
        get => _sale;
        set => SetField(ref _sale, value);
    }

    private int _instrumentId;
    public int InstrumentId
    {
        get => _instrumentId;
        set => SetField(ref _instrumentId, value);
    }

    private Instrument? _instrument;
    public Instrument? Instrument
    {
        get => _instrument;
        set => SetField(ref _instrument, value);
    }

    private decimal _price;
    public decimal Price
    {
        get => _price;
        set => SetField(ref _price, value);
    }

    private int _count;
    public int Count
    {
        get => _count;
        set => SetField(ref _count, value);
    }

    public decimal Total => Price * Count;

    public Order() { }

    public Order(decimal price, int count = 1)
    {
        Price = price;
        Count = count;
    }

    public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (_count <= 0)
            yield return new ValidationResult("Количество должно быть больше 0", [nameof(Count)]);

        if (_price <= 0)
            yield return new ValidationResult("Цена должна быть положительной", [nameof(Price)]);
    }

    public override AModel GetCopy() => new Order
    {
        SaleId = _saleId,
        InstrumentId = _instrumentId,
        Price = _price,
        Count = _count
    };

    public override void CopyFrom(AModel other)
    {
        if (other is not Order inst)
            throw new ArgumentException("CopyFrom может копировать только из типа Order");

        SaleId = inst.SaleId;
        InstrumentId = inst.InstrumentId;
        Price = inst.Price;
        Count = inst.Count;
    }

    public override bool Equals(AModel other)
    {
        if (other is not Order inst)
            throw new ArgumentException("Equals может сравнивать только с типом Order");
        return InstrumentId == inst.InstrumentId && SaleId == inst.SaleId;
    }
}