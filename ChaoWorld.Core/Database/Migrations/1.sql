create table info
(
    id int primary key not null default 1, -- enforced only equal to 1
    schema_version int,    
    garden_count int not null default 0,
    chao_count bigint not null default 0,
    constraint singleton check (id = 1) -- enforce singleton table/row
);

-- We do an insert here since we *just* added the table
-- Future migrations should do an update at the end
insert into info (schema_version) values (1);