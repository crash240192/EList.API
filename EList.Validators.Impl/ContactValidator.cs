using EList.Common.Models;
using EList.Common.Support;
using EList.Models.ContactData;
using EList.Repositories.Interfaces;
using EList.Validators.Interfaces;
using System.Net.Mail;
using System.Text.RegularExpressions;

namespace EList.Validators.Impl
{
    public class ContactValidator : IContactValidator
    {
        private const int MaxContactValueLength = 256;

        private readonly IContactsRepository _contactsRepository;

        public ContactValidator(IContactsRepository contactsRepository)
        {
            _contactsRepository = contactsRepository;
        }

        public async Task<CommandResult> ValidateAsync(
            ContactRequest request,
            ContactDataItem? existingContact = null,
            Guid? ownerAccountId = null,
            bool allowAuthorizationContact = false)
        {
            if (request == null)
                return CommandResult.Fail(ErrorCode.IsNullOrEmpty, "Данные контакта не указаны");

            if (request.TypeId == Guid.Empty)
                return CommandResult.Fail(ErrorCode.InvalidValue, "Не указан тип контакта");

            if (string.IsNullOrWhiteSpace(request.Value))
                return CommandResult.Fail(ErrorCode.IsNullOrEmpty, "Значение контакта не указано");

            var value = request.Value.Trim();
            if (value.Length > MaxContactValueLength)
                return CommandResult.Fail(ErrorCode.InvalidValue, "Слишком длинное значение контакта");

            var contactType = await _contactsRepository.GetContactTypeAsync(request.TypeId);
            if (contactType == null)
                return CommandResult.Fail(ErrorCode.InvalidValue, "Указан неизвестный тип контакта");

            if (!IsValueValidForType(value, contactType))
                return CommandResult.Fail(ErrorCode.InvalidValue, $"Значение контакта не соответствует типу \"{contactType.Name}\"");

            var isAuthorizationContact = request.IsAuthorizationContact
                || (existingContact?.IsAuthorizationContact ?? false);

            if (isAuthorizationContact && request.Show)
                return CommandResult.Fail(ErrorCode.InvalidValue, "Контакт для авторизации нельзя показывать другим пользователям");

            if (request.IsAuthorizationContact)
            {
                if (!allowAuthorizationContact)
                    return CommandResult.Fail(ErrorCode.AccessError, "Контакт для авторизации можно указать только при регистрации аккаунта");

                if (ownerAccountId != null)
                {
                    var existingAuthorizationContact = await _contactsRepository.GetAuthorizationContactAsync(ownerAccountId.Value);
                    if (existingAuthorizationContact != null)
                        return CommandResult.Fail(ErrorCode.AuthorizationContactIsNotEmpty, "Контакт для авторизации уже указан");
                }
            }

            var valueChanged = existingContact == null
                || !string.Equals(existingContact.Value, value, StringComparison.Ordinal)
                || existingContact.ContactType?.Id != request.TypeId;

            if (valueChanged)
            {
                var contactIsAvailable = await _contactsRepository.CheckContactIsEmptyAsync(value, request.TypeId);
                if (!contactIsAvailable)
                    return CommandResult.Fail(ErrorCode.AuthorizationContactIsNotEmpty, "Указанный контакт уже используется");
            }

            return CommandResult.OK;
        }

        private static bool IsValueValidForType(string value, ContactType contactType)
        {
            if (!string.IsNullOrWhiteSpace(contactType.Mask))
            {
                try
                {
                    return Regex.IsMatch(value, contactType.Mask);
                }
                catch (ArgumentException)
                {
                    return false;
                }
            }

            return MailAddress.TryCreate(value, out _);
        }
    }
}
