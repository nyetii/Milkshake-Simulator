using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Kimi.Logging;
using Kimi.Services.Commands.Milkshake;
using Kimi.Services.Helpers;
using Kimi.Services.Models;
using Milkshake.Builders;
using Milkshake.Managers;
using Milkshake.Models;
using static System.Net.Mime.MediaTypeNames;

namespace Kimi.Commands.Modules.Milkshake
{
    public partial class ActiveMilkshake
    {
        [SlashCommand("source", "Source")]
        public async Task HandleSourceCommand(
            [Summary("search", "Find a Template by its Name or ID"), Autocomplete]
            string? search = null,
            [Summary("create", "Create a Template by first sending an image")]
            IAttachment? image = null)
        {
            Console.WriteLine(_service.Options.BasePath);
            if (!string.IsNullOrWhiteSpace(search))
            {
                if (Guid.TryParse(search, out Guid id))
                    await HandleSourceSearch(id);
                else
                    await RespondAsync("Milkshake not found.");
            }
            else if (image != null && image.ContentType.Contains("image"))
            {
                //if (!image.IsValid())
                //{
                //    await RespondAsync("File size limit reached (1 MB)", ephemeral: true);
                //    return;
                //}

                var source = new CancellationTokenSource();

                (string name, string? description) fields = (string.Empty, null);

                _modal.OnSent += async (sender, args) =>
                {
                    var sent = (OnSentArgs)args;

                    if(sent.CustomId == "source_modal" && sent.User == Context.User)
                    {
                        var modal = (SourceModal?)sender;
                        fields.name = modal!.Name;
                        fields.description = modal.Description;
                        source.Cancel();
                    }
                };

                await Context.Interaction.RespondWithModalAsync<SourceModal>("source_modal", options: _options);

                //var a = SendTemplateImage(image, _fields, 49484);

                try
                {
                    await Wait(source, 120000);
                    await Context.Channel.SendMessageAsync($"Source creation timed out. {Context.User.Mention}");
                }
                catch (TaskCanceledException)
                {
                    await SendSourceImage(image, fields, 49484);
                }

            }
            else
            {
                await RespondAsync("Please select one of the arguments.", ephemeral: true);
            }
        }
        

        [AutocompleteCommand("search", "source")]
        public async Task SourceAutocomplete()
        {
            // ReSharper disable file UseNegatedPatternMatching
            var context = (Context.Interaction as SocketAutocompleteInteraction);

            if (context is null)
                return;

            var userInput = context.Data.Current.Value.ToString() ?? "";

            var allMilkshakes = (Source[])await _source.GetAllMilkshakes();

            var preResult = allMilkshakes
                .Select(name => new AutocompleteResult(name.Name, name.Id.ToString()))
                .ToList();


            var results = new List<AutocompleteResult>(preResult)
                .Where(x => x.Name.StartsWith(userInput, StringComparison.InvariantCultureIgnoreCase));

            await context.RespondAsync(results.Take(25));
        }

        public class SourceModal : IModal
        {
            public string Title => "Source Creator";

            [RequiredInput(true)]
            [InputLabel("Name")]
            [ModalTextInput("source_name", TextInputStyle.Short, "Nazubeans", 1, 16)]
            public string Name { get; set; }

            [RequiredInput(false)]
            [InputLabel("Description")]
            [ModalTextInput("source_description", TextInputStyle.Paragraph, "Nazubeans is absolutely awesome!", 1, 64)]
            public string? Description { get; set; }
        }

        [ModalInteraction("source_modal", true)]
        public async Task HandleSourceModal(SourceModal modal)
        {
            Console.WriteLine(modal.Name + " - " + modal.Description);

            _fields.Item1 = modal.Name;
            _fields.Item2 = modal.Description;

            await _modal.OnMilkshakeSent(modal, new OnSentArgs(){CustomId = "source_modal", User = Context.Interaction.User});

            await Context.Interaction.RespondAsync("Working on it");
            // TODO - Find out a way to check the modal id


            //await _modal.ModalSent(Context.Interaction.User.Id, (modal.Name, modal.Description));
        }

