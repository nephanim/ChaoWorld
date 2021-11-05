create extension if not exists pg_trgm;
create extension if not exists fuzzystrmatch;

update info set schema_version = 7;