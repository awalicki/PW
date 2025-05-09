//____________________________________________________________________________________________________________________________________
//
//  Copyright (C) 2024, Mariusz Postol LODZ POLAND.
//
//  To be in touch join the community by pressing the `Watch` button and get started commenting using the discussion panel at
//
//  https://github.com/mpostol/TP/discussions/182
//
//_____________________________________________________________________________________________________________________________________

using System;

namespace TP.ConcurrentProgramming.BusinessLogic
{
    internal class Ball : IBall
    {
        private readonly Data.IBall _underlyingBall;
        private readonly Data.DataAbstractAPI _dataLayer;
        private readonly double _ballRadius;
        private readonly double _tableWidth;
        private readonly double _tableHeight;

        internal Ball(Data.IBall ball, Data.DataAbstractAPI dataLayer)
        {
            _underlyingBall = ball;
            _dataLayer = dataLayer;
            _underlyingBall.NewPositionNotification += RaisePositionChangeEvent;

            var dimensions = BusinessLogicAbstractAPI.GetDimensions;
            _ballRadius = dimensions.BallDimension / 2.0;

            _tableWidth = 392.0;
            _tableHeight = 372.0;
        }

        #region IBall

        public event EventHandler<IPosition>? NewPositionNotification;

        #endregion IBall

        #region private

        private void RaisePositionChangeEvent(object? sender, Data.IVector dataPosition)
        {
            double currentVx = _underlyingBall.Velocity.x;
            double currentVy = _underlyingBall.Velocity.y;
            double newVx = currentVx;
            double newVy = currentVy;

            double newX = dataPosition.x + currentVx;
            double newY = dataPosition.y + currentVy;

            if(newX < 0 || newX > _tableWidth) newVx = -newVx;

            if (newY < 0 || newY > _tableHeight) newVy = -newVy;

            if (newVx != currentVx || newVy != currentVy)
            {
                _underlyingBall.Velocity = _dataLayer.CreateVector(newVx, newVy);
            }

            NewPositionNotification?.Invoke(this, new Position(dataPosition.x, dataPosition.y));
        }

        #endregion private
    }
}
