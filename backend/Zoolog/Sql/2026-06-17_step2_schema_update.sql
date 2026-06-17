IF COL_LENGTH('collection', 'is_public') IS NULL
BEGIN
    ALTER TABLE [collection]
    ADD [is_public] bit NOT NULL
    CONSTRAINT DF_collection_is_public DEFAULT 0;
END;

IF COL_LENGTH('specimen', 'size') IS NULL
BEGIN
    ALTER TABLE [specimen]
    ADD [size] varchar(100) NULL;
END;

IF COL_LENGTH('specimen', 'photo_path') IS NULL
BEGIN
    ALTER TABLE [specimen]
    ADD [photo_path] varchar(500) NULL;
END;

IF COL_LENGTH('users', 'status') IS NULL
BEGIN
    ALTER TABLE [users]
    ADD [status] varchar(20) NOT NULL
    CONSTRAINT DF_users_status DEFAULT 'active';
END;
