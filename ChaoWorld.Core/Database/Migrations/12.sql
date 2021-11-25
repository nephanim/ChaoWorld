create table if not exists expeditions (
	id integer primary key,
	name text not null,
	description text not null,
	minimumchao integer not null default 1,
	maximumchao integer not null default 1,
	prizerings integer not null default 0,
	difficulty integer not null default 1,
	mindurationminutes integer not null default 60,
	maxdurationminutes integer not null default 1440,
    progressrequired integer not null default 0,
	swimrating integer not null default 0,
	flyrating integer not null default 0,
	runrating integer not null default 0,
	powerrating integer not null default 0,
	staminarating integer not null default 0,
	intelligencerating integer not null default 0,
	luckrating integer not null default 0
);

create table if not exists expeditionprerequisites (
	expeditionid integer not null references expeditions (id) on delete cascade,
	prerequisiteid integer not null references expeditions (id) on delete cascade
);

create table if not exists gardenexpeditions (
	gardenid integer not null references gardens (id) on delete cascade,
	expeditionid integer not null references expeditions (id) on delete cascade,
	iscomplete boolean not null default false
);

create table if not exists expeditioninstances (
	id bigserial primary key,
	expeditionid integer not null references expeditions (id) on delete cascade,
    leaderid integer not null references gardens (id) on delete cascade,
	state integer not null default 0,
	createdon timestamp without time zone not null default (current_timestamp),
	expireson timestamp without time zone not null,
	completedon timestamp without time zone,
	timeelapsedseconds integer,
	totalcontribution integer,
	mvpchaoid bigint
);

create table if not exists expeditioninstancechao (
	expeditioninstanceid integer not null references expeditioninstances (id) on delete cascade,
	chaoid bigint not null,
	createdon timestamp without time zone not null default (current_timestamp),
	contribution integer not null default 0,
	finishrank integer
);

update info set schema_version = 12;