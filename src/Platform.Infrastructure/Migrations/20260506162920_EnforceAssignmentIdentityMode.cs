using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EnforceAssignmentIdentityMode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE OR REPLACE FUNCTION assignment_tenant_guard()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $$
                DECLARE
                    campaign_mode text;
                BEGIN
                    SELECT c.response_identity_mode
                    INTO campaign_mode
                    FROM campaign AS c
                    WHERE c.id = NEW.campaign_id
                      AND c.tenant_id = NEW.tenant_id;

                    IF campaign_mode IS NULL THEN
                        RAISE EXCEPTION 'assignment campaign must be owned by the same tenant';
                    END IF;

                    IF (
                        campaign_mode = 'identified'
                        AND NEW.anonymous = TRUE
                    ) OR (
                        campaign_mode IN ('anonymous', 'anonymous_longitudinal')
                        AND NEW.anonymous = FALSE
                    ) THEN
                        RAISE EXCEPTION 'assignment shape does not match campaign response identity mode';
                    END IF;

                    IF NEW.target_subject_id IS NOT NULL AND NOT EXISTS (
                        SELECT 1
                        FROM subject AS s
                        WHERE s.id = NEW.target_subject_id
                          AND s.tenant_id = NEW.tenant_id
                    ) THEN
                        RAISE EXCEPTION 'assignment target subject must be owned by the same tenant';
                    END IF;

                    IF NEW.respondent_subject_id IS NOT NULL AND NOT EXISTS (
                        SELECT 1
                        FROM subject AS s
                        WHERE s.id = NEW.respondent_subject_id
                          AND s.tenant_id = NEW.tenant_id
                    ) THEN
                        RAISE EXCEPTION 'assignment respondent subject must be owned by the same tenant';
                    END IF;

                    IF NEW.invite_token_id IS NOT NULL AND NOT EXISTS (
                        SELECT 1
                        FROM invitation_token AS token
                        WHERE token.id = NEW.invite_token_id
                          AND token.tenant_id = NEW.tenant_id
                          AND token.campaign_id = NEW.campaign_id
                    ) THEN
                        RAISE EXCEPTION 'assignment invitation token must be owned by the same tenant and campaign';
                    END IF;

                    RETURN NEW;
                END;
                $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE OR REPLACE FUNCTION assignment_tenant_guard()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1
                        FROM campaign AS c
                        WHERE c.id = NEW.campaign_id
                          AND c.tenant_id = NEW.tenant_id
                    ) THEN
                        RAISE EXCEPTION 'assignment campaign must be owned by the same tenant';
                    END IF;

                    IF NEW.target_subject_id IS NOT NULL AND NOT EXISTS (
                        SELECT 1
                        FROM subject AS s
                        WHERE s.id = NEW.target_subject_id
                          AND s.tenant_id = NEW.tenant_id
                    ) THEN
                        RAISE EXCEPTION 'assignment target subject must be owned by the same tenant';
                    END IF;

                    IF NEW.respondent_subject_id IS NOT NULL AND NOT EXISTS (
                        SELECT 1
                        FROM subject AS s
                        WHERE s.id = NEW.respondent_subject_id
                          AND s.tenant_id = NEW.tenant_id
                    ) THEN
                        RAISE EXCEPTION 'assignment respondent subject must be owned by the same tenant';
                    END IF;

                    IF NEW.invite_token_id IS NOT NULL AND NOT EXISTS (
                        SELECT 1
                        FROM invitation_token AS token
                        WHERE token.id = NEW.invite_token_id
                          AND token.tenant_id = NEW.tenant_id
                          AND token.campaign_id = NEW.campaign_id
                    ) THEN
                        RAISE EXCEPTION 'assignment invitation token must be owned by the same tenant and campaign';
                    END IF;

                    RETURN NEW;
                END;
                $$;
                """);
        }
    }
}
