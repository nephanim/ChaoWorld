create table if not exists gardens
(
	id serial primary key,
    ringbalance bigint not null default 0,
    createdon timestamp without time zone not null default (current_timestamp)
);

create table if not exists accounts
(
    uid bigint primary key,
    gardenid serial not null references gardens (id) on delete cascade
);

create table if not exists chao
(
	id bigserial primary key,
    gardenid serial not null references gardens (id) on delete cascade,
    name text not null,
    isactive boolean not null default false,
    isdeleted boolean not null default false,
    createdon timestamp without time zone not null default (current_timestamp),
    deletedon timestamp without time zone,
    primarycolor integer not null default 0,
    secondarycolor integer,
    isshiny boolean not null default false,
    istwotone boolean not null default false,
    age integer not null default 0,
    reincarnations integer not null default 0,
    evolutionstate integer not null default 0,
    alignment integer not null default 0,
    alignmentvalue integer not null default 0,
    evolutionprogress integer not null default 0,
    firstevolutiontype integer,
    secondevolutiontype integer,
    flyswimaffinity integer not null default 0,
    runpoweraffinity integer not null default 0,
    swimgrade integer not null default 0,
    swimlevel integer not null default 0,
    swimprogress integer not null default 0,
    swimvalue integer not null default 0,
    flygrade integer not null default 0,
    flylevel integer not null default 0,
    flyprogress integer not null default 0,
    flyvalue integer not null default 0,
    rungrade integer not null default 0,
    runlevel integer not null default 0,
    runprogress integer not null default 0,
    runvalue integer not null default 0,
    powergrade integer not null default 0,
    powerlevel integer not null default 0,
    powerprogress integer not null default 0,
    powervalue integer not null default 0,
    staminagrade integer not null default 0,
    staminalevel integer not null default 0,
    staminaprogress integer not null default 0,
    staminavalue integer not null default 0,
    intelligencegrade integer not null default 0,
    intelligencelevel integer not null default 0,
    intelligenceprogress integer not null default 0,
    intelligencevalue integer not null default 0,
    luckgrade integer not null default 0,
    lucklevel integer not null default 0,
    luckprogress integer not null default 0,
    luckvalue integer not null default 0
);