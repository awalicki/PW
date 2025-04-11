using System;
using System.Windows;
using TP.ConcurrentProgramming.Presentation.ViewModel;

namespace TP.ConcurrentProgramming.PresentationView
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(BallsCountTextBox.Text, out int ballCount) && ballCount >= 1 && ballCount <= 15)
            {
                ErrorTextBlock.Text = ""; // Czyścimy komunikat błędu
                MainWindowViewModel viewModel = (MainWindowViewModel)DataContext;
                viewModel.Start(ballCount);
            }
            else
            {
                ErrorTextBlock.Text = "Podaj liczbę z przedziału 1-15.";
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            if (DataContext is MainWindowViewModel viewModel)
                viewModel.Dispose();
            base.OnClosed(e);
        }
    }
}
