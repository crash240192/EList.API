using EList.Common.Models;
using EList.Models.ContactData;

namespace EList.Validators.Interfaces
{
    public interface IContactValidator
    {
        /// <summary>
        /// Проверяет корректность контактных данных перед созданием или обновлением.
        /// </summary>
        Task<CommandResult> ValidateAsync(
            ContactRequest request,
            ContactDataItem? existingContact = null,
            Guid? ownerAccountId = null,
            bool allowAuthorizationContact = false);
    }

}
