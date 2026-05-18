INSERT INTO tenant (id, slug, name, region, default_locale, status, created_at, updated_at)
VALUES
    (
        '33333333-3333-4333-8333-333333333333',
        'validation-oh-research',
        'Validation Demo - Occupational Health Research',
        'eu',
        'en',
        'active',
        now(),
        now()
    ),
    (
        '44444444-4444-4444-8444-444444444444',
        'validation-se-education',
        'Validation Demo - Student Experience Research',
        'eu',
        'en',
        'active',
        now(),
        now()
    ),
    (
        '55555555-5555-4555-8555-555555555555',
        'validation-osh-consulting',
        'Validation Demo - Work Safety Consulting',
        'eu',
        'en',
        'active',
        now(),
        now()
    )
ON CONFLICT (id) DO UPDATE
SET
    slug = EXCLUDED.slug,
    name = EXCLUDED.name,
    region = EXCLUDED.region,
    default_locale = EXCLUDED.default_locale,
    status = EXCLUDED.status,
    updated_at = now();
