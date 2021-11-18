create table if not exists broadcastchannels (
	general bigint not null,
	races bigint not null,
	tournaments bigint not null,
	expeditions bigint not null
);

update info set schema_version = 10;