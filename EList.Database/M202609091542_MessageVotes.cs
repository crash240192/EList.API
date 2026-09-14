using FluentMigrator;

namespace EList.Database
{
    [Migration(4, "message-votes")]
    public class M202609091542_MessageVotes : Migration
    {
        public override void Down() { }

        public override void Up() => Execute.EmbeddedScript("M202609091542_message_votes.sql");
    }
}
