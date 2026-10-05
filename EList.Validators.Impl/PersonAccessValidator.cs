using EList.Common.Models;
using EList.Common.Support;
using EList.Models.Person;
using EList.Repositories.Interfaces;
using EList.Services.Interfaces;
using EList.Validators.Interfaces;

namespace EList.Validators.Impl
{
    public class PersonAccessValidator : IPersonAccessValidator
    {
        private readonly IAccountsRepository _accountsRepository;
        private readonly IProfilePrivacyService _profilePrivacyService;

        public PersonAccessValidator(
            IAccountsRepository accountsRepository,
            IProfilePrivacyService profilePrivacyService)
        {
            _accountsRepository = accountsRepository;
            _profilePrivacyService = profilePrivacyService;
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

        public async Task<PersonInfo> ApplyViewPolicyAsync(PersonInfo person, Guid targetAccountId, Guid? viewerAccountId)
        {
            var ageYears = CalculateAgeYears(person.BirthDate);
            var isBirthdayToday = IsBirthdayToday(person.BirthDate);

            if (viewerAccountId == targetAccountId)
            {
                person.AgeYears = ageYears;
                person.IsBirthdayToday = isBirthdayToday;
                return person;
            }

            var settings = await _profilePrivacyService.GetOrDefaultAsync(targetAccountId);

            var canSeeAge = await _profilePrivacyService.CanViewerSeeAsync(
                targetAccountId, viewerAccountId, settings.AgeVisibility);
            var canSeeGender = await _profilePrivacyService.CanViewerSeeAsync(
                targetAccountId, viewerAccountId, settings.GenderVisibility);

            // Полная дата рождения чужим не отдаём — только возраст / флаг ДР.
            person.BirthDate = null;
            person.Patronymic = null;
            person.Gender = canSeeGender ? person.Gender : null;
            person.AgeYears = canSeeAge ? ageYears : null;
            person.IsBirthdayToday = settings.ShowBirthdayToday && isBirthdayToday == true
                ? true
                : null;

            return person;
        }

        private static int? CalculateAgeYears(DateTime? birthDate)
        {
            if (birthDate == null)
                return null;

            var today = DateTime.UtcNow.Date;
            var birth = birthDate.Value.Date;
            var age = today.Year - birth.Year;
            if (birth > today.AddYears(-age))
                age--;
            return age < 0 ? null : age;
        }

        private static bool? IsBirthdayToday(DateTime? birthDate)
        {
            if (birthDate == null)
                return null;

            var today = DateTime.UtcNow.Date;
            var birth = birthDate.Value.Date;
            return birth.Month == today.Month && birth.Day == today.Day;
        }
    }
}
