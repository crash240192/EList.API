using LinqToDB.Mapping;

namespace EList.DbDataProvider.Models.Enums
{
    public enum MessageVoteValue
    {
        [MapValue(Value = "like")]
        Like,

        [MapValue(Value = "dislike")]
        Dislike
    }
}
