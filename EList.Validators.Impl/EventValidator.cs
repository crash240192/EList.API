using EList.Common.Models;
using EList.Common.Support;
using EList.Models.Enums;
using EList.Models.Events;
using EList.Models.Events.EventMetadata;
using EList.Validators.Interfaces;

namespace EList.Validators.Impl
{
    public class EventValidator : IEventValidator
    {
        public const int MaxNameLength = 255;
        public const int MaxAddressLength = 500;
        public const int MaxDescriptionLength = 32000;
        public const int MaxSearchNameLength = 255;
        public const int MaxLocationRangeMeters = 500_000;
        public const int MaxGuidListSize = 1000;
        public const int MaxMaxPersonsCount = 1_000_000;
        public const double MaxEventCost = EventCostLimits.Max;

        private static readonly int[] AllowedAgeLimits = Enum.GetValues<AgeRating>().Cast<int>().ToArray();

        public CommandResult ValidateEventBody(EventRequest? request, bool requireName = true)
        {
            if (request == null)
                return CommandResult.Fail(ErrorCode.IsNullOrEmpty, "Данные мероприятия не указаны");

            if (requireName && string.IsNullOrWhiteSpace(request.Name))
                return CommandResult.Fail(ErrorCode.IsNullOrEmpty, "Название мероприятия не указано");

            var nameError = ValidationCommon.ValidateOptionalTextLength(request.Name, MaxNameLength, "Название мероприятия");
            if (!nameError.Success)
                return nameError;

            var addressError = ValidationCommon.ValidateOptionalTextLength(request.Address, MaxAddressLength, "Адрес");
            if (!addressError.Success)
                return addressError;

            var descriptionError = ValidationCommon.ValidateOptionalTextLength(
                request.Description, MaxDescriptionLength, "Описание");
            if (!descriptionError.Success)
                return descriptionError;

            if (request.EndTime < request.StartTime)
                return CommandResult.Fail(ErrorCode.InvalidValue, "Дата окончания не может быть раньше даты начала");

            if (request.Latitude is < -90 or > 90)
                return CommandResult.Fail(ErrorCode.InvalidValue, "Некорректная широта (допустимо от -90 до 90)");

            if (request.Longitude is < -180 or > 180)
                return CommandResult.Fail(ErrorCode.InvalidValue, "Некорректная долгота (допустимо от -180 до 180)");

            return CommandResult.OK;
        }

        public CommandResult ValidateParameters(EventParametersRequest? parameters)
        {
            if (parameters == null)
                return CommandResult.Fail(ErrorCode.IsNullOrEmpty, "Параметры мероприятия не указаны");

            if (!AllowedAgeLimits.Contains(parameters.AgeLimit))
                return CommandResult.Fail(
                    ErrorCode.InvalidAgeLimitValue,
                    "Значение возрастного ограничения может принимать значения '0', '6', '12', '16' или '18'");

            if (parameters.Cost is < 0)
                return CommandResult.Fail(ErrorCode.InvalidValue, "Стоимость не может быть отрицательной");

            if (parameters.Cost is double cost)
            {
                if (double.IsNaN(cost) || double.IsInfinity(cost))
                    return CommandResult.Fail(ErrorCode.InvalidValue, "Некорректная стоимость мероприятия");

                if (cost > MaxEventCost)
                    return CommandResult.Fail(
                        ErrorCode.InvalidValue,
                        $"Стоимость не может превышать {MaxEventCost:N0} ₽");
            }

            if (parameters.MaxPersonsCount is < 0)
                return CommandResult.Fail(ErrorCode.InvalidValue, "Максимальное число участников не может быть отрицательным");

            if (parameters.MaxPersonsCount is > MaxMaxPersonsCount)
                return CommandResult.Fail(ErrorCode.InvalidValue, "Слишком большое значение максимального числа участников");

            if (parameters.AllowedGender.HasValue && !Enum.IsDefined(typeof(Gender), parameters.AllowedGender.Value))
                return CommandResult.Fail(ErrorCode.InvalidValue, "Указан некорректный допустимый пол участников");

            if (parameters.TicketTypes != null)
            {
                var typesError = ValidateTicketTypes(parameters.TicketTypes, parameters.TicketsEnabled);
                if (!typesError.Success)
                    return typesError;
            }
            else if (parameters.TicketsEnabled)
            {
                // TicketTypes == null: типы не переданы — сервис сделает fallback «Стандарт» из Cost.
            }

            return CommandResult.OK;
        }

        private static CommandResult ValidateTicketTypes(List<EventTicketTypeRequest>? ticketTypes, bool ticketsEnabled)
        {
            if (ticketTypes == null)
                return CommandResult.OK;

            if (ticketTypes.Count == 0)
            {
                if (ticketsEnabled)
                    return CommandResult.Fail(ErrorCode.InvalidValue,
                        "При продаже билетов нужен хотя бы один активный тип билета");
                return CommandResult.OK;
            }

            var activeCount = 0;
            foreach (var t in ticketTypes)
            {
                if (string.IsNullOrWhiteSpace(t.Name))
                    return CommandResult.Fail(ErrorCode.InvalidValue, "Укажите название типа билета");
                if (t.Name.Trim().Length > 120)
                    return CommandResult.Fail(ErrorCode.InvalidValue, "Название типа билета слишком длинное");
                if (t.Price < 0)
                    return CommandResult.Fail(ErrorCode.InvalidValue, "Цена типа билета не может быть отрицательной");
                if (t.Price > (decimal)MaxEventCost)
                    return CommandResult.Fail(ErrorCode.InvalidValue,
                        $"Цена типа билета не может превышать {MaxEventCost:N0} ₽");
                if (t.Capacity is <= 0)
                    return CommandResult.Fail(ErrorCode.InvalidValue, "Лимит мест типа должен быть больше 0");
                if (t.Active)
                    activeCount++;
            }

            if (ticketsEnabled && activeCount == 0)
                return CommandResult.Fail(ErrorCode.InvalidValue,
                    "При продаже билетов нужен хотя бы один активный тип билета");

            return CommandResult.OK;
        }

