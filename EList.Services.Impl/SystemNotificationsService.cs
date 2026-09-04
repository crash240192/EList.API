using EList.Common.CorrelationId;
using EList.Common.Extensions;
using EList.Common.Logger;
using EList.Common.Models;
using EList.Common.Support;
using EList.Common.TemplateParser;
using EList.Models.ContactData;
using EList.Models.Enums;
using EList.Models.Notifications;
using EList.Repositories.Interfaces;
using EList.Services.Interfaces;
using EList.Sms;
using EList.Smtp;
using NLog;
using System.Diagnostics;
using System.Net.Mail;

namespace EList.Services.Impl
{
    public class SystemNotificationsService : ISystemNotificationsService
    {
        #region logger
        private static readonly ILogger log = LogManager.GetCurrentClassLogger();
        private static readonly ILoggerWrapper logger = new NLogLoggerWrapper(log);
        private const string LOGGER_NAME = "EList.Services.Impl.SystemNotificationsService.";
        #endregion

        private readonly ICorrelationIdProvider _correlationIdProvider;
        private readonly IContactsRepository _contactsRepository;
        private readonly ISmtpClient _smtpClient;
        private readonly INotificationsRepository _notificationsRepository;
        private readonly IAuthorizationRepository _authorizationRepository;
        private readonly ITemplateParser _templateParser;
        private readonly ISmsClient _smsClient;
        private readonly IAccountDataHolder _accountDataHolder;

        public SystemNotificationsService(ICorrelationIdProvider correlationIdProvider,
            IContactsRepository contactsRepository,
            ISmtpClient smtpClient,
            INotificationsRepository notificationsRepository,
            IAuthorizationRepository authorizationRepository,
            ITemplateParser templateParser,
            ISmsClient smsClient,
            IAccountDataHolder accountDataHolder)
        {
            _correlationIdProvider = correlationIdProvider ?? throw new ArgumentNullException(nameof(correlationIdProvider));
            _contactsRepository = contactsRepository ?? throw new ArgumentNullException(nameof(contactsRepository));
            _smtpClient = smtpClient ?? throw new ArgumentNullException(nameof(smtpClient));
            _notificationsRepository = notificationsRepository ?? throw new ArgumentNullException(nameof(notificationsRepository));
            _authorizationRepository = authorizationRepository ?? throw new ArgumentNullException(nameof(authorizationRepository));
            _templateParser = templateParser ?? throw new ArgumentNullException(nameof(templateParser));
            _smsClient = smsClient ?? throw new ArgumentNullException(nameof(smsClient));
            _accountDataHolder = accountDataHolder;
        }

        public async Task<CommandResult<string>> NotifyUserByContactAsync(SystemNotificationType notificationType, Guid? accountId = null)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(NotifyUserByContactAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var contacts = new List<ContactDataItem>();
            var tokenData = await _authorizationRepository.GetAuthorizationDataAsync(_accountDataHolder.Token.Value);

            if (accountId == null)            
                contacts = await _contactsRepository.GetAccountContactsAsync(tokenData.AccountId);            
            else
                contacts = await _contactsRepository.GetAccountContactsAsync(accountId.Value);

            contacts = contacts?.Where(i => i.IsAuthorizationContact).ToList();

            if (!contacts.NullSafeAny())
                return CommandResult<string>.Fail(ErrorCode.UserHasNoNecessaryContacts, "У пользователя отсутствует контакт для уведомления");

            var contact = contacts.FirstOrDefault();

            var tokens = new Dictionary<string, string>
            {
                { "#ACTIVATION_CODE#", tokenData.ActivationKey}
            };

            var notification = await _notificationsRepository.GetNotificationByTypeAsync(notificationType);
            if (notification == null)
                return CommandResult<string>.Fail(ErrorCode.UnableToNotifyUser, $"Шаблон системного уведомления «{notificationType}» не найден");

            var successMessage = notificationType is SystemNotificationType.Activation
                or SystemNotificationType.ResetPasswordRequest
                ? $"Код был выслан на {contact.Value}"
                : $"Уведомление отправлено на {contact.Value}";

            var isEmail = MailAddress.TryCreate(contact.Value, out _);
            if (isEmail)
            {
                var messageBody = _templateParser.Parse(notification.Message, tokens);
                await _smtpClient.SendMessageAsync(correlationId, new Smtp.Models.Message
                {
                    IsBodyHtml = true,
                    MessageBody = messageBody,
                    MessageSubject = notification.Header ?? "EList",
                    RecipientEmail = contact.Value
                });
                logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
                return new CommandResult<string>(successMessage);
            }

            // SMS channel (phone authorization contact)
            {
                var messageBody = _templateParser.Parse(notification.ShortMessage, tokens);
                await _smsClient.SendSmsAsync(contact.Value, messageBody);
                logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
                return new CommandResult<string>(successMessage);
            }
        }

