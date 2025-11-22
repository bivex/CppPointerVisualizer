using System;
using System.Windows;
using System.Windows.Controls;
using CppPointerVisualizer.Parser;
using CppPointerVisualizer.ViewModels;

namespace CppPointerVisualizer
{
    public partial class MainWindow : Window
    {
        private readonly MainWindowViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = new MainWindowViewModel();
            DataContext = _viewModel;
        }

        private void VisualizeButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string code = CodeInput.Text;
                var parser = new CppPointerAntlrParser();
                var memoryState = parser.Parse(code);

                _viewModel.Visualize(memoryState);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка парсинга: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExamplesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ExamplesList.SelectedItem is ListBoxItem item)
            {
                CodeInput.Text = item.Tag?.ToString() ?? "";
            }
        }
    }
}
