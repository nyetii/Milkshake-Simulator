using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kimi.Services.Models;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore;
using Milkshake.Attributes;
using Milkshake.Crud;
using Milkshake.Models;
using Milkshake.Models.Interfaces;
using Instance = Milkshake.Models.Instance;

namespace Kimi.Services.Milkshake
{
    [Handle(typeof(Template))]
    public class TemplateHandler : IMilkshakeHandler<Template, TemplateHandler>
    {
        private readonly Guid _guid = Guid.NewGuid();
        private readonly string? _name;
        private readonly string? _description;
        private readonly (int width, int height) _dimensions;
        private readonly ImageTags _tags;
        private readonly Guid? _parentGuid;
        private readonly Instance? _milkshake;

        private Template? _template;

        private readonly ApplicationDbContext? _context;

        public TemplateHandler(string name, string description, int width, int height, ImageTags tags,
            Servers servers, ApplicationDbContext context)
        {
            _name = name;
            _description = description;
            _dimensions = (width, height);
            _tags = tags;
            _parentGuid = servers.MilkshakeContextId;
            _milkshake = servers.Milkshake;
            _context = context;
        }

        public TemplateHandler(Template media)
        {
            _template = media;
        }

        public TemplateHandler(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Template> Get(Guid id) => await _context.Template.FindAsync(id);

        public async Task<Template[]> Get(string name) => await _context.Template.FirstAsync(template => template.Name == name).ToAsyncEnumerable().ToArrayAsync();

        public async Task<Template[]> GetAll() => await _context.Template.ToArrayAsync();

        public async Task<Template[]> GetAll(Guid id) => await _context!.Template!.Where(template => template.MilkshakeContextId == id).ToArrayAsync();

        public async Task<TemplateHandler> GetMilkshake(Guid id)
        {
            //if (!Guid.TryParse(id, out var guid))
            //    throw new NotSupportedException();
            
            _template = await _context.Template.FindAsync(id);

            return this;
        }

        public async Task Add(Template media) => await _context.Template.AddAsync(media);
        public Task Update(Template media, Guid id)
        {
            return id == media.Id ? Task.FromResult(_context.Template.Update(media)) : Task.FromException(new InvalidOperationException());
        }

        public Task Delete(Template media) => Task.FromResult(_context.Template.Remove(media));
        public Task Delete() => Task.FromResult(_context.Template.Remove(_template));

        public Template Build()
        {
            _template = new Template()
            {
                Id = _guid,
                Name = _name,
                Description = _description,
                CreationDateTime = DateTime.Now,
                Width = _dimensions.width,
                Height = _dimensions.height,
                Tags = _tags,
                Path = $"{_name}_{_guid}",
                MilkshakeContextId = _parentGuid ?? throw new ArgumentNullException(),
                Milkshake = _milkshake,
                Toppings = new List<Topping>(){ new Topping()
                {
                    Name = "test",
                    Description = "test",
                    Width = 50,
                    Height = 50,
                    X = 50,
                    Y = 50,
                    Index = 9
                }}
            };

            return _template;
        }
    }
}
