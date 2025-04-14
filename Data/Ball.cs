namespace TP.ConcurrentProgramming.Data
{
    internal class Ball : IBall
    {
        #region ctor

        internal Ball(Vector initialPosition, Vector initialVelocity)
        {
            Position = initialPosition;
            Velocity = initialVelocity;
        }

        #endregion ctor

        #region IBall

        public event EventHandler<IVector>? NewPositionNotification;

        public IVector Velocity { get; set; }

        #endregion IBall

        #region private

        private Vector Position;

        private Vector InternalVelocity => (Vector)Velocity;

        private void RaiseNewPositionChangeNotification()
        {
            NewPositionNotification?.Invoke(this, Position);
        }

        internal void MoveWithBounds(Vector delta, double minX, double maxX, double minY, double maxY)
        {
            double newX = Position.x + delta.x;
            double newY = Position.y + delta.y;

            if (newX < minX || newX > maxX)
                Velocity = new Vector(-InternalVelocity.x, InternalVelocity.y);

            if (newY < minY || newY > maxY)
                Velocity = new Vector(InternalVelocity.x, -InternalVelocity.y);

            Position = new Vector(
                Math.Clamp(newX, minX, maxX),
                Math.Clamp(newY, minY, maxY)
            );

            RaiseNewPositionChangeNotification();
        }

        internal void Move(Vector delta)
        {
            Position = new Vector(Position.x + delta.x, Position.y + delta.y);
            RaiseNewPositionChangeNotification();
        }

        #endregion private
    }
}
