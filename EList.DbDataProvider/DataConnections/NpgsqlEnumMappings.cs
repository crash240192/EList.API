using EList.DbDataProvider.Models.Enums;
using Npgsql;
using Npgsql.NameTranslation;

namespace EList.DbDataProvider.DataConnections
{
    /// <summary>
    /// Registers PostgreSQL enums with Npgsql. Required for linq2db <c>DataType.Enum</c>
    /// writes; without this, Npgsql throws "A PostgreSQL type with the name '…' was not found"
    /// (especially for types created after the process started, or not in the default catalog).
    /// </summary>
    public static class NpgsqlEnumMappings
    {
        private static bool _registered;
        private static readonly object Sync = new();

        public static void Register()
        {
            if (_registered)
                return;

            lock (Sync)
            {
                if (_registered)
                    return;

                // Values are snake_case in PG: pending / resolved / cancelled
                var translator = new NpgsqlSnakeCaseNameTranslator();
#pragma warning disable CS0618 // GlobalTypeMapper obsolete in Npgsql 7, still needed without NpgsqlDataSource
                NpgsqlConnection.GlobalTypeMapper.MapEnum<BugReportStatus>(
                    "public.bug_report_status",
                    translator);
                NpgsqlConnection.GlobalTypeMapper.MapEnum<PlatformRole>(
                    "public.platform_role",
                    translator);
                NpgsqlConnection.GlobalTypeMapper.MapEnum<ReportTargetType>(
                    "public.report_target_type",
                    translator);
                NpgsqlConnection.GlobalTypeMapper.MapEnum<ReportTargetScope>(
                    "public.report_target_scope",
                    translator);
                NpgsqlConnection.GlobalTypeMapper.MapEnum<ReportSeverity>(
                    "public.report_severity",
                    translator);
                NpgsqlConnection.GlobalTypeMapper.MapEnum<ReportQueue>(
                    "public.report_queue",
                    translator);
                NpgsqlConnection.GlobalTypeMapper.MapEnum<ReportStatus>(
                    "public.report_status",
                    translator);
                NpgsqlConnection.GlobalTypeMapper.MapEnum<ReportResolutionAction>(
                    "public.report_resolution_action",
                    translator);
                NpgsqlConnection.GlobalTypeMapper.MapEnum<ReportActorContext>(
                    "public.report_actor_context",
                    translator);
                NpgsqlConnection.GlobalTypeMapper.MapEnum<ModerationPenaltyType>(
                    "public.moderation_penalty_type",
                    translator);
#pragma warning restore CS0618

                _registered = true;
            }
        }

        public static void ReloadConnectionTypes(ElistDataConnection connection)
        {
            var db = connection.OpenDbConnection();
            if (db is NpgsqlConnection npgsql)
                npgsql.ReloadTypes();
        }
    }
}
