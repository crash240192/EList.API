namespace EList.DbDataProvider.Security
{
    /// <summary>
    /// Шифрование персональных полей at-rest (AES-GCM) и blind-index (HMAC) для поиска.
    /// </summary>
    public interface IFieldEncryptor
    {
        /// <summary>Недетерминированное шифрование. null/пусто → null/пусто.</summary>
        string? Encrypt(string? plaintext);

        /// <summary>
        /// Расшифровка. Если значение не в формате ciphertext (legacy plaintext) — возвращает как есть.
        /// </summary>
        string? Decrypt(string? stored);

        /// <summary>True, если строка уже зашифрована этим инструментом.</summary>
        bool IsEncrypted(string? stored);

        /// <summary>
        /// Детерминированный HMAC для equality-поиска (после нормализации).
        /// </summary>
        string BlindIndex(string? plaintext);

        /// <summary>Нормализация контакта: trim + lower.</summary>
        string NormalizeContact(string? value);

        /// <summary>Нормализация ИНН/ОГРН: только цифры.</summary>
        string NormalizeDigits(string? value);

        /// <summary>Blind-index для цифровых идентификаторов (ИНН и т.п.).</summary>
        string BlindIndexDigits(string? value);
    }
}
