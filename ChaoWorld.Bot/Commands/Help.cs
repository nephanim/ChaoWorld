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
                .Field(new("How do I use the bot?", "For a full list of commands, see #resources. If you have any questions, just ask!"))
                .Footer(new($"Created by Noko#9290. Sonic the Hedgehog and Chao are property of Sega and Sonic Team."))
                .Color(DiscordUtils.Blue)
                .Build());
        }

        public async Task HelpAbilities(Context ctx)
        {
            await ctx.Reply(embed: new EmbedBuilder()
                .Title("Chao Development: Abilities")
                .Description("Develop your chao's abilities by participating in races and tournaments, feeding your chao, and other activities that reward ability progress.")
                .Field(new("How do I check my chao's abilities?", "Try using `!chao {name}`, replacing {name} with your chao's name. You will use this often to track your chao's development."))
                .Field(new("What types of abilities are there?", "All chao have the following abilities: Swim, Fly, Run, Power, Stamina, Intelligence, and Luck. \r\n\r\n**•** Swim determines speed in the water and defense in combat.\r\n**•** Fly determines gliding speed and dodging ability.\r\n**•** Run determines running and attack speed.\r\n**•** Power determines climbing speed and attack power.\r\n**•** Stamina determines how long your chao can last in a race and how much damage they can take in a fight.\r\n**•** Intelligence determines how long your chao takes to solve puzzles, but has some influence in many areas like path efficiency in races and recovery after a knockdown.\r\n**•** Luck determines whether your chao falls prey to traps, their likelihood of tripping in a race, and influences other areas like dodging."))
                .Field(new("What do all these letters and numbers next to the abilities mean?", "You will often see abilities represented in this format:\r\n\r\nAbility (Lv.##)\r\nLetter Grade • ##/100 (####)\r\n\r\nThe level next to the ability name ranges from 0 to 99. Each time the ability's level increases, your chao will improve in that ability.\r\n\r\nThe letter grade shown for an ability represents how talented the chao is in that area. Higher letter grades mean your chao will learn faster, resulting in higher total stats at level 99.\r\n\r\nProgress toward the next level (##/100) is shown next to the stat grade. Progress is obtained whenever the ability is used. Reaching 100 progress causes the ability level to increase.\r\n\r\nThe four-digit number shown in parentheses is the raw stat value for that ability. This is the only number that influences performance in a race. The higher the stat grade, the more points the chao will receive when the ability levels up."))
                .Build());
        }

        public async Task HelpEvolution(Context ctx)
        {
            await ctx.Reply(embed: new EmbedBuilder()
                .Title("Chao Development: Evolution")
                .Description("Over time, your chao will take on different forms via evolution. By understanding the evolution process, you can use it to your chao's advantage.")
                .Field(new("When will my chao evolve?", "Chao years are measured in real-time weeks. Your chao will remain a child for the first year (1 week) before their first evolution occurs. A second evolution begins around age four."))
                .Field(new("What does the first evolution do?", "The chao will become an adult. It will have one of three alignments - hero, dark, or neutral. It will also have an ability type - swim, fly, run, power, or normal. The alignment and ability type are determined based on fruits the chao has eaten. Both of these influence your chao's evolved appearance.\r\n\r\nFeeding your chao ability type fruit (e.g. Fly Fruit) shifts their affinity toward that type. When the first evolution occurs, the letter grade for the chosen ability type will increase by one rank, up to a maximum of S. For example, if your chao has eaten several Fly Fruits, they will evolve into a Fly type and their Fly ability will improve by one rank (e.g. E -> D).\r\n\r\nThere are no intelligence or luck-based evolution types, so these abilities cannot be improved through evolution."))
                .Field(new("What does the second evolution do?", "The second evolution does not affect ability grades, but provides a more unique appearance. The second evolution is more gradual, and it is possible for the type to change over time."))
                .Build());
        }

        public async Task HelpReincarnation(Context ctx)
        {
            await ctx.Reply(embed: new EmbedBuilder()
                .Title("Chao Development: Reincarnation")
                .Description("Chao are known to reincarnate like phoenixes. This allows them to cultivate power and chaos energy over many lifetimes. Some chao have even become immortal.")
                .Field(new("When will my chao reincarnate?", "This will not happen automatically, no matter how old your chao gets. Give your chao a Suspicious Potion to induce reincarnation. This is sometimes available for purchase from the Black Market."))
                .Field(new("What happens when my chao reincarnates?", "Your chao will revert to a newborn, but they will retain the following:\r\n\r\n**•** All current ability grades\r\n**•** A portion of their current stat values (10% initially)\r\n\r\nAbility levels will be reset to level 1, allowing your chao to level up again and improve their stats. As children, they can undergo first evolution again in order to improve the letter grade for an ability. Plan which fruits to feed your chao in order to get the most out of each life. Aiming to increase every ability to rank S by repeating evolution and reincarnation is a common goal."))
                .Field(new("What is 'chaos factor'?", "During reincarnation, a percentage of your chao's stats will be retained. This is based on levels of chaos energy carried within the chao's body. Initially, chao can retain 10% of their stats during reincarnation. This can be increased by giving your chao Chaos Juice, which is sometimes available for purchase from the Black Market."))
                .Build());
        }
    }
}