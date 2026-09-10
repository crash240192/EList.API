namespace EList.Models.Conversations
{
    /// <summary>
    /// Текстовая адресация ответа на комментарий (как у YouTube/Instagram):
    /// имя или название организации вставляются в текст, не как ссылка.
    /// </summary>
    public static class MessageReplyAddress
    {
        /// <summary>
        /// Метка адресата. Комментарий организации — название (или id организации).
        /// Комментарий пользователя — имя, иначе фамилия, иначе id.
        /// </summary>
        public static string BuildLabel(
            string? firstName,
            string? lastName,
            Guid? accountId,
            string? organizationName,
            Guid? organizationId,
            Guid messageId)
        {
            if (organizationId.HasValue)
            {
                if (!string.IsNullOrWhiteSpace(organizationName))
                    return organizationName.Trim();

                return organizationId.Value.ToString();
            }

            if (!string.IsNullOrWhiteSpace(firstName))
                return firstName.Trim();

            if (!string.IsNullOrWhiteSpace(lastName))
                return lastName.Trim();

            if (accountId.HasValue)
                return accountId.Value.ToString();

            return messageId.ToString();
        }

        /// <summary>
        /// Префикс для поля ввода ответа на ответ: «Имя, ».
        /// Клиент вставляет его в <c>messageText</c>; сервер текст не дополняет.
        /// </summary>
        public static string BuildPrefix(
            string? firstName,
            string? lastName,
            Guid? accountId,
            string? organizationName,
            Guid? organizationId,
            Guid messageId)
        {
            return $"{BuildLabel(firstName, lastName, accountId, organizationName, organizationId, messageId)}, ";
        }
    }
}
