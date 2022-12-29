using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using DSharpPlus.SlashCommands;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace BellevueAllianceBot.ReactRole
{
    public class ReactRoleCommands : ApplicationCommandModule
    {
        [SlashCommand("SendReactRoles", "Send the react role messages in the specified channel.")]
        public async Task SendReactRoles(InteractionContext ctx, [Option("channel", "The channel to send the react role messages in.")] DiscordChannel channel)
        {
            if (!Program.Client.CurrentApplication.Owners.Any(user => user.Id == ctx.User.Id)
                && !(ctx.Channel != null && ctx.Channel.PermissionsFor(ctx.Member).HasFlag(DSharpPlus.Permissions.Administrator)))
            {
                Console.WriteLine("Failed check");
                return;
            }

            await ctx.DeferAsync();

            foreach (ReactRoleManager.ReactRoleMessage message in ReactRoleManager._reactRoles)
            {
                if (message.MessageId == 0)
                {
                    continue;
                }

                try
                {
                    await (await channel.GetMessageAsync(message.MessageId)).DeleteAsync();
                    await Task.Delay(1300);
                }
                catch (NotFoundException)
                {

                }
            }

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
