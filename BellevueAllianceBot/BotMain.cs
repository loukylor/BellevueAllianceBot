using BellevueAllianceBot.Managers;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.EventArgs;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace BellevueAllianceBot
{
    public class BotMain
    {
        public static DiscordClient Client { get; } = new(new DiscordConfiguration()
        {
            Token = Config.Instance.Token,
            Intents = DiscordIntents.AllUnprivileged
        });

        public static SlashCommandsExtension SlashCommands { get; } = Client.UseSlashCommands();

        public static DiscordGuild? BAGuild { get; private set; }

        public static void Main(string[] args)
        {
            foreach (Type type in Assembly.GetExecutingAssembly().GetTypes())
            {
                if (typeof(ApplicationCommandModule).IsAssignableFrom(type))
                {
                    SlashCommands.RegisterCommands(type);
                }
            }

            Client.Ready += async (DiscordClient _, ReadyEventArgs _) =>
            {
                // init all the managers
                foreach (Type type in Assembly.GetExecutingAssembly().GetTypes())
                {
                    if (type != typeof(IManager) && typeof(IManager).IsAssignableFrom(type))
                    {
                        type.GetMethod(nameof(IManager.OnBotInit))!.Invoke(Activator.CreateInstance(type), null);
                    }
                }

                BAGuild = await Client.GetGuildAsync(Config.Instance.BADiscordID);
            };

            SlashCommands.SlashCommandErrored += (SlashCommandsExtension _, SlashCommandErrorEventArgs e) =>
            {
                Console.Error.WriteLine(e.Exception);
                return Task.CompletedTask;
            };

            Start().ConfigureAwait(false).GetAwaiter().GetResult();
        }

        private static async Task Start()
        {
            await Client.ConnectAsync();

            await Task.Delay(-1);
        }
    }
}