        public CommandResult ValidateCreateRequest(CreateEventRequest? request)
        {
            if (request == null)
                return CommandResult.Fail(ErrorCode.IsNullOrEmpty, "Запрос на создание мероприятия не указан");

            var bodyError = ValidateEventBody(request.Event);
            if (!bodyError.Success)
                return bodyError;

            if (request.EventParameters != null)
            {
                var parametersError = ValidateParameters(request.EventParameters);
                if (!parametersError.Success)
                    return parametersError;
            }

            var typesError = ValidateEventTypeIds(request.EventTypes, requireAtLeastOne: true);
            if (!typesError.Success)
                return typesError;

            var organizatorsError = ValidationCommon.ValidateGuidList(
                request.OrganizatorAccountIds, "Список организаторов-аккаунтов", MaxGuidListSize);
            if (!organizatorsError.Success)
                return organizatorsError;

            var orgsError = ValidationCommon.ValidateGuidList(
                request.OrganizatorOrganizationIds, "Список организаций-организаторов", MaxGuidListSize);
            if (!orgsError.Success)
                return orgsError;

            foreach (var listError in new[]
            {
                ValidationCommon.ValidateGuidList(request.InviteUsers, "Список приглашённых", MaxGuidListSize),
                ValidationCommon.ValidateGuidList(request.BlackList, "Чёрный список", MaxGuidListSize),
                ValidationCommon.ValidateGuidList(request.WhiteList, "Белый список", MaxGuidListSize)
            })
            {
                if (!listError.Success)
                    return listError;
            }

            return CommandResult.OK;
        }

        public CommandResult ValidateEventTypeIds(List<Guid>? typeIds, bool requireAtLeastOne = false)
        {
            return ValidationCommon.ValidateGuidList(
                typeIds,
                "Список типов мероприятия",
                MaxGuidListSize,
                allowEmpty: !requireAtLeastOne,
                requireAtLeastOne: requireAtLeastOne);
        }

        public CommandResult ValidateSearchRequest(EventsSearchRequest? request)
        {
            if (request == null)
                return CommandResult.Fail(ErrorCode.IsNullOrEmpty, "Параметры поиска не указаны");

            if (request.StartTime != null && request.EndTime != null && request.EndTime < request.StartTime)
                return CommandResult.Fail(ErrorCode.InvalidValue, "Конец периода поиска не может быть раньше начала");

            if (request.Latitude is < -90 or > 90)
                return CommandResult.Fail(ErrorCode.InvalidValue, "Некорректная широта в параметрах поиска");

            if (request.Longitude is < -180 or > 180)
                return CommandResult.Fail(ErrorCode.InvalidValue, "Некорректная долгота в параметрах поиска");

            if (request.LocationRange is <= 0)
                return CommandResult.Fail(ErrorCode.InvalidValue, "Радиус поиска должен быть больше нуля");

            if (request.LocationRange is > MaxLocationRangeMeters)
                return CommandResult.Fail(ErrorCode.InvalidValue, "Слишком большой радиус поиска");

            if (request.AgeLimit != null && !AllowedAgeLimits.Contains(request.AgeLimit.Value))
                return CommandResult.Fail(ErrorCode.InvalidAgeLimitValue, "Некорректное значение возрастного порога в поиске");

            if (request.AllowedGender.HasValue && !Enum.IsDefined(typeof(Gender), request.AllowedGender.Value))
                return CommandResult.Fail(ErrorCode.InvalidValue, "Некорректный пол в параметрах поиска");

            if (request.Price is < 0)
                return CommandResult.Fail(ErrorCode.InvalidValue, "Стоимость в поиске не может быть отрицательной");

            if (request.Price is double price)
            {
                if (double.IsNaN(price) || double.IsInfinity(price))
                    return CommandResult.Fail(ErrorCode.InvalidValue, "Некорректная стоимость в параметрах поиска");

                if (price > MaxEventCost)
                    return CommandResult.Fail(
                        ErrorCode.InvalidValue,
                        $"Стоимость в поиске не может превышать {MaxEventCost:N0} ₽");
            }

            var nameError = ValidationCommon.ValidateOptionalTextLength(request.Name, MaxSearchNameLength, "Название в поиске");
            if (!nameError.Success)
                return nameError;

            var typesError = ValidationCommon.ValidateGuidList(request.Types, "Типы в поиске", MaxGuidListSize);
            if (!typesError.Success)
                return typesError;

            var categoriesError = ValidationCommon.ValidateGuidList(request.Categories, "Категории в поиске", MaxGuidListSize);
            if (!categoriesError.Success)
                return categoriesError;

            return CommandResult.OK;
        }
    }
}
