using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.Interactions;
using Kimi.Logging;
using Kimi.Services.CRUD;

namespace Kimi.Commands.Modules.Meta
{
    [Group("meta", "configurations")]
    public partial class Meta : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly ServerCrud _server;
        public Meta(ServerCrud server)
        {
            _server = server;
        }

        [SlashCommand("server", "server related configurations")]
        public async Task HandleServerCommand()
        {
            await DeferAsync();
            var servers = await _server.GetAll();

            bool exists = servers.Any(item => item.Id == Context.Guild.Id);

            if (!exists)
                await HandleServerCreation();
            else
                await FollowupAsync("I don't remember what should happen here after the server is created.");
        }

        private async Task HandleServerCreation()
        {
            try
            {
                await _server.AddServer(Context.Guild.Id, Context.Guild.Name);
                await FollowupAsync("Server added successfully!");
            }
            catch (Exception ex)
            {
                await Log.Write(ex.ToString(), Severity.Error);
                await FollowupAsync("Server could not be added");
            }
        }
    }
}
