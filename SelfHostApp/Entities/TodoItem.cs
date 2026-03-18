using Volo.Abp.Domain.Entities;

namespace SelfHostApp.Entities
{
    public class TodoItem : BasicAggregateRoot<Guid>
    {
        public string Text { get; set; } = string.Empty;
    }
}
