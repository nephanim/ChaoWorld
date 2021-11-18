create table if not exists casino (
	jackpotbalance int not null default 30000
);

update info set schema_version = 11;