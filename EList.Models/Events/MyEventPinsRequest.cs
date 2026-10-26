namespace EList.Models.Events
{
    /// <summary>Полная замена списка закрепов «Мои мероприятия» (порядок = приоритет сверху вниз).</summary>
    public class MyEventPinsRequest
    {
        public List<Guid> EventIds { get; set; } = new();
    }

    public class MyEventPinsResponse
    {
        /// <summary>Id событий сверху вниз (как в UI pins).</summary>
        public List<Guid> EventIds { get; set; } = new();
    }
}