        private async Task SendSourceImage(IAttachment data, (string name, string? description) modal, ulong id)
        {
            //if(id != Context.Interaction.User.Id)
            //{
            //    await Log.Write($"User context ids don't match.\n" +
            //                    $"Expected {id} and got {Context.Interaction.User.Id}.");
            //    return;
            //}

            await Log.Write($"Expected {id} and got {Context.Interaction.User.Id}.");
            try
            {
                var server = await _server.GetMilkshakeId(Context.Guild.Id);
                _contextData = await _instance.GetContext(server);
                
                var source = new SourceBuilder(_service, _contextData)
                    .WithName(modal.name)
                    .WithDescription(modal.description)
                    .WithUrl(data.Url)
                    .WithStats(Context.User.Mention)
                    .Build();

                await _source.CreateMilkshake(source, Context.Guild.Id);

                //await RespondWithFileAsync(source.Path);

                var filename = Path.GetFileName(source.Path);

                filename = Regex.Replace(filename, "\\s+", "_");
                filename = new string(filename.Where(x => !char.IsSymbol(x) && !char.IsPunctuation(x) || x is '.' or '_' or '-').ToArray());
                //await Context.Channel.SendFileAsync(filename);
                Console.WriteLine(filename);
                var embed = new EmbedBuilder()
                        .WithAuthor("Source")
                        .WithTitle(CultureInfo.CurrentCulture.TextInfo.ToTitleCase(modal.name))
                        .WithDescription(modal.description)
                        .AddField(field =>
                        {
                            field.IsInline = true;
                            field.Name = "Tags";
                            field.Value = source.Tags.ToString();
                        })
                        .AddField(field =>
                        {
                            field.IsInline = true;
                            field.Name = "Times generated";
                            field.Value = source.TimesUsed;
                        })
                        .AddField(field =>
                        {
                            field.IsInline = true;
                            field.Name = "Sent by";
                            field.Value = source.Creator;
                        })
                        .WithColor(0xf9a7b7)
                        .WithImageUrl($"attachment://{filename}")
                        .WithTimestamp(source.CreationDateTime)
                        .WithFooter(source.Id.ToString())
                        .Build();

                var menu = new SelectMenuBuilder()
                    .WithPlaceholder("Select up to 3 tags")
                    .WithCustomId("stag_selec")
                    .WithMinValues(1)
                    .WithMaxValues(3)
                    .AddOption("Any", $"0;{source.Id}", "Generic tag")
                    .AddOption("Person", $"1;{source.Id}", "Real or fictional, human or animal")
                    .AddOption("Symbol", $"2;{source.Id}", "Scripts, logos, flags")
                    .AddOption("Object", $"4;{source.Id}", "Inanimate things")
                    .AddOption("Shitpost", $"8;{source.Id}", "Memes")
                    .AddOption("Picture", $"16;{source.Id}", "Any real life photo or screenshot")
                    .AddOption("Post", $"32;{source.Id}", "Social media posts")
                    .AddOption("Text", $"64;{source.Id}", "Billboards, signs, etc.");

                var builder = new ComponentBuilder()
                    .WithSelectMenu(menu)
                    .Build();

                await Context.Channel.SendFileAsync(source.Path, null, false, embed, components: builder, options: _options);

                await Log.Write("Successfully sent!", Severity.Verbose);
            }
            catch (Exception ex)
            {
                await Log.Write(ex.ToString(), Severity.Error);
                var application = await Context.Client.GetApplicationInfoAsync();
                //var channel = await application.Owner.CreateDMChannelAsync();
                

                var embed = new EmbedBuilder()
                    .WithAuthor(application.Name, Context.Client.CurrentUser.GetAvatarUrl())
                    .WithTitle($"Exception - {ex.Source}")
                    .WithDescription($"```\n{ex}\n```")
                    .WithCurrentTimestamp()
                    .WithFooter($"#{Context.Channel.Name} in {Context.Guild.Name}")
                    .WithColor(0xFF0000)
                    .Build();


                

                await application.Owner.SendMessageAsync(embed: embed);
                await Context.Channel.SendMessageAsync("Sorry, something went wrong.");
                //await channel.SendMessageAsync(embed: embed);

                // TODO - Send Exceptions to the owner's DM
            }
            finally
            {
                // TODO - Remove leftover references
                await Task.CompletedTask;
            }
        }

