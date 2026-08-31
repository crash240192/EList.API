using EList.Common.Models;
using EList.Common.Support;
using EList.Models.Person;
using EList.Repositories.Interfaces;
using EList.Validators.Interfaces;

namespace EList.Validators.Impl
{
    public class PersonAccessValidator : IPersonAccessValidator
    {
        private readonly IAccountsRepository _accountsRepository;

        public PersonAccessValidator(IAccountsRepository accountsRepository)
        {
            _accountsRepository = accountsRepository;
        }

        public async Task<CommandResult> CanViewPersonInfoAsync(Guid targetAccountId, Guid? viewerAccountId)
        {
            var account = await _accountsRepository.GetAccountAsync(targetAccountId);
            if (account == null)
                return CommandResult.Fail(ErrorCode.AccountNotFound, "Аккаунт не найден");

            return CommandResult.OK;
        }

        public CommandResult CanEditPersonInfo(Guid targetAccountId, Guid editorAccountId)
        {
            if (targetAccountId != editorAccountId)
                return CommandResult.Fail(ErrorCode.AccessError, "Изменять персональные данные можно только для своего аккаунта");

            return CommandResult.OK;
        }

        public PersonInfo ApplyViewPolicy(PersonInfo person, Guid targetAccountId, Guid? viewerAccountId)
        {
            if (viewerAccountId == targetAccountId)
                return person;

            return new PersonInfo
            {
                Id = person.Id,
                AccountId = person.AccountId,
                FirstName = person.FirstName,
                LastName = person.LastName,
                Patronymic = null,
                Gender = null,
                BirthDate = null
            };
        }
    }
}
