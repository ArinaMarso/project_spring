using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using StoreG5G11.api.ef;
using StoreG5G11.models.ef.entities;
using StoreG5G11.models.ef.processes;
using StoreG5G11.utils;

namespace StoreG5G11.src.modelViews;  
public class SaleViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private Sale _currentSale = new();
    private Instrument? _selectedInstrument;
    private int _quantityToAdd = 1;
    private ObservableCollection<Instrument> _availableInstruments = new();
    private ObservableCollection<Employee> _employees = new();

    public Sale CurrentSale
    {
        get => _currentSale;
        set => SetField(ref _currentSale, value);
    }

    public Instrument? SelectedInstrument
    {
        get => _selectedInstrument;
        set => SetField(ref _selectedInstrument, value);
    }

    public int QuantityToAdd
    {
        get => _quantityToAdd;
        set => SetField(ref _quantityToAdd, value);
    }

    public ObservableCollection<Instrument> AvailableInstruments
    {
        get => _availableInstruments;
        set => SetField(ref _availableInstruments, value);
    }

    public ObservableCollection<Employee> Employees
    {
        get => _employees;
        set => SetField(ref _employees, value);
    }

    public Array PaymentMethods => Enum.GetValues(typeof(PaymentMethod));

    public ICommand AddToSaleCommand { get; }
    public ICommand RemoveFromSaleCommand { get; }
    public ICommand CompleteSaleCommand { get; }

    public SaleViewModel()
    {
        AddToSaleCommand = new RelayCommand(AddToSale);
        RemoveFromSaleCommand = new RelayCommand(RemoveFromSale);
        CompleteSaleCommand = new RelayCommand(CompleteSale);

        LoadData();
    }

    private void LoadData()
    {
        try
        {
            var context = ApplicationContext.Instance;

            var instruments = context.Instruments.Where(i => i.StockQuantity > 0).ToList();
            AvailableInstruments.Clear();
            foreach (var item in instruments) AvailableInstruments.Add(item);

            var employees = context.Employees.ToList();
            Employees.Clear();
            foreach (var item in employees) Employees.Add(item);

            if (Employees.Count > 0)
                CurrentSale.EmployeeId = Employees[0].Id;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка загрузки: {ex.Message}", "Ошибка");
        }
    }

    private void AddToSale(object? parameter)
    {
        if (SelectedInstrument == null)
        {
            MessageBox.Show("Выберите инструмент");
            return;
        }

        if (QuantityToAdd <= 0)
        {
            MessageBox.Show("Количество должно быть больше 0");
            return;
        }

        if (QuantityToAdd > SelectedInstrument.StockQuantity)
        {
            MessageBox.Show($"Недостаточно товара. Доступно: {SelectedInstrument.StockQuantity}");
            return;
        }

        var existingOrder = CurrentSale.Orders.FirstOrDefault(o => o.InstrumentId == SelectedInstrument.Id);
        if (existingOrder != null)
        {
            existingOrder.Count += QuantityToAdd;
        }
        else
        {
            var order = new Order
            {
                InstrumentId = SelectedInstrument.Id,
                Instrument = SelectedInstrument,
                Price = SelectedInstrument.Price,
                Count = QuantityToAdd
            };
            CurrentSale.Orders.Add(order);
        }

        CurrentSale.Total = CurrentSale.Orders.Sum(o => o.Price * o.Count);

        OnPropertyChanged(nameof(CurrentSale));
        QuantityToAdd = 1;
        OnPropertyChanged(nameof(QuantityToAdd));
    }

    private void RemoveFromSale(object? parameter)
    {
        if (parameter is Order order)
        {
            CurrentSale.Orders.Remove(order);
            CurrentSale.Total = CurrentSale.Orders.Sum(o => o.Price * o.Count);
            OnPropertyChanged(nameof(CurrentSale));
        }
    }

    private void CompleteSale(object? parameter)
    {
        if (CurrentSale.Orders.Count == 0)
        {
            MessageBox.Show("Добавьте товары в продажу");
            return;
        }

        if (CurrentSale.EmployeeId == 0)
        {
            MessageBox.Show("Выберите сотрудника");
            return;
        }

        try
        {
            var context = ApplicationContext.Instance;
            CurrentSale.Date = DateTime.Now;
            var saleToSave = new Sale
            {
                EmployeeId = CurrentSale.EmployeeId,
                Date = CurrentSale.Date,
                Total = CurrentSale.Total,
                PaymentMethod = CurrentSale.PaymentMethod
            };

            context.Sales.Add(saleToSave);
            context.SaveChanges();

            foreach (var order in CurrentSale.Orders)
            {
                var orderToSave = new Order
                {
                    SaleId = saleToSave.Id,
                    InstrumentId = order.InstrumentId,
                    Price = order.Price,
                    Count = order.Count
                };

                context.Orders.Add(orderToSave);

                var instrument = context.Instruments.Find(order.InstrumentId);
                if (instrument != null)
                {
                    instrument.StockQuantity -= order.Count;
                }
            }

            context.SaveChanges();

            var receipt = $"ЧЕК №{saleToSave.Id}\n" +
                         $"Дата: {CurrentSale.Date:dd.MM.yyyy HH:mm}\n" +
                         $"Сумма: {CurrentSale.Total:C}\n" +
                         $"Спасибо за покупку!";

            MessageBox.Show(receipt, "Продажа завершена");

            CurrentSale = new Sale();
            if (Employees.Count > 0)
            {
                CurrentSale.EmployeeId = Employees[0].Id;
            }

            LoadData(); 
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка");
        }
    }
}