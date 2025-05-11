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

            foreach (var ball in _balls)
                ball.DetachFromDataBall();

            _balls.Clear();
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

                _balls.Add(ball);

                ball.NewPositionNotification += (sender, pos) => OnBallMoved(ball);
                upperLayerHandler(initialPositionBL, ball);
            });
        }

        #endregion BusinessLogicAbstractAPI

        #region Collision Detection

        private void OnBallMoved(Ball movedBall)
        {
            var pos1 = movedBall.GetPosition();

            foreach (var other in _balls)
            {
                if (other == movedBall)
                    continue;

                var pos2 = other.GetPosition();

                if (AreBallsColliding(pos1, pos2))
                {
                    var v1 = movedBall.GetVelocity();
                    var v2 = other.GetVelocity();

                    movedBall.SetVelocity(v2);
                    other.SetVelocity(v1);

                    Console.WriteLine("BL: Collision between balls detected and resolved.");
                }
            }
        }

        private bool AreBallsColliding(IPosition pos1, IPosition pos2)
        {
            double dx = pos1.x - pos2.x;
            double dy = pos1.y - pos2.y;
            double distanceSquared = dx * dx + dy * dy;
            return distanceSquared <= 400;
        }

        #endregion Collision Detection

        #region private

        private bool Disposed = false;
        private readonly UnderneathLayerAPI layerBellow;
        private readonly List<Ball> _balls = new();
        private const double BallRadius = 10;

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
