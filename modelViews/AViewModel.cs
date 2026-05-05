using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using StoreG5G11.api.ef;
using StoreG5G11.models.ef;
using StoreG5G11.models.ef.entities;
using StoreG5G11.models.ef.processes;
using StoreG5G11.src.models.ef.entities;
using StoreG5G11.utils;

namespace StoreG5G11.src.modelViews; 

public abstract class AViewModel<T> : INotifyPropertyChanged
    where T : AModel, new()
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected DbSet<T>? _entities;
    public ObservableCollection<T> Items { get; } = new();

    private T? _selectedItem;
    private T? _originalItem;
    private bool _isAdding = false;
    private bool _isInitialized = false;

    protected AViewModel()
    {
        InitializeCommands();
    }

    protected abstract DbSet<T> GetEntities(ApplicationContext context);

    private void InitializeCommands()
    {
        AddCommand = new RelayCommand(AddNewItem);
        RemoveCommand = new RelayCommand(RemoveItem, CanRemoveItem);
        SaveCommand = new RelayCommand(SaveItem, CanSaveItem);
        RefreshCommand = new RelayCommand(LoadData);
    }

    private void EnsureInitialized()
    {
        if (!_isInitialized)
        {
            try
            {
                var context = ApplicationContext.Instance;
                if (context == null)
                    throw new InvalidOperationException("ApplicationContext не инициализирован");

                context.Database.EnsureCreated();
                _entities = GetEntities(context);
                _isInitialized = true;
                LoadData();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка инициализации: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
        }
    }

    private void LoadData(object? parameter = null)
    {
        try
        {
            EnsureInitialized();

            if (_entities == null)
            {
                MessageBox.Show("Не удалось получить доступ к данным", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Items.Clear();
            foreach (var item in _entities.ToList())
            {
                Items.Add(item);
            }

            CommandManager.InvalidateRequerySuggested();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    public void Load() => LoadData(null);

    public T? SelectedItem
    {
        get => _selectedItem;
        set
        {
            if (_selectedItem != null && _originalItem != null && HasChanges())
            {
                var result = MessageBox.Show("Сохранить изменения?", "Сохранение",
                    MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes) SaveItem(null);
                else if (result == MessageBoxResult.Cancel) return;
            }

            _originalItem = value != null ? (T)value.GetCopy() : null;
            _selectedItem = value;
            OnPropertyChanged();
            CommandManager.InvalidateRequerySuggested();
        }
    }

    private bool HasChanges() => _selectedItem != null && _originalItem != null &&
                                 !_selectedItem.Equals(_originalItem);

    public RelayCommand AddCommand { get; private set; } = null!;
    public RelayCommand RemoveCommand { get; private set; } = null!;
    public RelayCommand SaveCommand { get; private set; } = null!;
    public RelayCommand RefreshCommand { get; private set; } = null!;

    private bool CanRemoveItem(object? parameter) => SelectedItem != null && !_isAdding;
    private bool CanSaveItem(object? parameter) => SelectedItem != null;

    private void AddNewItem(object? parameter = null)
    {
        try
        {
            EnsureInitialized();
            _isAdding = true;
            var newItem = new T();
            SetDefaultValues(newItem);
            Items.Add(newItem);
            SelectedItem = newItem;
            CommandManager.InvalidateRequerySuggested();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при добавлении: {ex.Message}", "Ошибка",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void SetDefaultValues(T item)
    {
        if (item is Instrument instrument)
        {
            instrument.Name = "Новый инструмент";
            instrument.Brand = "Бренд";
            instrument.Description = "Описание нового инструмента";
            instrument.Price = 10000.00m;
            instrument.InstrumentType = InstrumentType.String;
            instrument.StockQuantity = 1;
        }
        else if (item is Employee employee)
        {
            employee.Name = "Фамилия";
            employee.FirstName = "Имя";
            employee.SecondName = "Отчество";
            employee.Position = "Консультант";

            if (DateTime.Now.AddYears(-25) is DateTime birthDate)
                employee.BirthDate = DateOnly.FromDateTime(birthDate);
            else
                employee.BirthDate = DateOnly.FromDateTime(new DateTime(1995, 1, 1));
            employee.Salary = 30000.00m;
        }
        else if (item is Sale sale)
        {
            sale.EmployeeId = 1;
            sale.Total = 0;
            sale.Date = DateTime.Now;
            sale.PaymentMethod = PaymentMethod.Cash; 
        }
    }

    private void RemoveItem(object? parameter = null)
    {
        if (SelectedItem == null)
        {
            MessageBox.Show("Выберите элемент для удаления", "Информация",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var result = MessageBox.Show("Удалить запись?", "Удаление",
            MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result != MessageBoxResult.Yes) return;

        try
        {
            EnsureInitialized();

            if (_entities == null)
            {
                MessageBox.Show("Не удалось получить доступ к данным", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_isAdding && SelectedItem != null && SelectedItem.Id == 0)
            {
                Items.Remove(SelectedItem);
                SelectedItem = null;
                _isAdding = false;
                return;
            }

            _entities.Remove(SelectedItem);
            ApplicationContext.Instance.SaveChanges();

            var itemToRemove = SelectedItem;
            SelectedItem = null;
            Items.Remove(itemToRemove);
            _isAdding = false;

            MessageBox.Show("Запись успешно удалена", "Удаление",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (DbUpdateException ex)
        {
            MessageBox.Show($"Ошибка удаления: {ex.InnerException?.Message}",
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка удаления: {ex.Message}", "Ошибка",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }

        CommandManager.InvalidateRequerySuggested();
    }

    private void SaveItem(object? parameter = null)
    {
        if (SelectedItem == null)
        {
            MessageBox.Show("Выберите элемент для сохранения", "Информация",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            EnsureInitialized();

            if (_entities == null)
            {
                MessageBox.Show("Не удалось получить доступ к данным", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!ValidateItem(SelectedItem)) return;

            if (_isAdding)
            {
                var newItem = new T();
                newItem.CopyFrom(SelectedItem);
                _entities.Add(newItem);
                ApplicationContext.Instance.SaveChanges();

                var index = Items.IndexOf(SelectedItem);
                if (index >= 0)
                {
                    Items[index] = newItem;
                    SelectedItem = newItem;
                }

                _isAdding = false;
                MessageBox.Show("Запись успешно добавлена", "Сохранение",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                if (_originalItem == null) return;
                _originalItem.CopyFrom(SelectedItem);
                _entities.Update(_originalItem);
                ApplicationContext.Instance.SaveChanges();

                var index = Items.IndexOf(_originalItem);
                if (index >= 0)
                {
                    Items[index] = _originalItem;
                    SelectedItem = _originalItem;
                }

                MessageBox.Show("Изменения успешно сохранены", "Сохранение",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }

            CommandManager.InvalidateRequerySuggested();
        }
        catch (DbUpdateException ex)
        {
            MessageBox.Show($"Ошибка сохранения: {ex.InnerException?.Message}",
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private bool ValidateItem(T item)
    {
        if (item is Instrument instrument)
        {
            if (string.IsNullOrWhiteSpace(instrument.Name))
            {
                MessageBox.Show("Название инструмента не может быть пустым", "Ошибка валидации",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            if (instrument.Price <= 0)
            {
                MessageBox.Show("Цена должна быть положительной", "Ошибка валидации",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
        }
        else if (item is Employee employee)
        {
            if (string.IsNullOrWhiteSpace(employee.Name) ||
                string.IsNullOrWhiteSpace(employee.FirstName))
            {
                MessageBox.Show("Фамилия и имя не могут быть пустыми", "Ошибка валидации",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            if (employee.Salary <= 0)
            {
                MessageBox.Show("Зарплата должна быть положительной", "Ошибка валидации",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
        }
        else if (item is Sale sale)
        {
            if (sale.EmployeeId <= 0)
            {
                MessageBox.Show("Сотрудник обязателен", "Ошибка валидации",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
        }

        return true;
    }

    public bool TrySave()
    {
        if (SelectedItem != null)
        {
            SaveItem(null);
            return true;
        }
        return false;
    }

    public bool IsInitialized => _isInitialized;

    public void Reinitialize()
    {
        _isInitialized = false;
        _entities = null;
        LoadData(null);
    }
}