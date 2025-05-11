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
        public DataImplementation() { }

        private readonly List<Task> _ballTasks = new List<Task>();
        private readonly List<Ball> _ballsList = new List<Ball>();

        public override void Start(int numberOfBalls, Action<IVector, IBall> upperLayerHandler)
        {
            if (Disposed)
                throw new ObjectDisposedException(nameof(DataImplementation));
            if (upperLayerHandler == null)
                throw new ArgumentNullException(nameof(upperLayerHandler));

            Random random = new Random();

            for (int i = 0; i < numberOfBalls; i++)
            {
                Vector startingPosition = new(random.Next(100, 400 - 100), random.Next(100, 400 - 100));
                Vector startingVelocity = new(random.Next(-80 - -20, 80 - 20), random.Next(-80 - -20, 80 - 20));
                Ball newBall = new(startingPosition, startingVelocity);
                upperLayerHandler(startingPosition, newBall);
                Task movementTask = newBall.StartMovementTask();
                _ballTasks.Add(movementTask);
                _ballsList.Add(newBall);
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
                    foreach (var ball in _ballsList)
                    {
                        ball.StopMovement();
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
                }

                Disposed = true;
            }
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
