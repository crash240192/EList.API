namespace EList.Models.EventTemplates
{
    /// <summary>
    /// Запрос на поиск доступных шаблонов мероприятий
    /// </summary>
    public class EventTemplateSearchRequest
    {
        /// <summary>
        /// Идентификатор организации, от имени которой создаётся мероприятие.
        /// Если не указан — возвращаются шаблоны текущего пользователя.
        /// </summary>
        public Guid? OrganizationId { get; set; }

        /// <summary>
        /// Фильтр по названию шаблона (частичное совпадение)
        /// </summary>
        public string? Name { get; set; }
    }
}
