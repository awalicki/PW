//__________________________________________________________________________________________
//
//  Copyright 2024 Mariusz Postol LODZ POLAND.
//
//  To be in touch join the community by pressing the `Watch` button and to get started
//  comment using the discussion panel at
//  https://github.com/mpostol/TP/discussions/182
//__________________________________________________________________________________________

using System;
using System.Windows;
using TP.ConcurrentProgramming.Presentation.ViewModel;

namespace TP.ConcurrentProgramming.PresentationView
{
    /// <summary>
    /// View implementation
    /// </summary>
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
                StartButton.Visibility = Visibility.Collapsed;
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
