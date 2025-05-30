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
        private static int _nextBallId = 0;
        public int Id { get; }
        internal Ball(Vector initialPosition, Vector initialVelocity, double weight, Logger logger)
        {
            Id = Interlocked.Increment(ref _nextBallId);
            Position = initialPosition;
            Velocity = initialVelocity;
            BallRadius = 10;
            Weight = weight;
            _cancellationTokenSource = new CancellationTokenSource();
            _logger = logger;
        }

        public event EventHandler<IVector>? NewPositionNotification;
        private readonly object _velocityLock = new object();

        public IVector Velocity { get; set;}

        public double Weight { get; }
        public double BallRadius { get; }

        public void StopMovement()
        {
            _cancellationTokenSource.Cancel();
        }

        private Vector Position;

        private readonly CancellationTokenSource _cancellationTokenSource;
        private Task? _movementTask;
        private readonly Logger _logger;

        private void RaiseNewPositionChangeNotification()
        {
            NewPositionNotification?.Invoke(this, Position);
            _logger.Log(new DiagnosticData
            {
                Timestamp = DateTime.Now,
                PositionX = Position.x,
                PositionY = Position.y,
                VelocityX = Velocity.x,
                VelocityY = Velocity.y,
                EventType = DiagnosticEventType.PositionUpdate,
                Message = $"Ball position update: ({Position.x:F2}, {Position.y:F2}), Velocity: ({Velocity.x:F2}, {Velocity.y:F2})"
            });
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

                    IVector currentVelocity;
                    lock (_velocityLock)
                    {
                        currentVelocity = Velocity;
                    }

                    Vector velocityVector = (Vector)currentVelocity;
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