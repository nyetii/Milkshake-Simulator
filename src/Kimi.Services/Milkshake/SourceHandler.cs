using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kimi.Services.CRUD;
using Kimi.Services.Models;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore;
using Milkshake.Attributes;
using Milkshake.Crud;
using Milkshake.Models;
using Instance = Milkshake.Models.Instance;

namespace Kimi.Services.Milkshake
{
    [Handle(typeof(Source))]
    public class SourceHandler : IMilkshakeHandler<Source, SourceHandler>
    {
        private readonly Guid _guid = Guid.NewGuid();
        private readonly string? _name;
        private readonly string? _description;
        private readonly (int width, int height) _dimensions;
        private readonly ImageTags _tags;
        private readonly Guid? _parentGuid;
        private readonly Instance? _milkshake;

        private Source? _source;

        private readonly ApplicationDbContext? _context;

        public SourceHandler(string name, string description, int width, int height, ImageTags tags,
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

        public SourceHandler(Source media)
        {
            _source = media;
        }

        public SourceHandler(ApplicationDbContext context)
        {
            _context = context;
        }

        //public SourceBuilder GetParent(Servers set)
        //{
        //    _parentGuid = set.MilkshakeContextId ?? throw new ArgumentNullException();
        //    _milkshake = set.Milkshake ?? throw new ArgumentNullException();
        //    return this;
        //}

        public async Task<Source> Get(Guid id) => await _context.Source.FindAsync(id);

        public async Task<Source[]> Get(string name) => await _context.Source.Where(source => source.Name == name).ToArrayAsync();

        public async Task<Source[]> GetAll() => await _context.Source.ToArrayAsync();

        public async Task<Source[]> GetAll(Guid id) => await _context!.Source!.Where(x => x.MilkshakeContextId == id).ToArrayAsync();

        public async Task<SourceHandler> GetMilkshake(Guid id)
        {
            //if (!Guid.TryParse(id, out var guid))
            //    throw new NotSupportedException();

            _source = await _context.Source.FindAsync(id);

            return this;
        }

        public async Task Add(Source media) => await _context.Source.AddAsync(media);
        //{
        //    _context.Source.AddAsync(media);
        //    Console.WriteLine("aaaaaaaaaa");
        //}

        public Task Update(Source media, Guid id)
        {
            return id == media.Id ? Task.FromResult(_context.Source.Update(media)) : Task.FromException(new InvalidOperationException());
        }

        public Task Delete(Source media) => Task.FromResult(_context.Source.Remove(media));
        public Task Delete() => Task.FromResult(_context.Source.Remove(_source));

        public Source Build()
        {
            _source = new Source()
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
                Milkshake = _milkshake
            };

            //_source = new Source();

            //_source.Name = _name;
            //_source.Description = _description;
            //_source.Width = _dimensions.width;
            //_source.Height = _dimensions.height;
            //_source.Tags = _tags;
            //_source.Path = $"test";
            //_source.MilkshakeContextId = (Guid)_parentGuid;
            //_source.Milkshake = _milkshake;

            return _source;
        }
    }
}
