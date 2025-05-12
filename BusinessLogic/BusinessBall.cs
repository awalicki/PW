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

        public double GetWeight()
        {
            return _underlyingBall.Weight;
        }
        
        public double GetBallRadius()
        {
            return _underlyingBall.BallRadius;
        }

        public void SetVelocity(Data.IVector velocity)
        {
            _underlyingBall.Velocity = velocity;
        }

        #endregion IBall

        #region private

        private void RaisePositionChangeEvent(object? sender, Data.IVector dataPosition)
        {
            _currentPosition = new Position(dataPosition.x, dataPosition.y);

            IPosition pos1 = _currentPosition;
            Data.IVector v1 = GetVelocity();
            double currentVx = v1.x;
            double currentVy = v1.y;
            double newVx = currentVx;
            double newVy = currentVy;
            double positionX = pos1.x;
            double positionY = pos1.y;
            if (positionX <= 0 && currentVx < 0)
            {
                newVx = -currentVx;
                Console.WriteLine($"BL: X Bounce Left at {positionX:F2}");
            }
            else if (positionX >= _tableWidth && currentVx > 0)
            {
                newVx = -currentVx;
                Console.WriteLine($"BL: X Bounce Right at {positionX:F2}");
            }

            if (positionY <= 0 && currentVy < 0)
            {
                newVy = -currentVy;
                Console.WriteLine($"BL: Y Bounce Top at {positionY:F2}");
            }
            else if (positionY >= _tableHeight && currentVy > 0)
            {
                newVy = -currentVy;
                Console.WriteLine($"BL: Y Bounce Bottom at {positionY:F2}");
            }

            if (newVx != currentVx || newVy != currentVy)
            {
                SetVelocity(_dataLayer.CreateVector(newVx, newVy));
                v1 = GetVelocity();
                currentVx = v1.x;
                currentVy = v1.y;
                Console.WriteLine($"BL: Updated Data Ball Velocity (Wall) to ({newVx:F2}, {newVy:F2})");
            }           
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