create table if not exists races
(
	id integer primary key,
    name text not null,
    description text not null,
    availableon timestamp without time zone not null default (current_timestamp),
    frequencyminutes integer not null,
    readydelayminutes integer not null default 5,
    isenabled boolean not null default false,
    minimumchao integer not null default 1,
    maximumchao integer not null default 8,
    prizerings integer not null default 0,
    difficulty integer,
    swimpercentage double precision,
    flypercentage double precision,
    runpercentage double precision,
    powerpercentage double precision,
    intelligencepercentage double precision,
    luckpercentage double precision
);

create table if not exists racesegments
(
    id integer primary key,
    raceid integer not null references races (id) on delete cascade,
    raceindex int not null,
    description text not null,
    terraintype integer not null default 0,
    startelevation integer not null default 0,
    endelevation integer not null default 0,
    terraindistance integer not null,
    staminalossmultiplier double precision not null default 1.0,
    terraindifficulty integer not null default 0,
    intelligencerating integer not null default 0,
    luckrating integer not null default 0
);

create table if not exists raceinstances
(
	id bigserial primary key,
    raceid integer not null references races (id) on delete cascade,
    state integer not null default 0,
    createdon timestamp without time zone not null default (current_timestamp),
    readyon timestamp without time zone,
    completedon timestamp without time zone,
    winnerchaoid bigint,
    timeelapsedseconds integer
);

create table if not exists raceinstancebans
(
	raceinstanceid serial not null references raceinstances (id) on delete cascade,
	gardenid serial not null references gardens (id) on delete cascade,
	expireson timestamp without time zone not null default (current_timestamp + interval '10 minutes')
);

create table if not exists raceinstancechao
(
    raceinstanceid bigserial not null references raceinstances (id) on delete cascade,
    chaoid bigint not null,
    state integer not null default 0,
    totaltimeseconds integer,
    finishposition integer
);

create table if not exists raceinstancechaosegments
(
    raceinstanceid bigserial not null references raceinstances (id) on delete cascade,
    racesegmentid integer not null references racesegments (id) on delete cascade,
    chaoid bigint not null,
    state integer not null default 0,
    segmenttimeseconds integer,
    totaltimeseconds integer,
    startstamina integer,
    endstamina integer,
    startelevation integer,
    endelevation integer
);

update info set schema_version = 3;