namespace JassTournamentManager.Domain.Common
{
    public abstract class BaseEntity
    {
        public Guid Id { get; protected set; }

        public DateTimeOffset CreatedAt { get; protected set; }

        public DateTimeOffset UpdatedAt { get; protected set; }

        protected BaseEntity()
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;

            Id = Guid.NewGuid();
            CreatedAt = now;
            UpdatedAt = now;
        }

        public void MarkAsUpdated()
        {
            UpdatedAt = DateTimeOffset.UtcNow;
        }
    }
}
