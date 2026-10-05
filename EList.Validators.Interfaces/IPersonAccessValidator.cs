using EList.Common.Models;
using EList.Models.Person;

namespace EList.Validators.Interfaces
{
    public interface IPersonAccessValidator
    {
        /// <summary>
        /// Проверяет, может ли пользователь просматривать персональные данные аккаунта.
        /// </summary>
        Task<CommandResult> CanViewPersonInfoAsync(Guid targetAccountId, Guid? viewerAccountId);

        /// <summary>
        /// Проверяет, может ли пользователь изменять персональные данные аккаунта.
        /// </summary>
        CommandResult CanEditPersonInfo(Guid targetAccountId, Guid editorAccountId);

        /// <summary>
        /// Ограничивает набор полей для пользователей, которые не являются владельцем профиля,
        /// с учётом настроек приватности.
        /// </summary>
        Task<PersonInfo> ApplyViewPolicyAsync(PersonInfo person, Guid targetAccountId, Guid? viewerAccountId);
    }
}
