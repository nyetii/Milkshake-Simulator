using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kimi.Services.Models;
using Microsoft.EntityFrameworkCore;
using Milkshake.Attributes;
using Milkshake.Crud;
using Milkshake.Models;
using Milkshake.Models.Interfaces;

namespace Kimi.Services.Milkshake
{
    [Handle(typeof(Topping))]
    public class PropertiesHandler : IMilkshakeHandler<Topping, PropertiesHandler>
    {
        private readonly ApplicationDbContext _context;
        private Topping? _properties;

        public PropertiesHandler(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Topping?> Get(Guid id) => await _context.TemplateProperties!.FindAsync(id)!;
        
        public async Task<PropertiesHandler> GetMilkshake(Guid id)
        {
            _properties = await _context.TemplateProperties!.FindAsync(id);

            return this;
        }

        public async Task<Topping[]> Get(string name)
        {
            throw new NotImplementedException();
        }

        public async Task<Topping[]> GetAll()
        {
            throw new NotImplementedException();
        }

        public async Task<Topping[]> GetAll(Guid id) => await _context.TemplateProperties!.Where(x => x.TemplateId == id).ToArrayAsync();

        public async Task Add(Topping media) => await _context.TemplateProperties!.AddAsync(media);

        public async Task Update(Topping media, Guid id)
        {
            throw new NotImplementedException();
        }

        public async Task Delete(Topping media) => await Task.FromResult(_context.TemplateProperties!.Remove(media));

        public async Task Delete() => await Task.FromResult(_context.TemplateProperties!.Remove(_properties!));

        public Topping Build()
        {
            throw new NotImplementedException();
        }
    }
}
