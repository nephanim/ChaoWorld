using System.Threading.Tasks;

using Myriad.Builders;

namespace ChaoWorld.Bot
{
    public class Help
    {
        public async Task HelpRoot(Context ctx)
        {
            await ctx.Reply(embed: new EmbedBuilder()
                .Title("Chao World")
                .Description("Chao World is a Discord bot inspired by chao raising minigames from Sega's *Sonic the Hedgehog* series. You can raise your own chao, race them, fight in karate tournaments, and more!")
                .Field(new("What's a chao?", "Chao were introduced in Sega's 1998 Dreamcast game *Sonic Adventure*. They have featured in several Sonic titles and made cameos in other franchises since."))
                .Field(new("How do I get one?", "Try using the following commands:\n**•** `!garden new` - Create a garden (if you haven't already)\n**•** `!garden list` - See a list of chao in your garden\n**•** `!chao Unnamed name {name}` - Pick a name for your chao (you can always change it later)\n**•** `!chao {name}` - See details about your new chao\n**•** `!garden default {chao name}` - Set this chao as your default for races and items"))
                .Field(new("Can I get more chao?", "While the starter chao comes with your garden, you'll have to purchase eggs from the Black Market with rings to get additional chao. Use `!collect` to gather rings once per day. You will also earn rings by participating in races and tournaments."))
                .Field(new("How do I use the bot?", "For a full list of commands, see [the command list](https://bytebarcafe.com/chao/commands.php). If you have any questions, just ask!"))
                .Footer(new($"Created by Noko for Byte Bar & Cafe. Sonic the Hedgehog and Chao are property of Sega and Sonic Team."))
                .Color(DiscordUtils.Blue)
                .Build());
        }
    }
}