using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kimi.Services.Models;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore;
using Milkshake.Models;

namespace Kimi.Services.CRUD
{
    public class AddSource
    {
        private ApplicationDbContext _context;
        public AddSource(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task AddSourceAsync()
        {
            var guid = _context.Servers.Find((ulong)973401092274659358).MilkshakeContextId;
            var source = new Source()
            {
                MilkshakeContextId = (Guid)guid,
                Name = "Test",
                Description = "TestDescription",
                Height = 50,
                Path = "no",
                Tags = ImageTags.Any,
                Width = 50
            };
            
            _context.Source.Add(source);
            await _context.SaveChangesAsync();




        }

        public async Task AddMilkshakeAsync()
        {
            var milkshake = new MilkshakeInstance()
            {
                Servers = { _context.Servers.Find((ulong)973401092274659358) },
            };
            _context.Add(milkshake);
            await _context.SaveChangesAsync();
        }

        public async Task AddServerAsync()
        {
            var server = new Servers()
            {
                Id = 973401092274659358,
                Name = "Notes"
            };
            _context.Add(server);
            await _context.SaveChangesAsync();
        }
    }
}
