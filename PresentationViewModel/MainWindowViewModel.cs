//__________________________________________________________________________________________
//
//  Copyright 2024 Mariusz Postol LODZ POLAND.
//
//  To be in touch join the community by pressing the `Watch` button and to get started
//  comment using the discussion panel at
//  https://github.com/mpostol/TP/discussions/182
//__________________________________________________________________________________________

using System;
using System.Collections.ObjectModel;
using TP.ConcurrentProgramming.Presentation.Model;
using TP.ConcurrentProgramming.Presentation.ViewModel.MVVMLight;
using ModelIBall = TP.ConcurrentProgramming.Presentation.Model.IBall;

namespace TP.ConcurrentProgramming.Presentation.ViewModel
{
    public class MainWindowViewModel : ViewModelBase, IDisposable
    {
        #region Properties and Commands

        private bool _hasStarted = false;
        public bool HasStarted
        {
            get => _hasStarted;
            set
            {
                if (Set(ref _hasStarted, value))
                    StartCommand.RaiseCanExecuteChanged();
            }
        }


        private string _ballsCountText;
        public string BallsCountText
        {
            get => _ballsCountText;
            set
            {
                if (Set(ref _ballsCountText, value))
                {
                    StartCommand.RaiseCanExecuteChanged();
                }
            }
        }

        private string _errorMessage;
        public string ErrorMessage
        {
            get => _errorMessage;
            set => Set(ref _errorMessage, value);
        }

        public ObservableCollection<ModelIBall> Balls { get; } = new ObservableCollection<ModelIBall>();
        public RelayCommand StartCommand { get; }

        #endregion


        #region ctor

        public MainWindowViewModel() : this(null) { }

        internal MainWindowViewModel(ModelAbstractApi modelLayerAPI)
        {
            ModelLayer = modelLayerAPI ?? ModelAbstractApi.CreateModel();
            Observer = ModelLayer.Subscribe<ModelIBall>(x => Balls.Add(x));

            StartCommand = new RelayCommand(
                execute: StartAction,
                canExecute: () => !HasStarted &&
                !string.IsNullOrEmpty(BallsCountText) &&
                int.TryParse(BallsCountText, out int count) &&
                count >= 1 && count <= 15
            );
        }


        #endregion ctor

        #region public API

        private void StartAction()
        {
            if (int.TryParse(BallsCountText, out int ballCount) && ballCount >= 1 && ballCount <= 15)
            {
                ErrorMessage = string.Empty;
                Start(ballCount);
                HasStarted = true;
            }
            else
            {
                ErrorMessage = "Podaj liczbę z przedziału 1-15.";
            }
        }

        internal void Start(int numberOfBalls)
        {
            if (Disposed)
                throw new ObjectDisposedException(nameof(MainWindowViewModel));

            // Clear previous balls
            Balls.Clear();

            ModelLayer.Start(numberOfBalls);
        }

        #endregion public API

        #region IDisposable

        protected virtual void Dispose(bool disposing)
        {
            if (!Disposed)
            {
                if (disposing)
                {
                    Balls.Clear();
                    Observer.Dispose();
                    ModelLayer.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                Disposed = true;
            }
        }

        public void Dispose()
        {
            if (Disposed)
                throw new ObjectDisposedException(nameof(MainWindowViewModel));
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion IDisposable

        #region private

        private IDisposable Observer = null;
        private ModelAbstractApi ModelLayer;
        private bool Disposed = false;

        #endregion private
    }
}