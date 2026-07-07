using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Platform.Infrastructure.Migrations
{
    /// <summary>
    /// Launch snapshots stay immutable for real studies (launched waves are
    /// snapshot-pinned, a domain invariant). Sample studies are rebuildable
    /// fixtures the seeder replaces wholesale (spec updates, locale switch);
    /// their cleanup deletes the whole wave including its snapshot, which the
    /// unconditional trigger blocked. Allow DELETE only when the snapshot
    /// belongs to a sample series. UPDATE stays forbidden for everything.
    /// </summary>
    public partial class AllowSampleLaunchSnapshotCleanup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION campaign_launch_snapshot_prevent_update_delete()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $$
                BEGIN
                    IF TG_OP = 'DELETE' AND EXISTS (
                        SELECT 1
                        FROM campaign_series AS series
                        WHERE series.id = OLD.campaign_series_id
                          AND series.study_kind = 'sample'
                    ) THEN
                        RETURN OLD;
                    END IF;

                    RAISE EXCEPTION 'campaign launch snapshots are immutable';
                END;
                $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION campaign_launch_snapshot_prevent_update_delete()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $$
                BEGIN
                    RAISE EXCEPTION 'campaign launch snapshots are immutable';
                END;
                $$;
                """);
        }
    }
}
