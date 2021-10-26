create table if not exists races
(
	id serial primary key,
    name text not null,
    description text not null,
    availableon timestamp without time zone not null default (current_timestamp),
    frequencyminutes integer not null,
    readydelayminutes integer not null default 5,
    isenabled boolean not null default false,
    minimumchao integer not null default 1,
    maximumchao integer not null default 8
);

create table if not exists racesegments
(
    id serial primary key,
    raceid serial not null references races (id) on delete cascade,
    description text not null,
    terraintype integer not null default 0,
    startelevation integer not null default 0,
    endelevation integer not null default 0,
    terraindistance integer not null,
    staminalossmultiplier double precision not null default 1.0,
    swimrating integer not null default 0,
    flyrating integer not null default 0,
    runrating integer not null default 0,
    powerrating integer not null default 0,
    intelligencerating integer not null default 0,
    luckrating integer not null default 0
);

create table if not exists raceinstances
(
	id bigserial primary key,
    raceid serial not null references races (id) on delete cascade,
    state integer not null default 0,
    createdon timestamp without time zone not null default (current_timestamp),
    readyon timestamp without time zone,
    completeon timestamp without time zone,
    winnerchaoid bigint,
    timeelapsedseconds integer,
    prizerings integer not null default 0
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
    racesegmentid serial not null references racesegments (id) on delete cascade,
    chaoid bigint not null,
    state integer not null default 0,
    segmenttimeseconds integer,
    totaltimeseconds integer,
    remainingstamina integer,
    endelevation integer
);

update info set schema_version = 3;