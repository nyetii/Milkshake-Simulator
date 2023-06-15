using Kimi.Services.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Milkshake.Attributes;
using Milkshake.Crud;
using Milkshake.Models;

namespace Kimi.Services.Milkshake
{
    [Handle(typeof(MilkshakeInstance))]
    public class InstanceHandler : IMilkshakeHandler<MilkshakeInstance, InstanceHandler>
    {
        private readonly ApplicationDbContext _context;

        private ulong _server;

        public InstanceHandler(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<MilkshakeInstance?> Get(Guid id) => await _context.Milkshake!.FindAsync(id);

        public async Task<InstanceHandler> GetMilkshake(Guid id)
        {
            //_server = ulong.Parse(id);
            return this;
        }

        public async Task<MilkshakeInstance[]> Get(string name)
        {
            throw new NotImplementedException();
        }

        public async Task<MilkshakeInstance[]> GetAll() => await _context.Milkshake!.ToArrayAsync();

        public async Task<MilkshakeInstance[]> GetAll(Guid id) => throw new NotImplementedException();
        public async Task Add(MilkshakeInstance media)
        {
            await _context.AddAsync(media);
        }

        public Task Update(MilkshakeInstance media, Guid id)
        {
            return id == media.ContextId ? Task.FromResult(_context.Milkshake!.Update(media)) : Task.FromException(new InvalidOperationException());
        }

        public async Task Delete(MilkshakeInstance media)
        {
            throw new NotImplementedException();
        }

        public MilkshakeInstance Build()
        {
            throw new NotImplementedException();
        }
    }
}
