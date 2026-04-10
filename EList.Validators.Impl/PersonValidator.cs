using EList.Common.Models;
using EList.Common.Support;
using EList.Models;
using EList.Models.Person;
using EList.Repositories.Interfaces;
using EList.Validators.Interfaces;
using System.Runtime.CompilerServices;

namespace EList.Validators.Impl
{
    public class PersonValidator : IPersonValidator
    {
        private readonly IPersonsRepository _personRepository;
        private readonly IAccountsRepository _accountsRepository;

        public PersonValidator(IPersonsRepository personRepository, IAccountsRepository accountsRepository)
        {
            _personRepository = personRepository;
            _accountsRepository = accountsRepository;
        }

        public CommandResult ValidateCreation(PersonRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.FirstName))
                return CommandResult.Fail(ErrorCode.InvalidFirstName, "Имя пользователя не указано");

            if (string.IsNullOrWhiteSpace(request.LastName))
                return CommandResult.Fail(ErrorCode.InvalidLastName, "Фамилия пользователя не указана");

            //if (request.Gender != 0 )
            //    return CommandResult.Fail(ErrorCode.InvalidLastName, "Пол указан неверно");

            //TODO: реализация валидации

            return CommandResult.OK;
        }

        public async Task<CommandResult> ValidateUpdation(Guid accountId, PersonRequest request)
        {
            var personExistsResult = await ValidateAccountExists(accountId);
            if (personExistsResult.Success)
                return personExistsResult;

            var creationResult = ValidateCreation(request);
            if (!creationResult.Success)
                return creationResult;

            //TODO: реализация валидации

            return CommandResult.OK;
        }

        public async Task<CommandResult> ValidateAccountExists(Guid accountId)
        {
            var person = await _accountsRepository.GetAccountAsync(accountId);

            if (person == null)
                return CommandResult.Fail(ErrorCode.PersonNotExists, $"Аккаунт пользователя не найден");

            return CommandResult.OK;
        }
    }
}