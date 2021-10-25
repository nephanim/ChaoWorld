create function message_context(account_id bigint, guild_id bigint, channel_id bigint)
    returns table (
        gardenid int
    )
as $$
select accounts.gardenid
from (select 1) as _placeholder
left join accounts on accounts.uid = account_id $$ language sql stable rows 1;