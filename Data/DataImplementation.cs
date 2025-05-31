//____________________________________________________________________________________________________________________________________
//
//  Copyright (C) 2024, Mariusz Postol LODZ POLAND.
//
//  To be in touch join the community by pressing the `Watch` button and get started commenting using the discussion panel at
//
//  https://github.com/mpostol/TP/discussions/182
//
//_____________________________________________________________________________________________________________________________________

using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;

namespace TP.ConcurrentProgramming.Data
{
    internal class DataImplementation : DataAbstractAPI
    {
        private readonly Logger _logger;
        public DataImplementation() 
        {
            _logger = new Logger("..\\..\\..\\..\\Logs\\diagnostic.log");
        }

        private readonly List<Task> _ballTasks = new List<Task>();
        private readonly List<Ball> _ballsList = new List<Ball>();
        private readonly object _balllock = new();

        public override void Start(int numberOfBalls, Action<IVector, IBall> upperLayerHandler)
        {
            if (Disposed)
                throw new ObjectDisposedException(nameof(DataImplementation));
            if (upperLayerHandler == null)
                throw new ArgumentNullException(nameof(upperLayerHandler));

            Random random = new Random();
            const double minDistance = 25;
            List<IVector> existingPositions = new List<IVector>();

            for (int i = 0; i < numberOfBalls; i++)
            {
                Vector startingPosition = null;
                bool validPosition = false;
                int maxAttempts = 1000;
                for (int attempt = 0; attempt < maxAttempts && !validPosition; attempt++)
                {
                    startingPosition = new Vector(random.Next(100, 300), random.Next(100, 380));
                    validPosition = true;
                    foreach (var pos in existingPositions)
                    {
                        double distance = Math.Sqrt(Math.Pow(startingPosition.x - pos.x, 2) + Math.Pow(startingPosition.y - pos.y, 2));
                        if (distance < minDistance)
                        {
                            validPosition = false;
                            break;
                        }
                    }
                }
                existingPositions.Add(startingPosition);

                Vector startingVelocity = new(random.Next(-100 - -20, 100 - 20), random.Next(-100 - -20, 100 - 20));
                double weight = 1.0;
                Ball newBall = new(startingPosition, startingVelocity, weight, _logger);
                upperLayerHandler(startingPosition, newBall);
                Task movementTask = newBall.StartMovementTask();
                lock (_balllock)
                {
                    _ballTasks.Add(movementTask);
                    _ballsList.Add(newBall);
                }
                Console.WriteLine($"Created Ball {i} at {startingPosition} with velocity {startingVelocity}");
            }
        }

        public override IVector CreateVector(double x, double y)
        {
            return new Vector(x, y);
        }

        private bool Disposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!Disposed)
            {
                if (disposing)
                {
                    lock (_balllock)
                    {
                        foreach (var ball in _ballsList)
                        {
                            ball.StopMovement();
                        }
                    }

                    try
                    {
                        Console.WriteLine("Waiting for ball tasks to finish...");
                        Task.WhenAll(_ballTasks).Wait();
                        Console.WriteLine("All ball tasks finished.");
                    }
                    catch (AggregateException ae)
                    {
                        foreach (var inner in ae.InnerExceptions)
                        {
                            if (inner is not TaskCanceledException)
                            {
                                Console.WriteLine($"Caught unexpected exception during Dispose: {inner.GetType().Name}: {inner.Message}");
                            }
                        }
                    }
                    catch (TaskCanceledException)
                    {
                        Console.WriteLine("Dispose caught TaskCanceledException.");
                    }

                    _ballTasks.Clear();
                    _ballsList.Clear();
                    _logger.Dispose();
                }

                Disposed = true;
            }
        }
        public override void LogDiagnosticData(DiagnosticData data)
        {
            _logger.Log(data);
        }

        public override void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        [Conditional("DEBUG")]
        internal void CheckBallsList(Action<IEnumerable<IBall>> returnBallsList)
        {
            returnBallsList(_ballsList);
        }

        [Conditional("DEBUG")]
        internal void CheckNumberOfBalls(Action<int> returnNumberOfBalls)
        {
            returnNumberOfBalls(_ballsList.Count);
        }

        [Conditional("DEBUG")]
        internal void CheckObjectDisposed(Action<bool> returnInstanceDisposed)
        {
            returnInstanceDisposed(Disposed);
        }
    }
}
