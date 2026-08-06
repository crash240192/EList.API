using Newtonsoft.Json;

namespace EList.Services.Impl.OrganizationRegistry
{
    internal class DaDataPartyResponse
    {
        [JsonProperty("suggestions")]
        public List<DaDataPartySuggestion>? Suggestions { get; set; }
    }

    internal class DaDataPartySuggestion
    {
        [JsonProperty("value")]
        public string? Value { get; set; }

        [JsonProperty("unrestricted_value")]
        public string? UnrestrictedValue { get; set; }

        [JsonProperty("data")]
        public DaDataPartyData? Data { get; set; }
    }

    internal class DaDataPartyData
    {
        [JsonProperty("inn")]
        public string? Inn { get; set; }

        [JsonProperty("kpp")]
        public string? Kpp { get; set; }

        [JsonProperty("ogrn")]
        public string? Ogrn { get; set; }

        [JsonProperty("type")]
        public string? Type { get; set; }

        [JsonProperty("name")]
        public DaDataPartyName? Name { get; set; }

        [JsonProperty("fio")]
        public DaDataPartyFio? Fio { get; set; }

        [JsonProperty("management")]
        public DaDataPartyManagement? Management { get; set; }

        [JsonProperty("address")]
        public DaDataPartyAddress? Address { get; set; }

        [JsonProperty("state")]
        public DaDataPartyState? State { get; set; }

        [JsonProperty("branch_type")]
        public string? BranchType { get; set; }
    }

    internal class DaDataPartyName
    {
        [JsonProperty("full_with_opf")]
        public string? FullWithOpf { get; set; }

        [JsonProperty("short_with_opf")]
        public string? ShortWithOpf { get; set; }

        [JsonProperty("full")]
        public string? Full { get; set; }

        [JsonProperty("short")]
        public string? Short { get; set; }
    }

    internal class DaDataPartyFio
    {
        [JsonProperty("surname")]
        public string? Surname { get; set; }

        [JsonProperty("name")]
        public string? Name { get; set; }

        [JsonProperty("patronymic")]
        public string? Patronymic { get; set; }
    }

    internal class DaDataPartyManagement
    {
        [JsonProperty("name")]
        public string? Name { get; set; }

        [JsonProperty("post")]
        public string? Post { get; set; }
    }

    internal class DaDataPartyAddress
    {
        [JsonProperty("value")]
        public string? Value { get; set; }

        [JsonProperty("unrestricted_value")]
        public string? UnrestrictedValue { get; set; }

        [JsonProperty("data")]
        public DaDataPartyAddressData? Data { get; set; }
    }

    internal class DaDataPartyAddressData
    {
        [JsonProperty("source")]
        public string? Source { get; set; }
    }

    internal class DaDataPartyState
    {
        [JsonProperty("status")]
        public string? Status { get; set; }
    }
}
