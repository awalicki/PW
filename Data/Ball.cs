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

        public IVector Velocity { get; set; }

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
                BallId = Id,
                PositionX = Position.x,
                PositionY = Position.y,
                VelocityX = Velocity.x,
                VelocityY = Velocity.y,
                EventType = DiagnosticEventType.PositionUpdate,
                Message = $"Ball position update"
            });
        }

        private void Move(Vector delta)
        {
            Position = new Vector(Position.x + delta.x, Position.y + delta.y);
            RaiseNewPositionChangeNotification();
        }

        private async Task RunMovementLoop(CancellationToken token)
        {
            const int targetFrameTimeMs = 8;
            long lastTick = DateTime.UtcNow.Ticks;

            while (!token.IsCancellationRequested)
            {
                try
                {
                    long currentTick = DateTime.UtcNow.Ticks;
                    double deltaTime = TimeSpan.FromTicks(currentTick - lastTick).TotalSeconds;
                    lastTick = currentTick;

                    IVector currentVelocity;
                    lock (_velocityLock)
                    {
                        currentVelocity = Velocity;
                    }

                    Vector velocityVector = (Vector)currentVelocity;
                    Vector delta = velocityVector * deltaTime;

                    Move(delta);
                    long processingTimeMs = TimeSpan.FromTicks(DateTime.UtcNow.Ticks - currentTick).Milliseconds;
                    int timeToWait = (int)Math.Max(0, targetFrameTimeMs - processingTimeMs);

                    if (timeToWait > 0)
                    {
                        await Task.Delay(timeToWait, token);
                    }
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