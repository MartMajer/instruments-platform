INSERT INTO tenant (id, slug, name, region, default_locale, status, created_at, updated_at)
VALUES (
    '11111111-1111-4111-8111-111111111111',
    'local-dev',
    'Local Development Tenant',
    'eu',
    'en',
    'active',
    now(),
    now()
)
ON CONFLICT (id) DO NOTHING;

INSERT INTO user_account (id, tenant_id, email, locale, failed_login_attempts, created_at, updated_at)
VALUES (
    '22222222-2222-4222-8222-222222222222',
    '11111111-1111-4111-8111-111111111111',
    'dev@local.test',
    'en',
    0,
    now(),
    now()
)
ON CONFLICT (id) DO NOTHING;
