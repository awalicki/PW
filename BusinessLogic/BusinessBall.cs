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
using TP.ConcurrentProgramming.Data;

namespace TP.ConcurrentProgramming.BusinessLogic
{
    internal class Ball : IBall
    {
        private readonly Data.IBall _underlyingBall;
        private readonly Data.DataAbstractAPI _dataLayer;
        private IPosition _currentPosition;
        private readonly double _tableWidth = 392;
        private readonly double _tableHeight = 372;

        internal Ball(Data.IBall ball, Data.DataAbstractAPI dataLayer, IPosition initialPosition)
        {
            _underlyingBall = ball;
            _dataLayer = dataLayer;
            _currentPosition = initialPosition;
            _underlyingBall.NewPositionNotification += RaisePositionChangeEvent;
        }

        #region IBall

        public event EventHandler<IPosition>? NewPositionNotification;

        public IPosition GetPosition()
        {
            return _currentPosition;
        }

        public Data.IVector GetVelocity()
        {
            return _underlyingBall.Velocity;
        }

        public void SetVelocity(Data.IVector velocity)
        {
            _underlyingBall.Velocity = velocity;
        }

        #endregion IBall

        #region private

        private void RaisePositionChangeEvent(object? sender, Data.IVector dataPosition)
        {
            double currentVx = _underlyingBall.Velocity.x;
            double currentVy = _underlyingBall.Velocity.y;
            double newVx = currentVx;
            double newVy = currentVy;
            double positionX = dataPosition.x;
            double positionY = dataPosition.y;

            if (positionX <= 0)
            {
                if (currentVx < 0)
                {
                    newVx = -currentVx;
                    Console.WriteLine($"BL: X Bounce Left at {positionX}");
                }
            }
            else if (positionX >= _tableWidth)
            {
                if (currentVx > 0)
                {
                    newVx = -currentVx;
                    Console.WriteLine($"BL: X Bounce Right at {positionX}");
                }
            }

            if (positionY <= 0)
            {
                if (currentVy < 0)
                {
                    newVy = -currentVy;
                    Console.WriteLine($"BL: Y Bounce Top at {positionY}");
                }
            }
            else if (positionY >= _tableHeight)
            {
                if (currentVy > 0)
                {
                    newVy = -currentVy;
                    Console.WriteLine($"BL: Y Bounce Bottom at {positionY}");
                }
            }

            if (newVx != currentVx || newVy != currentVy)
            {
                _underlyingBall.Velocity = _dataLayer.CreateVector(newVx, newVy);
                Console.WriteLine($"BL: Updated Data Ball Velocity to ({newVx}, {newVy})");
            }

            _currentPosition = new Position(positionX, positionY);
            NewPositionNotification?.Invoke(this, _currentPosition);
        }

        internal void DetachFromDataBall()
        {
            if (_underlyingBall != null)
            {
                _underlyingBall.NewPositionNotification -= RaisePositionChangeEvent;
                Console.WriteLine("BL: Detached from Data Ball event.");
            }
        }

        #endregion private
    }
}