﻿using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
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

            // Check if the messages are already in the channel
            bool alreadySent = (await channel.GetMessageAsync(ReactRoleManager._reactRoles[0].MessageId)) != null;

            if (!alreadySent)
            {
                foreach (ReactRoleManager.ReactRoleMessage message in ReactRoleManager._reactRoles)
                {
                    DiscordMessage createdMessage = await channel.SendMessageAsync(message.ToMessage());
                    message.MessageId = createdMessage.Id;


                    // Let's not piss of discord with way too many requests
                    await Task.Delay(1300);
                }
                ReactRoleManager.Save();
            }
            else
            {
                foreach (ReactRoleManager.ReactRoleMessage message in ReactRoleManager._reactRoles)
                {
                    DiscordMessage updatedMessage = await channel.GetMessageAsync(message.MessageId);
                    await updatedMessage.ModifyAsync(message.ToMessage());

                    // Let's not piss of discord with way too many requests
                    await Task.Delay(2000);
                }
            }

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Finished!"));
        }
    }
}
