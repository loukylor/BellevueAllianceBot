using BellevueAllianceBot.Managers;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace BellevueAllianceBot.ReactRole
{
    public class ReactRoleManager : IManager
    {
        internal const string ReactRoleJsonPath = "ReactRole.json";
        internal static List<ReactRoleMessage> _reactRoles = JsonConvert.DeserializeObject<List<ReactRoleMessage>>(File.ReadAllText(ReactRoleJsonPath))!;
        
        public void OnBotInit()
        {
            Program.Client.MessageReactionAdded+= HandleReaction;
            Program.Client.MessageReactionRemoved += HandleReactionRemove;
            Program.Client.ComponentInteractionCreated += HandleInteractionCreated;
        }

        private static Task HandleReaction(DiscordClient _, MessageReactionAddEventArgs e)
            => HandleReactionInternal(e.User, e.Guild, e.Message, e.Emoji, true);

        private static Task HandleReactionRemove(DiscordClient _, MessageReactionRemoveEventArgs e)
            => HandleReactionInternal(e.User, e.Guild, e.Message, e.Emoji, false);

        private static async Task HandleReactionInternal(
            DiscordUser user, 
            DiscordGuild guild,
            DiscordMessage message, 
            DiscordEmoji emoji,
            bool adding)
        {
            if (message.Author != Program.Client.CurrentUser)
                return;

            if (user.IsBot)
                return;

            ReactRole? reactRole = GetReactRole(message, emoji);
            if (reactRole == null)
                return;

            DiscordRole role = guild.GetRole(reactRole.RoleId);

            DiscordMember member = await guild.GetMemberAsync(user.Id);

            // Check if they currently have the role
            bool hasRole = member.Roles.Contains(role);
            if (adding)
            {
                // If we are adding a reaction, then we only do something if they don't have it
                if (!hasRole)
                {
                    await member.GrantRoleAsync(role, "React role added");
                }
            }
            else
            {
                // if we are removing a reaction, then we only do something if they do have it
                if (hasRole)
                {
                    await member.RevokeRoleAsync(role, "React role removed");
                }
            }
        }

        private static async Task HandleInteractionCreated(DiscordClient _, ComponentInteractionCreateEventArgs e)
        {
            e.Handled = true;
            await e.Interaction.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder().AsEphemeral(true));

            ReactRoleMessage reactRoleMessage = GetReactRoleMessage(e.Interaction.Data.CustomId)!;
            DiscordMember member = await e.Guild.GetMemberAsync(e.User.Id);

            // If it's a year then we have to handle role assigment differently
            if (reactRoleMessage.Years)
            {
                string year = e.Values[0];

                // Check if this year role exists
                DiscordRole? role = e.Guild.Roles.Values.FirstOrDefault(guildRole => guildRole.Name == year);

                // If it doesn't
                if (role == null)
                {
                    role = await e.Guild.CreateRoleAsync(year, Permissions.None);
                }

                // If it does then add/remove it
                await AddOrRemoveRole(member, role);
            }
            else
            {
                HashSet<ulong> reactRoles = reactRoleMessage.ReactRoles!.Select(reactRole => reactRole.RoleId).ToHashSet();

                // Start off with a list that has all the roles the user selected
                List<DiscordRole> newRoles = e.Values.Select(value => e.Guild.GetRole(ulong.Parse(value))).ToList();
                foreach (DiscordRole role in member.Roles)
                {
                    // If they didn't specfically click it, then check if it was in the react role prompt
                    if (!reactRoles.Contains(role.Id))
                    {
                        // Only if it wasn't in the prompt should we add it
                        newRoles.Add(role);
                    }
                }

                await member.ReplaceRolesAsync(newRoles);
            }

            await e.Interaction.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder()
                .AsEphemeral(true)
                .WithContent("Roles Updated!"));
        }

        private static ReactRoleMessage? GetReactRoleMessage(string id)
        {
            return _reactRoles.FirstOrDefault(reactRoleMessage => reactRoleMessage.Id == id);
        }

        private static ReactRole? GetReactRole(DiscordMessage message, DiscordEmoji emoji)
        {
            string emojiString = emoji.GetDiscordName();
            return _reactRoles.First(reactRoleMessage => reactRoleMessage.MessageId == message.Id)
                       .ReactRoles!.FirstOrDefault(reactRole => reactRole.Emoji == emojiString);
        }

        private static async Task AddOrRemoveRole(DiscordMember member, DiscordRole role)
        {
            // Check if they currently have the role
            // If they do, remove, if they dont, add
            if (!member.Roles.Any(anyRole => role == anyRole))
            {
                await member.GrantRoleAsync(role, "React role added");
            }
            else
            {
                await member.RevokeRoleAsync(role, "React role removed");
            }
        }

        public static void Save()
        {
            File.WriteAllText(ReactRoleJsonPath, JsonConvert.SerializeObject(_reactRoles, Formatting.Indented));
        }

        public class ReactRoleMessage
        {
            public ReactRoleMessage() { }

            [JsonProperty("message_id")]
            public ulong MessageId { get; set; }
            
            [JsonProperty("embed")]
            public DiscordEmbed? DiscordEmbed { get; set; }

            [JsonProperty("id")]
            public string? Id { get; set; }
            
            [JsonProperty("react_roles")]
            public IReadOnlyList<ReactRole>? ReactRoles { get; set; }

            [JsonProperty("max")]
            public int Max { get; set; } = 25;

            [JsonProperty("min")]
            public int Min { get; set; } = 1;

            [JsonProperty("years")]
            public bool Years { get; set; } = false;

            public DiscordMessageBuilder ToMessage()
            {
                DiscordMessageBuilder builder = new();

                builder.AddEmbed(DiscordEmbed);

                IEnumerable<DiscordSelectComponentOption>? options;
                int optionsLength;
                if (Years)
                {
                    // If we want to do years then enumerate from 2006-2030 and add those as options
                    List<DiscordSelectComponentOption> optionsAsList = new ();
                    for (int i = 1; i < 26; i++)
                    {
                        string role = $"{2005 + i}";
                        optionsAsList.Add(new DiscordSelectComponentOption(
                            role,
                            role
                        ));
                    }
                    options = optionsAsList;
                    optionsLength = 25;
                }
                else
                {
                    options = ReactRoles!.Select(reactRole => reactRole.ToOption());
                    optionsLength = ReactRoles!.Count;
                }

                DiscordSelectComponent messageDropdown = new(
                    Id,
                    "",
                    options,
                    minOptions: Min,
                    // Discord is dumb and Max options cant be bigger than options length
                    maxOptions: Max > optionsLength ? optionsLength : Max
                );

                builder.AddComponents(messageDropdown);

                return builder;
            }
        }

        public class ReactRole
        {
            public ReactRole() { }

            [JsonProperty("emoji")]
            public string? Emoji { get; set; }

            [JsonProperty("role_id")]
            public ulong RoleId { get; set; }

            public DiscordSelectComponentOption ToOption()
            {
                DiscordRole? role = Program.BAGuild?.GetRole(RoleId);
                string name = role == null ? "error lol oops" : role.Name;
                DiscordComponentEmoji emoji = new(DiscordEmoji.FromName(Program.Client, Emoji));

                return new DiscordSelectComponentOption(name, RoleId.ToString(), emoji: emoji);
            }
        }
    }
}
