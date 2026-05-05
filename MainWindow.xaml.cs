using System.Text;
using System.Windows;
using StoreG5G11.api.ef;
using StoreG5G11.modelViews;
using StoreG5G11.views;

namespace StoreG5G11;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Console.OutputEncoding = Encoding.UTF8;

        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        try
        {
            using var context = new ApplicationContext();
            context.Database.EnsureCreated();

            if (!context.Instruments.Any())
            {
                context.SaveChanges();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка инициализации базы данных: {ex.Message}",
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ManageInstruments_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var instrumentsWindow = new InstrumentsWindow();
            instrumentsWindow.Show();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка открытия окна инструментов: {ex.Message}",
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ManageEmployees_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var employeesWindow = new EmployeesView();
            employeesWindow.Show();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка открытия окна сотрудников: {ex.Message}",
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void SellInstruments_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var salesWindow = new SalesView();
            salesWindow.Show();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка открытия окна продаж: {ex.Message}",
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}