using EList.Common.Models;
using EList.Common.Support;
using EList.Models.ContactData;
using EList.Models.Enums;
using EList.Models.Person;
using EList.Repositories.Interfaces;
using EList.Validators.Interfaces;

namespace EList.Validators.Impl
{
    public class PersonValidator : IPersonValidator
    {
        private const int MaxNameLength = 100;
        private static readonly DateTime MinBirthDate = DateTime.UtcNow.Date.AddYears(-120);
        private static readonly DateTime MaxBirthDate = DateTime.UtcNow.Date;

        private readonly IAccountsRepository _accountsRepository;

        public PersonValidator(IAccountsRepository accountsRepository)
        {
            _accountsRepository = accountsRepository;
        }

        public CommandResult ValidateCreation(PersonRequest request)
        {
            return ValidateRequest(request);
        }

        public async Task<CommandResult> ValidateUpdation(Guid accountId, PersonRequest request)
        {
            var accountExistsResult = await ValidateAccountExists(accountId);
            if (!accountExistsResult.Success)
                return accountExistsResult;

            return ValidateRequest(request);
        }

        public async Task<CommandResult> ValidateAccountExists(Guid accountId)
        {
            var person = await _accountsRepository.GetAccountAsync(accountId);

            if (person == null)
                return CommandResult.Fail(ErrorCode.PersonNotExists, "Аккаунт пользователя не найден");

            return CommandResult.OK;
        }

        private static CommandResult ValidateRequest(PersonRequest request)
        {
            if (request == null)
                return CommandResult.Fail(ErrorCode.IsNullOrEmpty, "Персональные данные не указаны");

            if (string.IsNullOrWhiteSpace(request.FirstName))
                return CommandResult.Fail(ErrorCode.InvalidFirstName, "Имя пользователя не указано");

            if (string.IsNullOrWhiteSpace(request.LastName))
                return CommandResult.Fail(ErrorCode.InvalidLastName, "Фамилия пользователя не указана");

            if (request.FirstName.Trim().Length > MaxNameLength)
                return CommandResult.Fail(ErrorCode.InvalidFirstName, "Слишком длинное имя пользователя");

            if (request.LastName.Trim().Length > MaxNameLength)
                return CommandResult.Fail(ErrorCode.InvalidLastName, "Слишком длинная фамилия пользователя");

            if (!string.IsNullOrWhiteSpace(request.Patronymic) && request.Patronymic.Trim().Length > MaxNameLength)
                return CommandResult.Fail(ErrorCode.InvalidValue, "Слишком длинное отчество");

            if (request.Gender.HasValue && !Enum.IsDefined(typeof(Gender), request.Gender.Value))
                return CommandResult.Fail(ErrorCode.InvalidValue, "Указан некорректный пол");

            if (request.BirthDate.HasValue)
            {
                var birthDate = request.BirthDate.Value.Date;
                if (birthDate < MinBirthDate || birthDate > MaxBirthDate)
                    return CommandResult.Fail(ErrorCode.InvalidValue, "Указана некорректная дата рождения");
            }

            return CommandResult.OK;
        }
    }
}
