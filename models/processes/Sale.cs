using System.ComponentModel.DataAnnotations;
using StoreG5G11.models.ef.entities;
using StoreG5G11.modelViews;
using StoreG5G11.src.modelViews;

namespace StoreG5G11.models.ef.processes;

public class Sale : AModel
{
    private int _employeeId;
    public int EmployeeId
    {
        get => _employeeId;
        set => SetField(ref _employeeId, value);
    }

    private Employee? _employee;
    public Employee? Employee
    {
        get => _employee;
        set => SetField(ref _employee, value);
    }

    private decimal _total;
    public decimal Total
    {
        get => _total;
        set => SetField(ref _total, value);
    }

    private DateTime _date;
    public DateTime Date
    {
        get => _date;
        set => SetField(ref _date, value);
    }

    private PaymentMethod _paymentMethod;
    public PaymentMethod PaymentMethod
    {
        get => _paymentMethod;
        set => SetField(ref _paymentMethod, value);
    }

    public bool IsCompleted => Total > 0 && Date != default && Date <= DateTime.Now;

    public List<Order> Orders { get; init; } = [];

    public Sale()
    {
        Date = DateTime.Now;
        Total = 0;
        PaymentMethod = PaymentMethod.Cash;
    }

    public void AddOrder(Order order)
    {
        Orders.Add(order);
        CalculateTotal();
    }

    public void RemoveOrder(Order order)
    {
        Orders.Remove(order);
        CalculateTotal();
    }

    private void CalculateTotal()
    {
        Total = Orders.Sum(o => o.Price * o.Count);
    }

    public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (EmployeeId <= 0)
            yield return new ValidationResult("Сотрудник обязателен", [nameof(EmployeeId)]);

        if (Total < 0)
            yield return new ValidationResult("Сумма не может быть отрицательной", [nameof(Total)]);
    }

    public override AModel GetCopy() => new Sale
    {
        EmployeeId = _employeeId,
        Employee = _employee,
        Total = _total,
        Date = _date,
        PaymentMethod = _paymentMethod
    };

    public override void CopyFrom(AModel other)
    {
        if (other is not Sale inst)
            throw new ArgumentException("CopyFrom может копировать только из типа Sale");

        EmployeeId = inst.EmployeeId;
        Employee = inst.Employee;
        Total = inst.Total;
        Date = inst.Date;
        PaymentMethod = inst.PaymentMethod;
    }

    public override bool Equals(AModel other)
    {
        if (other is not Sale inst)
            throw new ArgumentException("Equals может сравнивать только с типом Sale");
        return EmployeeId == inst.EmployeeId && Date == inst.Date && Total == inst.Total;
    }
}