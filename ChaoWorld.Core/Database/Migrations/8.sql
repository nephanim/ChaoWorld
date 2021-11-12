create table if not exists tournaments
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
    prizerings integer not null default 0
);

insert into tournaments
	(id, name, description, frequencyminutes, readydelayminutes, isenabled, minimumchao, maximumchao, prizerings)
values
	(1, 'Practice', 'Honorable duel between two chao for bragging rights', 1, 1, false, 2, 2, 30),
	(2, 'Sprout', 'Small-scale tournament with two rounds', 2, 2, true, 1, 4, 100),
	(3, 'Disciple', 'Typical-scale tournament with three rounds', 3, 3, true, 1, 8, 200),
	(4, 'Master', 'Large-scale tournament with four rounds', 4, 4, true, 1, 16, 300),
	(5, 'Grandmaster', 'Massive-scale tournament with five rounds', 5, 5, true, 1, 32, 400);

create table if not exists tournamentinstances
(
	id bigserial primary key,
    tournamentid integer not null references tournaments (id) on delete cascade,
    state integer not null default 0,
    createdon timestamp without time zone not null default (current_timestamp),
    readyon timestamp without time zone,
    completedon timestamp without time zone,
    totaltimeelapsedseconds int,
    winnerchaoid bigint
);

create table if not exists tournamentinstancechao
(
    tournamentinstanceid bigserial not null references tournamentinstances (id) on delete cascade,
    chaoid bigint not null,
    state integer not null default 0,
    iswinner bool,
    highestround integer
);

create table if not exists tournamentinstancematches
(
    tournamentinstanceid bigserial not null references tournamentinstances (id) on delete cascade,
    state integer not null default 0,
    resulttype integer,
    roundnumber integer not null,
	roundorder integer not null,
	leftchaoid bigint not null,
	rightchaoid bigint not null,
	winnerchaoid bigint,
	elapsedtimeseconds integer
);

update info set schema_version = 8;