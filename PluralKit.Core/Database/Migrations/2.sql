create table shards (
    id int not null primary key,
    
    -- 0 = down, 1 = up
    status smallint not null default 0,
    
    ping float,
    last_heartbeat timestamptz,
    last_connection timestamptz
);

update info set schema_version = 2;