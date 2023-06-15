using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Discord.Interactions;
using Kimi.Logging;
using Kimi.Services.Core;
using IResult = Discord.Interactions.IResult;
using Kimi.Commands.Modules.Milkshake;
using Kimi.Services.CRUD;
using Kimi.Services.Milkshake;
using Kimi.Services.Models;
using Milkshake.Crud;
using Milkshake.Models;
using Discord;
using System.Reactive;
using Microsoft.EntityFrameworkCore;

namespace Kimi.Commands
{
    public class CommandHandler
    {
        public ulong[]? GuildId { get; init; }
        private readonly string[] _prefix;
        private readonly DiscordSocketClient _client;
        private readonly CommandService _commands;
        private readonly IConfigurationRoot _config;
        private readonly IServiceProvider _services;
        private readonly InteractionService _slash;

        public CommandHandler(Settings settings, DiscordSocketClient client,
            CommandService commands, InteractionService slash, IConfigurationRoot config,
            IServiceProvider services)
        {
            GuildId = settings.General.DebugGuildId;
            _prefix = settings.General.Prefix;
            _client = client;
            _commands = commands;
            _slash = slash;
            _config = config;
            _services = services;
        }

        public async Task InitializePrefixAsync()
        {

            await _commands.AddModulesAsync(Assembly.GetExecutingAssembly(), _services);
            _client.MessageReceived += HandleCommandAsync;
        }

        private async Task HandleCommandAsync(SocketMessage messageParam)
        {
            var message = messageParam as SocketUserMessage;
            if (message == null || message.Author.IsBot) return;

            int argPos = 0;

            bool hasPrefix = _prefix.Any(prefix => message.HasStringPrefix(prefix, ref argPos));

            if (!hasPrefix && !message.HasMentionPrefix(_client.CurrentUser, ref argPos))
                return;

            var context = new SocketCommandContext(_client, message);

            await _commands.ExecuteAsync(
                context: context,
                argPos: argPos,
                services: _services);
        }

        public async Task InitializeSlashAsync()
        {
            await _slash.AddModulesAsync(Assembly.GetExecutingAssembly(), _services);

            
            _client.InteractionCreated += HandleInteractionAsync;
            _slash.SlashCommandExecuted += SlashCommandExecuted;
            _slash.ModalCommandExecuted += ModalCommandExecuted;
            _slash.AutocompleteHandlerExecuted += AutocompleteExecuted;
            _slash.ComponentCommandExecuted += ComponentExecuted;
            _client.SelectMenuExecuted += SelectMenuExecuted;
        }

        private async Task HandleInteractionAsync(SocketInteraction arg)
        {
            try
            {
                var ctx = new SocketInteractionContext(_client, arg);
                await _slash.ExecuteCommandAsync(ctx, _services);
            }
            catch (Exception ex)
            {
                await Log.Write(ex.ToString());
            }
        }

        private async Task SlashCommandExecuted(SlashCommandInfo slash, Discord.IInteractionContext context, IResult result)
        {
            if (!result.IsSuccess)
                switch (result.Error)
                {
                    case InteractionCommandError.UnmetPrecondition:
                        await context.Interaction.RespondAsync($"Unmet Precondition: {result.ErrorReason}");
                        break;
                    case InteractionCommandError.UnknownCommand:
                        await context.Interaction.RespondAsync("Unknown command");
                        break;
                    case InteractionCommandError.ConvertFailed:
                        await context.Interaction.RespondAsync($"Convert Failed: {result.ErrorReason}");
                        break;
                    case InteractionCommandError.ParseFailed:
                        await context.Interaction.RespondAsync($"Parse Failed {result.ErrorReason}");
                        break;
                    case InteractionCommandError.BadArgs:
                        await context.Interaction.RespondAsync("Invalid arguments");
                        break;
                    case InteractionCommandError.Exception:
                        await context.Channel.SendMessageAsync("Sorry, something went wrong.");
                        break;
                    default:
                        await context.Interaction.RespondAsync("Command could not be executed.");
                        break;
                }
        }

        private async Task ModalCommandExecuted(ModalCommandInfo modal, Discord.IInteractionContext context, IResult result)
        {
            await Task.CompletedTask;
        }

        private async Task AutocompleteExecuted(IAutocompleteHandler autocomplete, Discord.IInteractionContext context, IResult result)
        {
            await Task.CompletedTask;
        }

        private async Task ComponentExecuted(ComponentCommandInfo component, Discord.IInteractionContext context, IResult result)
        {
            await Task.CompletedTask;
        }

        private async Task SelectMenuExecuted(SocketMessageComponent message)
        {
            await Task.CompletedTask;
        }

    }

    public class InstanceModuleHandler
    {
        public ulong[]? GuildId { get; init; }
        private readonly DiscordSocketClient _client;
        private readonly InteractionService _slash;
        private readonly ApplicationDbContext _context;
        private readonly ICrud<MilkshakeInstance> _crud;
        private readonly ServerCrud _server;

        public InstanceModuleHandler(Settings settings, DiscordSocketClient client,
            InteractionService slash, ICrud<MilkshakeInstance> crud, ServerCrud server, ApplicationDbContext context)
        {
            GuildId = settings.General.DebugGuildId;
            _client = client;
            _slash = slash;
            _crud = crud;
            _server = server;
            _context = context;
        }

        public static event Activated? MilkshakeActivated;
        public delegate Task Activated(ulong guild);

        public async Task InitializeAsync()
        {
            _client.Ready += HandleInstanceModules;
            
        }

        public static async Task OnMilkshakeActivate(ulong guild)
        {
            if (MilkshakeActivated != null)
                await MilkshakeActivated.Invoke(guild);
        }

        private async Task HandleInstanceModules()
        {
            var instances = await _crud.GetAllMilkshakes(); //TODO - (MilkshakeInstance[]) JUST IN CASE
            var servers = await _server.GetAll();

            var guilds = _client.Guilds;
            var inactive = new List<SocketGuild>(guilds);

            var activeModule = _slash.GetModuleInfo<ActiveMilkshake>();
            var inactiveModule = _slash.GetModuleInfo<InactiveMilkshake>();

            foreach (var guild in guilds)
            {
                foreach (var instance in instances)
                {
                    foreach (var server in servers)
                    {
                        if (server.Id == guild.Id && server.MilkshakeContextId == instance.ContextId)
                        {
                            await _slash.AddModulesToGuildAsync(guild, true, activeModule);
                            inactive.Remove(guild);
                        }
                    }
                    //if (instance.Servers.Any(server => guild.Id == server.Id))
                    //{

                    //    await _slash.AddModulesToGuildAsync(guild, true, activeModule);
                    //    inactive.Remove(guild);
                    //}
                }
            }

            foreach (var guild in inactive)
            {

                await _slash.AddModulesToGuildAsync(guild, true, inactiveModule);
            }

            MilkshakeActivated += async (id) =>
            {
                await _slash.RemoveModuleAsync<InactiveMilkshake>();
                var module = _slash.GetModuleInfo<ActiveMilkshake>();
                var guild = _client.GetGuild(id);
                await _slash.AddModulesToGuildAsync(guild, true, module);
            };
        }
    }
}
