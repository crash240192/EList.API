using FluentMigrator;

namespace EList.Database
{
    [Migration(4, "message-votes")]
    public class MessageVotesMigration : Migration
    {
        public override void Down() { }

        public override void Up() => Execute.EmbeddedScript("MessageVotes.sql");
    }
}