        [ComponentInteraction("stag_selec", true)]
        public async Task SourceTagSelection(string[] selection)
        {
            var context = Context.Interaction as SocketMessageComponent;

            if (context is null)
                return;

            var id = Guid.Parse(selection.FirstOrDefault()!.Split(';').Last());

            //var j = selection.Select(x => x.Split(';').Last()).AsQueryable();
            //id = Guid.Parse(j.FirstOrDefault());
            //string id = string.Empty;

            //foreach (var item in selection)
            //{
            //    var x = item.Split(';').Last();
            //    id = x;
            //    break;
            //}

            var tagInt = selection.Select(x => int.Parse(x.Split(';').First())).ToArray();

            var tags = tagInt.Aggregate(ImageTags.Any, (current, item) => current | (ImageTags)item);

            var source = await _source.GetMilkshake(id) as Source;
            source!.Tags = tags;
            await _source.UpdateMilkshake(source, source.Id);

            var embed = new EmbedBuilder()
                .WithAuthor("Source")
                .WithTitle(CultureInfo.CurrentCulture.TextInfo.ToTitleCase(source.Name))
                .WithDescription(source.Description)
                .AddField(field =>
                {
                    field.IsInline = true;
                    field.Name = "Tags";
                    field.Value = source.Tags.ToString();
                })
                .AddField(field =>
                {
                    field.IsInline = true;
                    field.Name = "Times generated";
                    field.Value = source.TimesUsed;
                })
                .AddField(field =>
                {
                    field.IsInline = true;
                    field.Name = "Sent by";
                    field.Value = source.Creator;
                })
                .WithColor(0xf9a7b7)
                .WithImageUrl($"attachment://{source.GetFilteredFileName()}")
                .WithTimestamp(source.CreationDateTime)
                .WithFooter(source.Id.ToString())
                .Build();

            var changeTags = new ButtonBuilder()
                .WithCustomId($"source_tag:{context.User.Id}")
                .WithLabel("Change tags")
                .WithStyle(ButtonStyle.Success);

            var rename = new ButtonBuilder()
                .WithCustomId($"source_rename:{context.User.Id}")
                .WithLabel("Edit")
                .WithStyle(ButtonStyle.Primary);

            var delete = new ButtonBuilder()
                .WithCustomId($"source_delete:{context.User.Id}")
                .WithLabel("Eliminate")
                .WithStyle(ButtonStyle.Danger);

            var component = new ComponentBuilder()
                .WithButton(changeTags)
                .WithButton(rename)
                .WithButton(delete)
                .Build();

            //var embed = new EmbedBuilder()
            //    .WithAuthor("author")
            //    .WithTitle("title")
            //    .WithDescription("Description")
            //    .Build();
            
            await context!.UpdateAsync(x =>
            {
                x.Embed = embed;
                x.Components = component;
            });
        }

        [DoUserCheck]
        [ComponentInteraction("source_rename:*", true)]
        public async Task SourceRenameButton(string user)
        {
            var cancel = new CancellationTokenSource();

            var context = Context.Interaction as SocketMessageComponent;

            var embed = context.Message.Embeds.FirstOrDefault();

            //var message = context.Message;

            var id = Guid.Parse(embed.Footer.Value.Text);

            var source = await _source.GetMilkshake(id) as Source;

            _modal.OnSent += async (sender, args) =>
            {
                var sent = (OnSentArgs)args;

                if (sent.CustomId == "rename_modal" && sent.User == context.User)
                {
                    var modal = (SourceModal?)sender;
                    source.Path = source.RenameFile($"{modal.Name}-{source.Id}");
                    source.Name = modal.Name;
                    source.Description = modal.Description;
                    
                    cancel.Cancel();
                }
            };

            //if (isRename)
            //    return;

            await context.RespondWithModalAsync<SourceModal>("rename_modal", options: _options);

            try
            {
                await Wait(cancel, 60000);
                await Context.Channel.SendMessageAsync($"Source editing timed out. {Context.User.Mention}");
            }
            catch (TaskCanceledException)
            {
                await _source.UpdateMilkshake(source, id);

                embed = embed
                    .ToEmbedBuilder()
                    .WithTitle(source.Name)
                    .WithImageUrl($"attachment://{source.GetFilteredFileName()}")
                    .WithDescription(source.Description)
                    .Build();

                var changeTags = new ButtonBuilder()
                    .WithCustomId($"source_tag:{context.User.Id}")
                    .WithLabel("Change tags")
                    .WithStyle(ButtonStyle.Success);

                var rename = new ButtonBuilder()
                    .WithCustomId($"source_rename:{context.User.Id}")
                    .WithLabel("Edit")
                    .WithStyle(ButtonStyle.Primary);

                var delete = new ButtonBuilder()
                    .WithCustomId($"source_delete:{context.User.Id}")
                    .WithLabel("Eliminate")
                    .WithStyle(ButtonStyle.Danger);

                var component = new ComponentBuilder()
                    .WithButton(changeTags)
                    .WithButton(rename)
                    .WithButton(delete)
                    .Build();

                await context.Message.DeleteAsync();

                await Context.Channel.SendFileAsync(embed: embed, components: component, filePath: source.Path, options: _options);
                //await context.UpdateAsync(x => x.Components = builder);
            }
        }
        
        [ModalInteraction("rename_modal", true)]
        public async Task HandleRenameModal(SourceModal modal)
        {
            await DeferAsync(true);
            await _modal.OnMilkshakeSent(modal, new OnSentArgs() { User = Context.User, CustomId = "rename_modal"});
        }

