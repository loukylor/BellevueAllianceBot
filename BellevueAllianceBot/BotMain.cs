using BellevueAllianceBot.Managers;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.EventArgs;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
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

        private static readonly Regex _imRegex = new(@"(?i:\bI'?\s*a?m\b)");
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

            Client.MessageCreated += async (DiscordClient _, MessageCreateEventArgs ev) =>
            {
                if (ev.Guild != null || ev.Channel.Id != 569147890413469718 ||  ev.Author.IsBot)
                {
                    return;
                }

                Match match = _imRegex.Match(ev.Message.Content);
                if (!match.Success)
                {
                    return;
                }

                DiscordMessageBuilder reply = new DiscordMessageBuilder().WithContent($"Hi {ev.Message.Content[(match.Index + match.Length)..].Trim()} I'm dad.");
                reply.WithAllowedMentions(new List<IMention>());
                await ev.Message.RespondAsync(reply);
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
