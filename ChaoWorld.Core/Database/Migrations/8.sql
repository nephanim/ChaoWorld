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
	(1, 'Duel', 'Honorable duel between two chao for bragging rights', 1, 1, false, 2, 2, 30),
	(2, 'Small Tournament', 'Small-scale tournament with two rounds', 1, 1, true, 1, 4, 100),
	(3, 'Medium Tournament', 'Typical-scale tournament with three rounds', 3, 3, true, 1, 8, 200),
	(4, 'Large Tournament', 'Large-scale tournament with four rounds', 5, 5, true, 1, 16, 300),
	(5, 'World Championship', 'Massive-scale tournament open to everyone', 10, 10, false, 1, 32, 400);

create table if not exists tournamentinstances
(
	id bigserial primary key,
    tournamentid integer not null references tournaments (id) on delete cascade,
    state integer not null default 0,
    createdon timestamp without time zone not null default (current_timestamp),
    readyon timestamp without time zone,
    completedon timestamp without time zone,
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