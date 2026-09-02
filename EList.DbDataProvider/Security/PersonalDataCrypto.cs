using EList.DbDataProvider.Models;
using System.Globalization;

namespace EList.DbDataProvider.Security
{
    /// <summary>
    /// Encrypt/decrypt personal fields on DTOs before write / after read.
    /// </summary>
    public static class PersonalDataCrypto
    {
        public static void EncryptContact(ContactDataDto item, IFieldEncryptor crypto)
        {
            if (item == null || crypto == null)
                return;

            var plain = item.Value;
            if (!string.IsNullOrEmpty(plain) && !crypto.IsEncrypted(plain))
            {
                item.ValueHash = crypto.BlindIndex(plain);
                item.Value = crypto.Encrypt(plain)!;
            }
            else if (!string.IsNullOrEmpty(plain) && string.IsNullOrEmpty(item.ValueHash))
            {
                // Уже ciphertext без hash — не восстановить plaintext; hash пустой.
            }
        }

        public static void DecryptContact(ContactDataDto? item, IFieldEncryptor crypto)
        {
            if (item == null || crypto == null)
                return;

            item.Value = crypto.Decrypt(item.Value)!;
        }

        public static void EncryptPerson(PersonInfoDto item, IFieldEncryptor crypto)
        {
            if (item == null || crypto == null)
                return;

            item.FirstName = crypto.Encrypt(item.FirstName)!;
            item.LastName = crypto.Encrypt(item.LastName)!;
            item.Patronymic = crypto.Encrypt(item.Patronymic)!;

            if (!string.IsNullOrEmpty(item.Birthdate) && !crypto.IsEncrypted(item.Birthdate))
            {
                // Уже ISO или datetime text — шифруем как строку
                if (DateTime.TryParse(item.Birthdate, CultureInfo.InvariantCulture,
                        DateTimeStyles.RoundtripKind, out var dt))
                {
                    item.Birthdate = crypto.Encrypt(dt.ToString("o", CultureInfo.InvariantCulture));
                }
                else
                {
                    item.Birthdate = crypto.Encrypt(item.Birthdate);
                }
            }
        }

        public static void EncryptPersonFromDates(
            PersonInfoDto item,
            DateTime? birthDate,
            IFieldEncryptor crypto)
        {
            if (item == null || crypto == null)
                return;

            item.FirstName = crypto.Encrypt(item.FirstName)!;
            item.LastName = crypto.Encrypt(item.LastName)!;
            item.Patronymic = crypto.Encrypt(item.Patronymic)!;
            item.Birthdate = birthDate == null
                ? null
                : crypto.Encrypt(birthDate.Value.ToString("o", CultureInfo.InvariantCulture));
        }

        public static void DecryptPerson(PersonInfoDto? item, IFieldEncryptor crypto)
        {
            if (item == null || crypto == null)
                return;

            item.FirstName = crypto.Decrypt(item.FirstName)!;
            item.LastName = crypto.Decrypt(item.LastName)!;
            item.Patronymic = crypto.Decrypt(item.Patronymic)!;
            item.Birthdate = crypto.Decrypt(item.Birthdate);
        }

        public static DateTime? ParseBirthdate(string? stored)
        {
            if (string.IsNullOrWhiteSpace(stored))
                return null;

            if (DateTime.TryParse(stored, CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind, out var dt))
                return dt;

            if (DateTime.TryParse(stored, out dt))
                return dt;

            return null;
        }

        public static void EncryptLegal(OrganizationLegalDto item, IFieldEncryptor crypto)
        {
            if (item == null || crypto == null)
                return;

            if (!string.IsNullOrEmpty(item.Inn) && !crypto.IsEncrypted(item.Inn))
            {
                item.InnHash = crypto.BlindIndexDigits(item.Inn);
                item.Inn = crypto.Encrypt(item.Inn);
            }

            item.Ogrn = crypto.Encrypt(item.Ogrn);
            item.Kpp = crypto.Encrypt(item.Kpp);
            item.LegalAddress = crypto.Encrypt(item.LegalAddress);
            item.HeadName = crypto.Encrypt(item.HeadName);
            item.HeadBasis = crypto.Encrypt(item.HeadBasis);
        }

        public static void DecryptLegal(OrganizationLegalDto? item, IFieldEncryptor crypto)
        {
            if (item == null || crypto == null)
                return;

            item.Inn = crypto.Decrypt(item.Inn);
            item.Ogrn = crypto.Decrypt(item.Ogrn);
            item.Kpp = crypto.Decrypt(item.Kpp);
            item.LegalAddress = crypto.Decrypt(item.LegalAddress);
            item.HeadName = crypto.Decrypt(item.HeadName);
            item.HeadBasis = crypto.Decrypt(item.HeadBasis);
        }

        public static void EncryptPayout(OrganizationPayoutDto item, IFieldEncryptor crypto)
        {
            if (item == null || crypto == null)
                return;

            item.BankAccount = crypto.Encrypt(item.BankAccount);
            item.Bik = crypto.Encrypt(item.Bik);
            item.BankName = crypto.Encrypt(item.BankName);
            item.TaxRegime = crypto.Encrypt(item.TaxRegime);
            item.ProviderSellerId = crypto.Encrypt(item.ProviderSellerId);
        }

        public static void DecryptPayout(OrganizationPayoutDto? item, IFieldEncryptor crypto)
        {
            if (item == null || crypto == null)
                return;

            item.BankAccount = crypto.Decrypt(item.BankAccount);
            item.Bik = crypto.Decrypt(item.Bik);
            item.BankName = crypto.Decrypt(item.BankName);
            item.TaxRegime = crypto.Decrypt(item.TaxRegime);
            item.ProviderSellerId = crypto.Decrypt(item.ProviderSellerId);
        }
    }
}
