using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.Interactions;
using Milkshake.Exceptions;
using Milkshake.Generation;
using Milkshake.Models;

namespace Kimi.Commands.Modules.Milkshake
{
    public partial class ActiveMilkshake
    {
        [SlashCommand("generate", "Generate")]
        public async Task HandleGenerateCommand(
            [Summary("force", "Find a Template by its Name or ID"), Autocomplete]
            string? search = null)
        {
            await DeferAsync();
            _cancel = new CancellationTokenSource();
            _generation.ImageGenerated += HandleGeneratedEvent;

            try
            {
                var server = await _server.GetMilkshakeId(Context.Guild.Id);

                var templateArr = await _template.GetAllMilkshakes(server); //TODO - as Template[] JUST IN CASE
                var sources =
                    await _source.GetAllMilkshakes(server); //TODO - as Source[] JUST IN CASE

                if (templateArr is null || templateArr.Length is 0)
                {
                    await FollowupAsync("There are no templates on this server.");
                    return;
                }

                if (sources is null || sources.Length is 0)
                {
                    await FollowupAsync("There are no sources on this server.");
                    return;
                }

                var success = Guid.TryParse(search, out var guid);

                Template? template;

                if (success)
                    template = templateArr.FirstOrDefault(x => x.Id == guid);
                else if (search is null)
                {
                    var rng = new Random().Next(0, templateArr.Length);
                    template = templateArr[rng];
                }
                else
                {
                    await FollowupAsync("Template not found");
                    return;
                }

                var props = await _properties.GetAllMilkshakes(template!.Id) as global::Milkshake.Models.Topping[];

                var gen = new Generation()
                {
                    Caller = Context.Interaction.User.Mention,
                    Sources = sources.ToList(),
                    Template = template
                };

                _id = gen.Id;

                _generation.Enqueue(gen);
                await _generation.Generate();
                await Wait(_cancel);
            }
            catch (TaskCanceledException)
            {
                await FollowupWithFileAsync(_path);

                File.Delete(_path);
            }
            catch (InvalidMilkshakeException ex)
            {
                await FollowupAsync(ex.Message);
            }
            finally
            {
                _generation.ImageGenerated -= HandleGeneratedEvent;
            }
        }

        private CancellationTokenSource _cancel;
        private Guid _id;
        private string _path;

        private async Task HandleGeneratedEvent(object? sender, GeneratedEventArgs args)
        {
            if(args.Id == _id)
            {
                await IncrementStatsAsync(sender as Generation ?? throw new NullReferenceException());

                _path = args.FilePath;
                _cancel.Cancel();
            }
        }

        private async Task IncrementStatsAsync(Generation generation)
        {
            generation.Template.TimesUsed++;

            await _template.UpdateMilkshake(generation.Template, generation.Template.Id, false);

            var duplicate = new List<string>();

            foreach (var source in generation.Properties)
            {
                if(!duplicate.Any(x => x.Equals(source.Name)))
                {
                    source.Source.TimesUsed++;

                    duplicate.Add(source.Name);

                    await _source.UpdateMilkshake(source.Source, source.Source.Id, false);
                }
            }

            await _template.SaveAsync();
        }
    }
}
