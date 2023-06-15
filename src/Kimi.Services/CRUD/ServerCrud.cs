using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.WebSocket;
using Kimi.Services.Models;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.EntityFrameworkCore;

namespace Kimi.Services.CRUD
{
    public class ServerCrud
    {
        private readonly ApplicationDbContext _context;
        public ServerCrud(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Servers[]> GetAll() => await _context.Servers!.ToArrayAsync();

        public async Task<Guid> GetMilkshakeId(ulong serverId)
        {
            var server = await _context.Servers!.FindAsync(serverId);
            return server?.MilkshakeContextId ?? throw new InvalidOperationException();
        }

        public async Task SetMilkshakeId(SocketGuild guild, Guid id)
        {
            var server = await _context.Servers!.FindAsync(guild.Id);

            if (server != null)
                server.MilkshakeContextId = id;
            else await AddServer(guild.Id, guild.Name);
        }

        public async Task AddServer(ulong id, string name)
        {
            var server = new Servers() {Id = id, Name = name};
            await _context.Servers!.AddAsync(server);
            await _context.SaveChangesAsync();
        }
    }
}
