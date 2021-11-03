create table if not exists items
(
    id bigserial primary key,
    gardenid serial not null references gardens (id) on delete cascade,
    categoryid integer not null,
    typeid integer not null,
    quantity integer not null,
    createdon timestamp without time zone not null default (current_timestamp)
);

create table if not exists market
(
    nextrefreshon timestamp without time zone not null default (current_timestamp)
);

create table if not exists marketitems
(
    categoryid integer not null,
    typeid integer not null,
    quantity integer not null,
    price integer not null
);

update info set schema_version = 6;