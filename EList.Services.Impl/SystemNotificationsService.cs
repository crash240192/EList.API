using EList.Common.CorrelationId;
using EList.Common.Extensions;
using EList.Common.Logger;
using EList.Common.Models;
using EList.Common.Support;
using EList.Common.TemplateParser;
using EList.Models.ContactData;
using EList.Models.Enums;
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
            if (accountId == null)
            {
                var tokenData = await _authorizationRepository.GetAuthorizationDataAsync(_accountDataHolder.Token.Value);
                contacts = await _contactsRepository.GetAccountContactsAsync(tokenData.AccountId);
            }
            else
            {
                contacts = await _contactsRepository.GetAccountContactsAsync(accountId.Value);
            }

            contacts = contacts?.Where(i => i.IsAuthorizationContact).ToList();

            if (!contacts.NullSafeAny())
                return CommandResult<string>.Fail(ErrorCode.UserHasNoNecessaryContacts, "У пользователя отсутствует контакт для уведомления");

            var contact = contacts.FirstOrDefault();

            var tokens = new Dictionary<string, string>
            {
                { "#ACTIVATION_CODE#", tokenData.ActivationKey}
            };

            var notification = await _notificationsRepository.GetNotificationByTypeAsync(notificationType);

            var isEmail = MailAddress.TryCreate(contact.Value, out var eMail);
            if (isEmail)
            {
                var messageBody = _templateParser.Parse(notification.Message, tokens);
                await _smtpClient.SendMessageAsync(correlationId, new Smtp.Models.Message
                {
                    IsBodyHtml = true,
                    MessageBody = messageBody,
                    MessageSubject = "EList",
                    RecipientEmail = contact.Value
                });
                logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
                return new CommandResult<string>($"Код активации был выслан на {contact.Value}");
            }

            var isPhone = true; //TODO: Валидация на корректность введения телефона
            {
                var messageBody = _templateParser.Parse(notification.ShortMessage, tokens);
                await _smsClient.SendSmsAsync(contact.Value, messageBody);
            }

            return CommandResult<string>.Fail(ErrorCode.UnableToNotifyUser, "Не удалось уведомить пользователя");
        }
    }
}
