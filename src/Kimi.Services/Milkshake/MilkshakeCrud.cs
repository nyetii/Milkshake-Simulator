using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Reactive;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using Discord.Interactions;
using Kimi.Logging;
using Kimi.Services.Models;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Milkshake.Attributes;
using Milkshake.Crud;
using Milkshake.Managers;
using Milkshake.Models;
using Milkshake.Models.Interfaces;
using Severity = Kimi.Logging.Severity;

namespace Kimi.Services.Milkshake
{
    [Crud]
    public class MilkshakeCrud<T> : ICrud<T> where T : class, ICommonBase, new()
    {
        private readonly ApplicationDbContext _context;
        private readonly IMilkshakeHandler<MilkshakeInstance, InstanceHandler> _instance;
        private readonly IMilkshakeHandler<Source, SourceHandler> _source;
        private readonly IMilkshakeHandler<Template, TemplateHandler> _template;
        private readonly IMilkshakeHandler<Topping, PropertiesHandler> _properties;

        public MilkshakeCrud(ApplicationDbContext context, IMilkshakeHandler<Template, TemplateHandler> template,
            IMilkshakeHandler<Source, SourceHandler> source,
            IMilkshakeHandler<Topping, PropertiesHandler> properties,
            IMilkshakeHandler<MilkshakeInstance, InstanceHandler> instance)
        {
            _context = context;
            _template = template;
            _source = source;
            _properties = properties;
            _instance = instance;
        }

        public async Task<ContextData> GetContext(Guid id)
        {
            var instance = await _instance.Get(id) ?? throw new NullReferenceException();

            return new ContextData()
            {
                ContextId = instance.ContextId,
                Vips = instance.Vips ?? throw new NullReferenceException(),
            };
        }

        public async Task<T?> GetMilkshake(Guid id)
        {
            return new T() switch
            {
                Source => await _source.Get(id) as T,
                Template => await _template.Get(id) as T,
                Topping => await _properties.Get(id) as T,

                MilkshakeInstance => await _instance.Get(id) as T,

                _ => throw new Exception()
            };
        }

        public async Task<T[]> GetAllMilkshakes(Guid? id = null)
        {
            //var milkshake = new T();


            return new T() switch
            {
                Source => id is null ? await _source.GetAll() as T[] : await _source.GetAll((Guid)id) as T[],
                Template => id is null ? await _template.GetAll() as T[] : await _template.GetAll((Guid)id) as T[],
                Topping => id is null ? await _properties.GetAll() as T[] : await _properties.GetAll((Guid)id) as T[],

                MilkshakeInstance => await _instance.GetAll() as T[],

                _ => throw new NotImplementedException()
            } ?? Array.Empty<T>();


            //return type;
        }

        public async Task CreateMilkshake(T milkshake, ulong? server = null, bool save = true)
        {
            var a = new T() switch
            {
                Source => _source.Add(milkshake as Source ?? throw new InvalidOperationException()),
                Template => _template.Add(milkshake as Template ?? throw new InvalidOperationException()),
                Topping => _properties.Add(milkshake as Topping ?? throw new InvalidOperationException()),

                MilkshakeInstance => _instance.Add(milkshake as MilkshakeInstance ?? throw new InvalidOperationException()),

                _ => throw new NotImplementedException()
            };

            switch (milkshake)
            {
                case IMedia media:
                    await Log.Write($"Created {media.GetType().Name} - {media.Name}", Severity.Verbose);
                    break;
                case MilkshakeInstance instance:
                    await Log.Write($"Created Instance - {instance.ContextId}", Severity.Verbose);
                    break;
            }

            if (save)
                await SaveAsync();

            //if (milkshake.GetType() == typeof(Template))
            //{
            //    //IMilkshakeHandler<Template, TemplateHandler> builder =
            //    //    new TemplateHandler(milkshake.Name, milkshake.Description, milkshake.Width, milkshake.Height, milkshake.Tags, _context.Servers.Find(server), _context);

            //    //var source = builder.Build();
            //    //await builder.Add(source);
            //    //await _context.SaveChangesAsync();
            //}

        }

        public async Task UpdateMilkshake(T milkshake, Guid id, bool save = true)
        {
            var a = new T() switch
            {
                Source => _source.Update(milkshake as Source ?? throw new InvalidOperationException(), id),
                Template => _template.Update(milkshake as Template ?? throw new InvalidOperationException(), id),
                Topping => throw new NotImplementedException(),

                MilkshakeInstance => _instance.Update(milkshake as MilkshakeInstance ?? throw new InvalidOperationException(), id),

                _ => throw new NotImplementedException()
            };

            //IMilkshakeHandler<Source, SourceHandler> builder = new SourceHandler(_context);
            //var source = await builder.Get(id);

            //builder.Update(source, id);
            switch (milkshake)
            {
                case IMedia media:
                    await Log.Write($"Updated {media.GetType().Name} - {media.Name}", Severity.Verbose);
                    break;
                case MilkshakeInstance instance:
                    await Log.Write($"Updated Instance - {instance.ContextId}", Severity.Verbose);
                    break;
            }

            if(save)
                await SaveAsync();
        }

        public async Task DeleteMilkshake(Guid id, bool save = true)
        {
            _ = new T() switch
            {
                Source => new SourceHandler(_context).GetMilkshake(id).Result.Delete(),
                Template => _template.GetMilkshake(id).Result.Delete(),
                Topping => _properties.GetMilkshake(id).Result.Delete(),
                _ => throw new NotImplementedException()
            };

            if (save)
                await SaveAsync();
        }

        public async Task SaveAsync() => await _context.SaveChangesAsync();
    }
}
