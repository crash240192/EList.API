namespace EList.Models.Conversations
{
    public class MessagePathNode
    {
        public Guid MessageId { get; set; }
        public Guid? ParentId { get; set; }
        /// <summary>Индекс страницы среди сиблингов под ParentId (для корня — среди корней диалога).</summary>
        public int PageIndex { get; set; }
    }

    /// <summary>
    /// Позиция сообщения в дереве обсуждения для deep-link / прокрутки.
    /// </summary>
    public class MessageLocation
    {
        public Guid MessageId { get; set; }
        public Guid ConversationId { get; set; }
        public Guid? EventId { get; set; }

        /// <summary>Корневой комментарий ветки.</summary>
        public Guid RootId { get; set; }

        /// <summary>Прямой родитель (ReplyTo), null для корня.</summary>
        public Guid? ParentId { get; set; }

        /// <summary>
        /// Путь от корня до целевого сообщения (включая цель).
        /// PageIndex — страница, на которой лежит узел среди детей своего ParentId.
        /// </summary>
        public List<MessagePathNode> Path { get; set; } = new();

        /// <summary>Предки без целевого сообщения (для раскрытия веток).</summary>
        public List<Guid> AncestorIds { get; set; } = new();

        public int RootPageIndex { get; set; }
        public int SiblingPageIndex { get; set; }
    }
}