        [DoUserCheck]
        [ComponentInteraction("source_tag:*", true)]
        public async Task SourceTagsButton(string user)
        {
            var context = Context.Interaction as SocketMessageComponent;

            var embed = context.Message.Embeds.FirstOrDefault();

            var id = Guid.Parse(embed.Footer.Value.Text);

            //var source = await _source.GetMilkshake(id) as Source;
            //await _source.UpdateMilkshake(source, id);

            var menu = new SelectMenuBuilder()
                .WithPlaceholder("Select up to 3 tags")
                .WithCustomId("stag_selec")
                .WithMinValues(1)
                .WithMaxValues(3)
                .AddOption("Any", $"0;{id}", "Generic tag")
                .AddOption("Person", $"1;{id}", "Real or fictional, human or animal")
                .AddOption("Symbol", $"2;{id}", "Scripts, logos, flags")
                .AddOption("Object", $"4;{id}", "Inanimate things")
                .AddOption("Shitpost", $"8;{id}", "Memes")
                .AddOption("Picture", $"16;{id}", "Any real life photo or screenshot")
                .AddOption("Post", $"32;{id}", "Social media posts")
                .AddOption("Text", $"64;{id}", "Billboards, signs, etc.");

            var builder = new ComponentBuilder()
                .WithSelectMenu(menu)
                .Build();

            await context.UpdateAsync(x => x.Components = builder, options: _options);
        }

        [DoUserCheck]
        [ComponentInteraction("source_delete:*", true)]
        public async Task SourceDeleteButton(string user)
        {
            var context = Context.Interaction as SocketMessageComponent;

            var embed = context.Message.Embeds.FirstOrDefault();
            //var content = context.Message.Attachments.FirstOrDefault()!.Url;

            var changeTags = new ButtonBuilder()
                .WithCustomId("source_tag")
                .WithLabel("Change tags")
                .WithStyle(ButtonStyle.Success)
                .WithDisabled(true);

            var rename = new ButtonBuilder()
                .WithCustomId("source_rename")
                .WithLabel("Edit")
                .WithStyle(ButtonStyle.Primary)
                .WithDisabled(true);

            var delete = new ButtonBuilder()
                .WithCustomId("source_delete")
                .WithLabel("Eliminate")
                .WithStyle(ButtonStyle.Danger)
                .WithDisabled(true);

            var component = new ComponentBuilder()
                .WithButton(changeTags)
                .WithButton(rename)
                .WithButton(delete)
                .Build();

            //embed = embed.ToEmbedBuilder().WithTitle($"{embed.Title} (eliminated)").Build();

            //var builder = new EmbedBuilder()
            //    .WithAuthor(embed.Author.Value.Name)
            //    .WithTitle(embed.Title)
            //    .WithDescription(embed.Description)
            //    .WithColor(embed.Color.Value);

            //foreach (var field in embed.Fields)
            //    builder.AddField(field.Name, field.Value, field.Inline);

            //embed = builder
            //    .WithImageUrl(embed.Image.Value.Url)
            //    .WithFooter(embed.Footer.Value.Text)
            //    .WithTimestamp(embed.Timestamp.Value)
            //    .Build();

            var id = Guid.Parse(embed.Footer.Value.Text);

            try
            {
                var source = await _source.GetMilkshake(id) as Source ?? throw new InvalidOperationException();
                source.Delete();

                await _source.DeleteMilkshake(id);
                await context!.UpdateAsync(x => { x.Components = component; }, options: _options);
                await Context.Channel.SendMessageAsync(
                    $"{embed.Author!.Value.Name} {embed.Title} was brutally eliminated!");
            }
            catch (ArgumentNullException)
            {
                await context!.RespondAsync("This source has already been brutally eliminated.");
            }
            catch (Exception ex)
            {
                await ex.DeferExceptionAsync(Context);
            }
        }

        public async Task HandleSourceSearch(Guid id)
        {
            var server = await _server.GetMilkshakeId(Context.Guild.Id);
            _contextData = await _instance.GetContext(server);

            var source = await _source.GetMilkshake(id) as Source;

            var embed = new EmbedBuilder()
                .WithAuthor("Source")
                .WithTitle(CultureInfo.CurrentCulture.TextInfo.ToTitleCase(source.Name))
                .WithDescription(source.Description)
                .AddField(field =>
                {
                    field.IsInline = true;
                    field.Name = "Tags";
                    field.Value = source.Tags.ToString();
                })
                .AddField(field =>
                {
                    field.IsInline = true;
                    field.Name = "Times generated";
                    field.Value = source.TimesUsed;
                })
                .AddField(field =>
                {
                    field.IsInline = true;
                    field.Name = "Sent by";
                    field.Value = source.Creator;
                })
                .WithColor(0xf9a7b7)
                .WithImageUrl($"attachment://{source.GetFilteredFileName()}")
                .WithTimestamp(source.CreationDateTime)
                .WithFooter(source.Id.ToString())
                .Build();

            var instance = await _instance.GetMilkshake(source.MilkshakeContextId) as MilkshakeInstance;

            var component = SourceButtons(source, instance)
                .Build();

            await RespondWithFileAsync(source.Path, embed: embed, components: component, options: _options);
        }
    }
}
