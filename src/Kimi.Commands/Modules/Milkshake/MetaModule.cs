using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Kimi.Services.Commands.Milkshake;
using Kimi.Services.CRUD;
using Kimi.Services.Milkshake;
using Kimi.Services.Models;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore;
using Milkshake;
using Milkshake.Attributes;
using Milkshake.Builders;
using Milkshake.Crud;
using Milkshake.Managers;
using Milkshake.Models;

namespace Kimi.Commands.Modules.Milkshake
{
    public class MetaModule
    {
        private readonly DiscordSocketClient _client;
        private readonly InteractionService _slash;
        private readonly IServiceProvider _services;

        public MetaModule(InteractionService slash, DiscordSocketClient client, IServiceProvider services)
        {
            _slash = slash;
            _client = client;
            _services = services;
        }

        
    }

    [DontAutoRegister]
    [Group("milkshake", "Milkshake commands")]
    public partial class ActiveMilkshake
    {
        [Group("meta", "meta")]
        public class MilkshakeMeta : InteractionModuleBase<SocketInteractionContext>
        {
            public enum Choices
            {
                Info,
                Add,
                Remove
            }

            private readonly ICrud<MilkshakeInstance> _crud;
            private readonly MilkshakeService _service;
            private readonly InstanceBuilder<MilkshakeInstance> _builder;
            private readonly InstanceManager<MilkshakeInstance> _manager;
            private readonly ServerCrud _server;


            [SlashCommand("instance", "instance")]
            public async Task HandleInstanceCommand()
            {
                var instances = (MilkshakeInstance[])await _crud.GetAllMilkshakes();

                bool a;
                foreach (var instance in instances)
                {
                    if (instance.Servers.Any(server => Context.Guild.Id == server.Id))
                    {
                        a = true;
                    }
                }


            }

            [SlashCommand("vip", "vip")]
            public async Task HandleVipCommand(Choices choice = Choices.Info, IUser? user = null)
            {
                await DeferAsync();
                var guid = await _server.GetMilkshakeId(Context.Guild.Id);
                _manager.Instance = await _crud.GetMilkshake(guid) as MilkshakeInstance ?? throw new InvalidOperationException();

                var contextData = new ContextData()
                {
                    Caller = Context.Interaction.User.Id.ToString()
                };

                var instance = choice switch
                {
                    Choices.Add => user is null? null : _manager.AddVip(user.Id.ToString(), contextData),
                    Choices.Remove => user is null ? null : _manager.RemoveVip(user.Id.ToString(), contextData),
                    Choices.Info => null,

                    _ => null
                };

                if(instance is not null)
                {
                    await _crud.UpdateMilkshake(instance, instance.ContextId);
                    await FollowupAsync($"Vip - {choice.ToString().TrimEnd('e')}ed {user.Mention}.");
                }
                else
                    await FollowupAsync("info");

            }

            public MilkshakeMeta(ICrud<MilkshakeInstance> crud, MilkshakeService service, ServerCrud server, 
                InstanceBuilder<MilkshakeInstance> builder, InstanceManager<MilkshakeInstance> manager)
            {
                _crud = crud;
                _service = service;
                _builder = builder;
                _manager = manager;
                _server = server;
            }
        }

    }

    [DontAutoRegister]
    //[Group("milkshake", "a")]
    public partial class InactiveMilkshake : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly ICrud<MilkshakeInstance> _crud;
        private readonly ServerCrud _server;
        private readonly InstanceModuleHandler _handler;
        private readonly InstanceBuilder<MilkshakeInstance> _manager;

        public InactiveMilkshake(ICrud<MilkshakeInstance> crud, InstanceModuleHandler handler, ServerCrud server, InstanceBuilder<MilkshakeInstance> manager)
        {
            _crud = crud;
            _handler = handler;
            _server = server;
            _manager = manager;
        }
        
        [SlashCommand("milkshake", "Create or share a Milkshake context!")]
        public async Task CreateInstanceCommand(
            [Summary("id",
                "If the ID matches with some other server, the context will be shared.")]
            string? id = null)
        {
            await DeferAsync();

            MilkshakeInstance? instance;

            if (Guid.TryParse(id, out var guid))
            {
                instance = (MilkshakeInstance?)_crud.GetMilkshake(guid).Result;
                if (instance != null)
                {
                    await _server.SetMilkshakeId(Context.Guild, guid);
                    await _crud.SaveAsync();
                    await FollowupAsync($"Instance found and connected!");
                }
                else
                    await FollowupAsync($"Instance with id {id} not found.");

                return;
            };

            instance = _manager
                .WithVip(Context.User.Id)
                .WithVip(Context.Guild.OwnerId)
                .SetInstanceFolder()
                .Build();



            await _server.SetMilkshakeId(Context.Guild, instance.ContextId);
            await _crud.CreateMilkshake(instance);

            await InstanceModuleHandler.OnMilkshakeActivate(Context.Guild.Id);
            await FollowupAsync("Milkshake instance created!");
        }
    }
}