        public async Task<CommandResult<List<SystemNotification>>> GetAllAsync()
        {
            if (!_accountDataHolder.IsPlatformAdminOrAbove)
                return CommandResult<List<SystemNotification>>.Fail(ErrorCode.AccessError, "Недостаточно прав");

            var items = await _notificationsRepository.GetAllSystemNotificationsAsync();
            return new CommandResult<List<SystemNotification>>(items);
        }

        public async Task<CommandResult<SystemNotification?>> GetByIdAsync(Guid id)
        {
            if (!_accountDataHolder.IsPlatformAdminOrAbove)
                return CommandResult<SystemNotification?>.Fail(ErrorCode.AccessError, "Недостаточно прав");

            var item = await _notificationsRepository.GetSystemNotificationByIdAsync(id);
            if (item == null)
                return CommandResult<SystemNotification?>.Fail(ErrorCode.InvalidValue, "Системное уведомление не найдено");
            return new CommandResult<SystemNotification?>(item);
        }

        public async Task<CommandResult<Guid>> CreateAsync(SystemNotification item)
        {
            if (!_accountDataHolder.IsPlatformAdminOrAbove)
                return CommandResult<Guid>.Fail(ErrorCode.AccessError, "Недостаточно прав");

            if (string.IsNullOrWhiteSpace(item.Header))
                return CommandResult<Guid>.Fail(ErrorCode.IsNullOrEmpty, "Заголовок обязателен");
            if (string.IsNullOrWhiteSpace(item.Message))
                return CommandResult<Guid>.Fail(ErrorCode.IsNullOrEmpty, "Текст сообщения обязателен");
            if (string.IsNullOrWhiteSpace(item.ShortMessage))
                return CommandResult<Guid>.Fail(ErrorCode.IsNullOrEmpty, "Краткий текст обязателен");

            var id = await _notificationsRepository.CreateSystemNotificationAsync(item);
            return new CommandResult<Guid>(id);
        }

        public async Task<CommandResult> UpdateAsync(Guid id, SystemNotification item)
        {
            if (!_accountDataHolder.IsPlatformAdminOrAbove)
                return CommandResult.Fail(ErrorCode.AccessError, "Недостаточно прав");

            var existing = await _notificationsRepository.GetSystemNotificationByIdAsync(id);
            if (existing == null)
                return CommandResult.Fail(ErrorCode.InvalidValue, "Системное уведомление не найдено");

            if (string.IsNullOrWhiteSpace(item.Header))
                return CommandResult.Fail(ErrorCode.IsNullOrEmpty, "Заголовок обязателен");
            if (string.IsNullOrWhiteSpace(item.Message))
                return CommandResult.Fail(ErrorCode.IsNullOrEmpty, "Текст сообщения обязателен");
            if (string.IsNullOrWhiteSpace(item.ShortMessage))
                return CommandResult.Fail(ErrorCode.IsNullOrEmpty, "Краткий текст обязателен");

            item.Id = id;
            await _notificationsRepository.UpdateSystemNotificationAsync(item);
            return CommandResult.OK;
        }

        public async Task<CommandResult> DeleteAsync(Guid id)
        {
            if (!_accountDataHolder.IsPlatformAdminOrAbove)
                return CommandResult.Fail(ErrorCode.AccessError, "Недостаточно прав");

            var existing = await _notificationsRepository.GetSystemNotificationByIdAsync(id);
            if (existing == null)
                return CommandResult.Fail(ErrorCode.InvalidValue, "Системное уведомление не найдено");

            await _notificationsRepository.DeleteSystemNotificationAsync(id);
            return CommandResult.OK;
        }
    }
}
