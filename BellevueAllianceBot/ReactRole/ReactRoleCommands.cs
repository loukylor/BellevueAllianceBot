using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using System.Threading.Tasks;

namespace BellevueAllianceBot.ReactRole
{
    public class ReactRoleCommands : ApplicationCommandModule
    {
        [SlashCommand("SendReactRoles", "Send the react role messages in the specified channel.")]
        [SlashRequireUserPermissions(DSharpPlus.Permissions.Administrator)]
        public async Task SendReactRoles(InteractionContext ctx, [Option("channel", "The channel to send the react role messages in.")] DiscordChannel channel)
        {
            await ctx.DeferAsync();

            foreach (ReactRoleManager.ReactRoleMessage message in ReactRoleManager._reactRoles)
            {
                DiscordMessage createdMessage = await channel.SendMessageAsync(message.ToMessage());
                message.MessageId = createdMessage.Id;


                // Let's not piss of discord with way too many requests
                await Task.Delay(1300);
            }
            ReactRoleManager.Save();

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Finished!"));
        }
    }
}
