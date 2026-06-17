DECLARE @UserId int;
DECLARE @TaxonomyId int;
DECLARE @LocationId int;
DECLARE @CollectionId int;

SELECT @UserId = id FROM [users] WHERE username = 'demo_user';

IF @UserId IS NULL
BEGIN
    THROW 50001, 'demo_user does not exist. Create demo users through the API first so passwords are BCrypt-hashed.', 1;
END;

IF NOT EXISTS (SELECT 1 FROM [taxonomy] WHERE species = 'Danaus plexippus')
BEGIN
    INSERT INTO [taxonomy] (kingdom, phylum, [class], orders, family, genus, species)
    VALUES ('Animalia', 'Arthropoda', 'Insecta', 'Lepidoptera', 'Nymphalidae', 'Danaus', 'Danaus plexippus');
END;

SELECT @TaxonomyId = id FROM [taxonomy] WHERE species = 'Danaus plexippus';

IF NOT EXISTS (SELECT 1 FROM [location] WHERE name = 'Siegen Test Location')
BEGIN
    INSERT INTO [location] (name, latitude, longitude, country, region)
    VALUES ('Siegen Test Location', 50.8833, 8.0167, 'Germany', 'NRW');
END;

SELECT @LocationId = id FROM [location] WHERE name = 'Siegen Test Location';

IF NOT EXISTS (SELECT 1 FROM [collection] WHERE name = 'Demo Public Collection')
BEGIN
    INSERT INTO [collection] (name, description, created_by, is_public)
    VALUES ('Demo Public Collection', 'Seed collection for backend and frontend testing.', @UserId, 1);
END;

SELECT @CollectionId = id FROM [collection] WHERE name = 'Demo Public Collection';

IF NOT EXISTS (SELECT 1 FROM [specimen] WHERE name = 'Demo Monarch Butterfly')
BEGIN
    INSERT INTO [specimen] (
        name,
        description,
        date_collected,
        status,
        location_id,
        taxonomy_id,
        collection_id,
        added_by,
        size,
        photo_path
    )
    VALUES (
        'Demo Monarch Butterfly',
        'Seed specimen for testing the Zoolog API.',
        '2026-06-17',
        'available',
        @LocationId,
        @TaxonomyId,
        @CollectionId,
        @UserId,
        'medium',
        NULL
    );
END;
