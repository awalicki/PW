﻿//____________________________________________________________________________________________________________________________________
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
using System.Linq;
using TP.ConcurrentProgramming.Data;
using UnderneathLayerAPI = TP.ConcurrentProgramming.Data.DataAbstractAPI;

namespace TP.ConcurrentProgramming.BusinessLogic
{
    internal class BusinessLogicImplementation : BusinessLogicAbstractAPI
    {
        public BusinessLogicImplementation() : this(null)
        { }

        internal BusinessLogicImplementation(UnderneathLayerAPI? underneathLayer)
        {
            layerBellow = underneathLayer == null ? UnderneathLayerAPI.GetDataLayer() : underneathLayer;
        }

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

                ball.NewPositionNotification += (sender, pos) =>
                {
                    if (sender is Ball movedBall)
                    {
                        OnBallMoved(movedBall);
                    }
                };
                upperLayerHandler(initialPositionBL, ball);
                Console.WriteLine($"BL: Created Ball for upper layer at ({initialPositionBL.x:F2}, {initialPositionBL.y:F2})");
            });
        }

        private void OnBallMoved(Ball movedBall)
        {
            var pos1 = movedBall.GetPosition();
            var v1 = movedBall.GetVelocity();
            var m1 = movedBall.GetWeight();


            List<Ball> ballsCopy;
            lock (_lock)
            {
                ballsCopy = _balls.ToList();
            }

            foreach (var other in ballsCopy)
            {
                if (other == movedBall)
                    continue;

                var pos2 = other.GetPosition();
                var v2 = other.GetVelocity();
                var m2 = other.GetWeight();
                var r1 = other.GetBallRadius();

                if (AreBallsColliding(pos1, pos2, r1))
                {
                    double dx = pos1.x - pos2.x;
                    double dy = pos1.y - pos2.y;
                    double relVx = v1.x - v2.x;
                    double relVy = v1.y - v2.y;

                    double dotProduct = relVx * dx + relVy * dy;

                    if (dotProduct < 0)
                    {
                        layerBellow.LogDiagnosticData(new DiagnosticData
                        {
                            Timestamp = DateTime.Now,
                            PositionX = pos1.x,
                            PositionY = pos1.y,
                            VelocityX = v1.x,
                            VelocityY = v1.y,
                            EventType = DiagnosticEventType.CollisionDetected,
                            Message = $"Collision detected between balls at ({pos1.x:F2},{pos1.y:F2}) and ({pos2.x:F2},{pos2.y:F2}). Old V1: ({v1.x:F2},{v1.y:F2}), Old V2: ({v2.x:F2},{v2.y:F2})."
                        });
                        Console.WriteLine($"BL: Collision detected between balls at ({pos1.x:F2},{pos1.y:F2}) and ({pos2.x:F2},{pos2.y:F2}). Dot product: {dotProduct:F2}");

                        double totalMass = m1 + m2;

                        if (totalMass > 0)
                        {
                            double distance = Math.Sqrt(dx * dx + dy * dy);
                            double nx = dx / distance;
                            double ny = dy / distance;

                            double v1n = v1.x * nx + v1.y * ny;
                            double v2n = v2.x * nx + v2.y * ny;

                            double newV1n = ((m1 - m2) * v1n + 2 * m2 * v2n) / totalMass;
                            double newV2n = ((m2 - m1) * v2n + 2 * m1 * v1n) / totalMass;

                            double deltaV1n = newV1n - v1n;
                            double deltaV2n = newV2n - v2n;

                            double finalV1x = v1.x + deltaV1n * nx;
                            double finalV1y = v1.y + deltaV1n * ny;
                            double finalV2x = v2.x + deltaV2n * nx;
                            double finalV2y = v2.y + deltaV2n * ny;

                            movedBall.SetVelocity(layerBellow.CreateVector(finalV1x, finalV1y));
                            other.SetVelocity(layerBellow.CreateVector(finalV2x, finalV2y));

                            Console.WriteLine($"BL: Masses: {m1:F2}, {m2:F2}. Old V1: ({v1.x:F2},{v1.y:F2}), Old V2: ({v2.x:F2},{v2.y:F2}). New V1: ({finalV1x:F2},{finalV1y:F2}), New V2: ({finalV2x:F2},{finalV2y:F2})");
                        }
                        else
                        {
                            layerBellow.LogDiagnosticData(new DiagnosticData
                            {
                                Timestamp = DateTime.Now,
                                PositionX = pos1.x,
                                PositionY = pos1.y,
                                VelocityX = v1.x,
                                VelocityY = v1.y,
                                EventType = DiagnosticEventType.CollisionNoBounce,
                                Message = $"Balls overlapping but moving apart. Dot product: {dotProduct:F2}. Positions: ({pos1.x:F2},{pos1.y:F2}) and ({pos2.x:F2},{pos2.y:F2})."
                            });
                            Console.WriteLine("BL: Total mass is zero, cannot calculate collision response.");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"BL: Balls overlapping but moving apart. Dot product: {dotProduct:F2}");
                    }
                }
            }
        }

        private bool AreBallsColliding(IPosition pos1, IPosition pos2, double BallRadius)
        {
            double dx = pos1.x - pos2.x;
            double dy = pos1.y - pos2.y;
            double distanceSquared = dx * dx + dy * dy;
            return distanceSquared < (BallRadius * 2) * (BallRadius * 2);
        }

        private bool Disposed = false;
        private readonly UnderneathLayerAPI layerBellow;
        private readonly List<Ball> _balls = new();
        //private const double BallRadius = 10;
        private readonly object _lock = new();

        [Conditional("DEBUG")]
        internal void CheckObjectDisposed(Action<bool> returnInstanceDisposed)
        {
            returnInstanceDisposed(Disposed);
        }
    }
}
