using AutoMapper;
using EList.DbDataProvider.Interfaces;
using EList.DbDataProvider.Models;
using EList.DbDataProvider.Models.Enums;
using EList.Models.Person;
using EList.Repositories.Interfaces;
using System.Globalization;

namespace EList.Repositories.Impl
{
    public class PersonsRepository : IPersonsRepository
    {
        private readonly IPersonsDataProvider _personDataProvider;
        private readonly IMapper _mapper;

        public PersonsRepository(IPersonsDataProvider personDataProvider, IMapper mapper)
        {
            _personDataProvider = personDataProvider;
            _mapper = mapper;
        }

        public async Task<Guid> CreatePersonInfoAsync(Guid accountId, PersonRequest request)
        {
            var mappedRequest = new PersonInfoDto
            {
                AccountId = accountId,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Birthdate = request.BirthDate?.ToString("o", CultureInfo.InvariantCulture),
                Gender = _mapper.Map<Gender>(request.Gender),
                Patronymic = request.Patronymic
            };

            return await _personDataProvider.CreatePersonInfoAsync(mappedRequest);
        }

        public async Task<PersonInfo?> GetPersonInfoAsync(Guid accountId)
        {
            var person = await _personDataProvider.GetPersonInfoAsync(accountId);
            return person != null ? _mapper.Map<PersonInfoDto, PersonInfo>(person) : null;
        }

        public async Task UpdatePersonInfoAsync(Guid accountId, PersonRequest request)
        {
            var mappedRequest = new PersonInfoDto
            {
                AccountId = accountId,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Birthdate = request.BirthDate?.ToString("o", CultureInfo.InvariantCulture),
                Gender = _mapper.Map<Gender>(request.Gender),
                Patronymic = request.Patronymic
            };

            await _personDataProvider.UpdatePersonInfoAsync(mappedRequest);
        }
    }
}
