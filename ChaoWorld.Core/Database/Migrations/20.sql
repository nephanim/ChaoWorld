ALTER TABLE chao
ADD COLUMN swimfactor double not null default 0,
ADD COLUMN flyfactor double not null default 0,
ADD COLUMN runfactor double not null default 0,
ADD COLUMN powerfactor double not null default 0,
ADD COLUMN staminafactor double not null default 0,
ADD COLUMN intelligencefactor double not null default 0,
ADD COLUMN luckfactor double not null default 0
);