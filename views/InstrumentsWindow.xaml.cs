using System.Windows;
using StoreG5G11.modelViews;

namespace StoreG5G11.views;

public partial class InstrumentsWindow : Window
{
    public InstrumentsWindow()
    {
        InitializeComponent();
    }

    private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("Hello World!");
    }
}