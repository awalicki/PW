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
using System.Collections.Generic;
using System.Diagnostics;
using UnderneathLayerAPI = TP.ConcurrentProgramming.Data.DataAbstractAPI;

namespace TP.ConcurrentProgramming.BusinessLogic
{
    internal class BusinessLogicImplementation : BusinessLogicAbstractAPI
    {
        #region ctor

        public BusinessLogicImplementation() : this(null)
        { }

        internal BusinessLogicImplementation(UnderneathLayerAPI? underneathLayer)
        {
            layerBellow = underneathLayer == null ? UnderneathLayerAPI.GetDataLayer() : underneathLayer;
        }

        #endregion ctor

        #region BusinessLogicAbstractAPI

        public override void Dispose()
        {
            if (Disposed)
                throw new ObjectDisposedException(nameof(BusinessLogicImplementation));

            lock (_lock)
    {
        foreach (var ball in _balls)
            ball.DetachFromDataBall();
        _balls.Clear();
    }
            layerBellow.Dispose();
            Disposed = true;
        }

        public override void Start(int numberOfBalls, Action<IPosition, IBall> upperLayerHandler)
        {
            if (Disposed)
                throw new ObjectDisposedException(nameof(BusinessLogicImplementation));
            if (upperLayerHandler == null)
                throw new ArgumentNullException(nameof(upperLayerHandler));

            layerBellow.Start(numberOfBalls, (startingPositionFromData, dataBall) =>
            {
                var initialPositionBL = new Position(startingPositionFromData.x, startingPositionFromData.y);
                var ball = new Ball(dataBall, layerBellow, initialPositionBL);

                lock (_lock)
                {
                    _balls.Add(ball);
                }

                ball.NewPositionNotification += (sender, pos) => OnBallMoved(ball);
                upperLayerHandler(initialPositionBL, ball);
            });
        }

        #endregion BusinessLogicAbstractAPI

        #region Collision Detection

        private void OnBallMoved(Ball movedBall)
        {
            lock (_lock)
            {
                var pos1 = movedBall.GetPosition();
                var v1 = movedBall.GetVelocity();
                var m1 = movedBall.GetWeight();

                // Iterate through other balls to check for collisions
                foreach (var other in _balls)
                {
                    if (other == movedBall)
                        continue; // Don't check collision with itself

                    var pos2 = other.GetPosition();
                    var v2 = other.GetVelocity();
                    var m2 = other.GetWeight();

                    // Check if balls are colliding based on distance
                    if (AreBallsColliding(pos1, pos2))
                    {
                        Console.WriteLine($"BL: Collision detected between balls at ({pos1.x:F2},{pos1.y:F2}) and ({pos2.x:F2},{pos2.y:F2})");

                        double totalMass = m1 + m2;

                        if (totalMass > 0)
                        {
                            double newV1x = ((m1 - m2) * v1.x + 2 * m2 * v2.x) / totalMass;
                            double newV1y = ((m1 - m2) * v1.y + 2 * m2 * v2.y) / totalMass;
                            double newV2x = ((m2 - m1) * v2.x + 2 * m1 * v1.x) / totalMass;
                            double newV2y = ((m2 - m1) * v2.y + 2 * m1 * v1.y) / totalMass;

                            movedBall.SetVelocity(layerBellow.CreateVector(newV1x, newV1y));
                            other.SetVelocity(layerBellow.CreateVector(newV2x, newV2y));
                            Console.WriteLine($"BL: Masses: {m1:F2}, {m2:F2}. Old V1: ({v1.x:F2},{v1.y:F2}), Old V2: ({v2.x:F2},{v2.y:F2}). New V1: ({newV1x:F2},{newV1y:F2}), New V2: ({newV2x:F2},{newV2y:F2})");
                        }
                        else
                        {
                            Console.WriteLine("BL: Total mass is zero, cannot calculate collision response.");
                        }
                    }
                }
            }
        }

        private bool AreBallsColliding(IPosition pos1, IPosition pos2)
        {
            double dx = pos1.x - pos2.x;
            double dy = pos1.y - pos2.y;
            double distanceSquared = dx * dx + dy * dy;
            return distanceSquared < BallRadius * 2 * BallRadius * 2;
        }

        #endregion Collision Detection

        #region private

        private bool Disposed = false;
        private readonly UnderneathLayerAPI layerBellow;
        private readonly List<Ball> _balls = new();
        private const double BallRadius = 10;
        private readonly object _lock = new();

        #endregion private

        #region TestingInfrastructure

        [Conditional("DEBUG")]
        internal void CheckObjectDisposed(Action<bool> returnInstanceDisposed)
        {
            returnInstanceDisposed(Disposed);
        }

        #endregion TestingInfrastructure
    }
}
