using LinqToDB.Mapping;

namespace EList.DbDataProvider.Models.Enums
{
    public enum MessageVoteValue
    {
        [MapValue(Value = "like")]
        Like = 1,

        [MapValue(Value = "dislike")]
        Dislike = 2
    }
}
