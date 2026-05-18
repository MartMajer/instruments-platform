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
