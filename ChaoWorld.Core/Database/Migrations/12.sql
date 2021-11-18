create table if not exists trees (
	id bigserial primary key,
	gardenid serial not null references gardens (id) on delete cascade,
    name text not null,
	fruittypeid serial not null references itemtypes (typeid) on delete cascade,
	createdon timestamp without time zone not null default (current_timestamp),
	fruitquantity integer not null default 0,
	health integer not null default 0,
    nextwatering timestamp without time zone not null default (current_timestamp)
);

update info set schema_version = 12;