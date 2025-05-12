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
using System.Threading;
using System.Threading.Tasks;

namespace TP.ConcurrentProgramming.Data
{
    internal class Ball : IBall
    {
        internal Ball(Vector initialPosition, Vector initialVelocity, double weight)
        {
            Position = initialPosition;
            Velocity = initialVelocity;
            Weight = weight;
            _cancellationTokenSource = new CancellationTokenSource();
        }

        public event EventHandler<IVector>? NewPositionNotification;

        public IVector Velocity { get; set; }

        public double Weight { get; }

        public void StopMovement()
        {
            _cancellationTokenSource.Cancel();
        }

        private Vector Position;

        private readonly CancellationTokenSource _cancellationTokenSource;
        private Task? _movementTask;

        private void RaiseNewPositionChangeNotification()
        {
            NewPositionNotification?.Invoke(this, Position);
        }

        private void Move(Vector delta)
        {
            Position = new Vector(Position.x + delta.x, Position.y + delta.y);
            RaiseNewPositionChangeNotification();
        }

        private async Task RunMovementLoop(CancellationToken token)
        {
            const int movementIntervalMs = 16;

            while (!token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(movementIntervalMs, token);
                    Vector velocityVector = (Vector)Velocity;
                    Vector delta = velocityVector * (movementIntervalMs / 1000.0);

                    Move(delta);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in Ball movement task: {ex.Message}");
                    break;
                }
            }
            Console.WriteLine($"Ball movement task finished.");
        }

        internal Task StartMovementTask()
        {
            _movementTask = Task.Run(() => RunMovementLoop(_cancellationTokenSource.Token));
            return _movementTask;
        }
    }
}
