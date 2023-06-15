using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Timers;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using ImageMagick;
using Kimi.Logging;
using Kimi.Services.Commands;
using Kimi.Services.Commands.Milkshake;
using Kimi.Services.CRUD;
using Kimi.Services.Helpers;
using Kimi.Services.Milkshake;
using Kimi.Services.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Milkshake;
using Milkshake.Builders;
using Milkshake.Configuration;
using Milkshake.Crud;
using Milkshake.Generation;
using Milkshake.Managers;
using Milkshake.Models;
using Milkshake.Models.Interfaces;
using static System.Net.Mime.MediaTypeNames;
using Color = Milkshake.Color;
using SelectMenuBuilder = Discord.SelectMenuBuilder;
using Severity = Kimi.Logging.Severity;

namespace Kimi.Commands.Modules.Milkshake
{
    public partial class ActiveMilkshake : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly ModalEvents _modal;
        private readonly ICrud<Source> _source;
        private readonly ICrud<Template> _template;
        private readonly ICrud<global::Milkshake.Models.Topping> _properties;
        private readonly ICrud<MilkshakeInstance> _instance;
        private readonly ServerCrud _server;
        private readonly MilkshakeService _service;
        private readonly GenerationQueue _generation;
        private readonly SourceBuilder _sb;

        private readonly RequestOptions _options = new() { Timeout = 60000 };

        private ContextData? _contextData;

        private (string, string?) _fields;



        //private static ulong _id;

        //private readonly IOptions<MilkshakeOptions> _options;
        public ActiveMilkshake(ModalEvents modal, 
            ICrud<Source> source, ICrud<Template> template, ICrud<global::Milkshake.Models.Topping> properties,
            MilkshakeService service, SourceBuilder sb, ServerCrud server, ICrud<MilkshakeInstance> instance, GenerationQueue generation)
        {
            _modal = modal;
            _source = source;
            _template = template;
            _properties = properties;
            _service = service;
            _sb = sb;
            _instance = instance;
            _server = server;
            _generation = generation;
            //_options = options;
        }

        [SlashCommand("template", "Template")]
        public async Task HandleTemplateCommand(
            [Summary("search", "Find a Template by its Name or ID"), Autocomplete]
            string? search = null,
            [Summary("create", "Create a Template by first sending an image")]
            IAttachment? image = null)
        {
            Console.WriteLine(_service.Options.BasePath);
            if (!string.IsNullOrWhiteSpace(search))
            {
                if (Guid.TryParse(search, out Guid id))
                    await VisualizeTemplate(id);
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

                    if (sent.CustomId == "template_modal" && sent.User == Context.Interaction.User)
                    {
                        var modal = (TemplateModal?)sender;
                        fields.name = modal!.Name;
                        fields.description = modal.Description;
                        source.Cancel();
                    }
                };

                var user = Context.Interaction.User.Id.ToString();

                await Context.Interaction.RespondWithModalAsync<TemplateModal>($"template_modal:{user}");

                //var a = SendTemplateImage(image, _fields, 49484);

                try
                {
                    await Wait(source);
                }
                catch (TaskCanceledException)
                {
                    await SendTemplateImage(image, fields, user);
                }

            }
            else
            {
                await new HelpModule(Context).HandleTemplateHelp(Context.Interaction.User.Id.ToString(), "false");
            }
        }

        private static async Task Wait(CancellationTokenSource source, int milliseconds = -1)
        {
            await Task.Delay(milliseconds, source.Token);
        }

        [AutocompleteCommand("search", "template")]
        public async Task HandleSearchAutoComplete() => await Autocomplete();
        [AutocompleteCommand("force", "generate")]
        public async Task HandleForceAutoComplete() => await Autocomplete();

        public async Task Autocomplete()
        {
            // ReSharper disable once UseNegatedPatternMatching
            var context = (Context.Interaction as SocketAutocompleteInteraction);

            if (context is null)
                return;

            var userInput = context.Data.Current.Value.ToString() ?? "";

            var allMilkshakes = (Template[])await _template.GetAllMilkshakes();

            var preResult = allMilkshakes
                .Select(name => new AutocompleteResult(name.Name, name.Id.ToString()))
                .ToList();


            var results = new List<AutocompleteResult>(preResult)
                .Where(x => x.Name.StartsWith(userInput, StringComparison.InvariantCultureIgnoreCase));

            await context.RespondAsync(results.Take(25));
        }

        public class TemplateModal : IModal
        {
            public string Title => "Template Creator";

            [RequiredInput(true)]
            [InputLabel("Name")]
            [ModalTextInput("template_name", TextInputStyle.Short, "Nazubeans", 1, 16)]
            public string Name { get; set; }

            [RequiredInput(false)]
            [InputLabel("Description")]
            [ModalTextInput("template_description", TextInputStyle.Paragraph, "Nazubeans is absolutely awesome!", 1, 64)]
            public string? Description { get; set; }
        }

        private async Task<bool> HandleImage(IAttachment image)
        {
            try
            {
                await image.DownloadAttachment();
                return true;
            }
            catch (IOException ex)
            {
                await Log.Write(ex.Message, Severity.Error);

                await RespondAsync("The image you have sent is possibly a duplicate.", ephemeral: true);

                return false;
            }
            catch (Exception ex)
            {
                await Log.Write(ex.ToString(), Severity.Error);

                await RespondAsync("Sorry, something went wrong.", ephemeral: true);
                return false;
            }

        }

        [ModalInteraction("template_modal:*", true)]
        public async Task HandleTemplateModal(string user, TemplateModal modal)
        {
            await DeferAsync();

            _fields.Item1 = modal.Name;
            _fields.Item2 = modal.Description;

            await _modal.OnMilkshakeSent(modal, new OnSentArgs(){CustomId = "template_modal", User = Context.Interaction.User});
            
            //await _modal.ModalSent(Context.Interaction.User.Id, (modal.Name, modal.Description));
        }

        private async Task SendTemplateImage(IAttachment data, (string name, string? description) modal, string user)
        {
            //if(id != Context.Interaction.User.Id)
            //{
            //    await Log.Write($"User context ids don't match.\n" +
            //                    $"Expected {id} and got {Context.Interaction.User.Id}.");
            //    return;
            //}
            
            try
            {
                var server = await _server.GetMilkshakeId(Context.Guild.Id);
                _contextData = await _instance.GetContext(server);

                var template = new TemplateBuilder(_service, _contextData)
                    .WithName(modal.name)
                    .WithDescription(modal.description)
                    .WithUrl(data.Url)
                    .WithStats(Context.Interaction.User.Mention)
                    .Build();
                

                await _template.CreateMilkshake(template, Context.Guild.Id);

                //await RespondWithFileAsync(source.Path);

                //var filename = Path.GetFileName(template.Path);

                //filename = Regex.Replace(filename, "\\s+", "_");
                //filename = new string(filename.Where(x => !char.IsSymbol(x) && !char.IsPunctuation(x) || x is '.' or '_' or '-').ToArray());
                ////await Context.Channel.SendFileAsync(filename);
                //Console.WriteLine(filename);
                //var embed = new EmbedBuilder()
                //        .WithAuthor("Source")
                //        .WithTitle(CultureInfo.CurrentCulture.TextInfo.ToTitleCase(modal.name))
                //        .WithDescription(modal.description)
                //        .AddField(field =>
                //        {
                //            field.IsInline = true;
                //            field.Name = "Toppings";
                //            field.Value = template.Toppings!.Count;
                //        })
                //        .AddField(field =>
                //        {
                //            field.IsInline = true;
                //            field.Name = "Times generated";
                //            field.Value = template.TimesUsed;
                //        })
                //        .AddField(field =>
                //        {
                //            field.IsInline = true;
                //            field.Name = "Sent by";
                //            field.Value = template.Creator;
                //        })
                //        .WithColor(0xf9a7b7)
                //        .WithImageUrl($"attachment://{filename}")
                //        .WithTimestamp(template.CreationDateTime)
                //        .WithFooter(template.Id.ToString())
                //        .Build();

                //var properties = new ButtonBuilder()
                //    .WithCustomId("template_properties")
                //    .WithLabel("Change tags")
                //    .WithStyle(ButtonStyle.Success);

                //var rename = new ButtonBuilder()
                //    .WithCustomId("template_rename")
                //    .WithLabel("Edit")
                //    .WithStyle(ButtonStyle.Primary);

                //var delete = new ButtonBuilder()
                //    .WithCustomId("template_delete")
                //    .WithLabel("Eliminate")
                //    .WithStyle(ButtonStyle.Danger);

                //var component = new ComponentBuilder()
                //    .WithButton(properties)
                //    .WithButton(rename)
                //    .WithButton(delete)
                //    .Build();

                //await Context.Channel.SendFileAsync(template.Path, null, false, embed, components: component);

                //await Log.Write("Successfully sent!", Severity.Verbose);

                await VisualizeTemplate(template.Id, true, user);
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

        [ComponentInteraction("template_properties:*", true)]
        public async Task HandleTemplatePropertiesButton(string user)
        {
            var context = Context.Interaction as SocketMessageComponent;

            var prevEmbed = context.Message.Embeds.FirstOrDefault();
            var msg = context!.Message.Id.ToString();
            

            Guid.TryParse(prevEmbed.Footer.Value.Text, out var guid);

            var template = await _template.GetMilkshake(guid) as Template;
            var props = await _properties.GetAllMilkshakes(template!.Id) as global::Milkshake.Models.Topping[];

            if (props!.Length is 0)
            {
                await HandlePropertiesCommand(Choice.Add, template, user, msg);
                // TODO - Create property button
                return;
            }

            var menu = new SelectMenuBuilder()
                .WithPlaceholder("Select one property")
                .WithCustomId($"tprop_selec:{user}")
                .WithMinValues(1)
                .WithMaxValues(1);

            foreach (var item in props)
            {
                var type = item.IsText ? $"Text - {item.Width}x{item.Height}" : $"Image - {item.Width}x{item.Height}";
                menu.AddOption(item.Name, item.Id.ToString(), type);
            }

            var component = new ComponentBuilder()
                .WithSelectMenu(menu)
                .Build();

            await context.UpdateAsync(x => x.Components = component);
        }

        [ComponentInteraction("tprop_selec:*", true)]
        public async Task HandlePropertySelect(string user, string idStr)
        {
            await DeferAsync();
            var context = Context.Interaction as SocketMessageComponent;

            var id = Guid.Parse(idStr);

            var property = await _properties.GetMilkshake(id) as global::Milkshake.Models.Topping;

            var builder = new EmbedBuilder()
                .WithColor(0xf9a7b7)
                .WithAuthor("Topping")
                .WithTitle(property.Name)
                .AddField(field =>
                {
                    field.WithName("Tags");
                    field.WithValue(property.Tags.ToString().Replace(',', '\n'));
                    field.WithIsInline(true);
                })
                .AddField(field =>
                {
                    field.WithName("Anchor point");
                    field.WithValue($"**X** is {property.X}\n**Y** is {property.Y}");
                    field.WithIsInline(true);
                })
                .AddField(field =>
                {
                    field.WithName("Dimensions");
                    field.WithValue($"{property.Width}x{property.Height}");
                    field.WithIsInline(true);
                });

            if (property.IsText)
            {
                builder
                    .AddField(field =>
                    {
                        Color.TryParse(property.Color, out var color);
                        field.WithName("Color");
                        field.WithValue(color.Name);
                        field.WithIsInline(true);
                    })
                    .AddField(field =>
                    {
                        Font.TryFind(property.Font, out var font);
                        field.WithName("Font");
                        field.WithValue(font.DisplayName);
                        field.WithIsInline(true);
                    })
                    .AddField(field =>
                    {
                        field.WithName("Orientation");
                        field.WithValue(property.Orientation);
                        field.WithIsInline(true);
                    });

                if (property.StrokeWidth > 0)
                {
                    builder
                        .AddField(field =>
                        {
                            Color.TryParse(property.StrokeColor, out var color);
                            field.WithName("Stroke Color");
                            field.WithValue(color.Name);
                            field.WithIsInline(true);
                        })
                        .AddField(field =>
                        {
                            field.WithValue("\u200b");
                            field.WithName("\u200b");
                            field.WithIsInline(true);
                        })
                        .AddField(field =>
                        {
                            field.WithName("Stroke Width");
                            field.WithValue(property.StrokeWidth);
                            field.WithIsInline(true);
                        });
                }
            }
            else
            {
                builder
                    .AddField(field =>
                    {
                        field.WithName("\u200b");
                        field.WithValue("\u200b");
                        field.WithIsInline(true);
                    })
                    .AddField(field =>
                    {
                        field.WithName("Position");
                        field.WithValue(property.Layer.ToString());
                        field.WithIsInline(true);
                    })
                    .AddField(field =>
                    {
                        field.WithName("\u200b");
                        field.WithValue("\u200b");
                        field.WithIsInline(true);
                    });
            }
            
            var embed = builder
                .WithFooter(property.Id.ToString())
                .Build();

            var template = await _template.GetMilkshake(property.TemplateId) as Template;
            var instance = await _instance.GetMilkshake(template!.MilkshakeContextId) as MilkshakeInstance;
            

            var templateReturn = new ButtonBuilder()
                .WithCustomId($"template_return:{context.User.Id}")
                .WithLabel($"Return to {template.Name}")
                .WithEmote(new Emoji("\u21AA"))
                .WithStyle(ButtonStyle.Secondary);

            var row = new ActionRowBuilder()
                .WithButton(templateReturn);

            var component = PropAdminButtons(template, instance, user)
                .AddRow(row)
                .Build();

            await context!.Message.DeleteAsync();

            await context!.Channel.SendMessageAsync(embed: embed, components: component);
        }

        [ComponentInteraction("prop_new:*", true)]
        public async Task HandleNewProperty(string user)
        {
            var context = Context.Interaction as SocketMessageComponent;

            var prevEmbed = context!.Message.Embeds.FirstOrDefault();
            var msg = context!.Message.Id.ToString();


            Guid.TryParse(prevEmbed!.Footer!.Value.Text, out var guid);

            var template = await _template.GetMilkshake(guid);

            await HandlePropertiesCommand(Choice.Add, template!, user, msg);
        }
        
        [ComponentInteraction("template_delete:*", true)]
        public async Task HandleTemplateDelete(string user)
        {
            var context = Context.Interaction as SocketMessageComponent;

            var prevEmbed = context!.Message.Embeds.FirstOrDefault();


            Guid.TryParse(prevEmbed!.Footer!.Value.Text, out var id);

            var template = await _template.GetMilkshake(id) as Template ?? throw new InvalidOperationException();
            var props = await _properties.GetAllMilkshakes(template!.Id) as global::Milkshake.Models.Topping[];

            template.Delete();
            await _template.DeleteMilkshake(id, false);

            foreach (var prop in props!)
            {
                if (prop.TemplateId == template!.Id)
                    await _properties.DeleteMilkshake(prop.Id, false);
            }

            await _properties.SaveAsync();

            var instance = await _instance.GetMilkshake(template.MilkshakeContextId);
            var component = TemplateButtons(props!, template, instance, user).Build();

            //var properties = new ButtonBuilder()
            //    .WithCustomId("template_properties")
            //    .WithLabel(props!.Length is 0 ? "Create property" : "Toppings")
            //    .WithStyle(ButtonStyle.Success)
            //    .WithDisabled(true);

            //var newProp = new ButtonBuilder();

            //if (props!.Length > 0)
            //    newProp
            //        .WithCustomId("prop_new")
            //        .WithLabel("New")
            //        .WithStyle(ButtonStyle.Success)
            //        .WithDisabled(true);

            //var rename = new ButtonBuilder()
            //    .WithCustomId("template_rename")
            //    .WithLabel("Edit")
            //    .WithStyle(ButtonStyle.Primary)
            //    .WithDisabled(true);

            //var delete = new ButtonBuilder()
            //    .WithCustomId("template_delete")
            //    .WithLabel("Eliminate")
            //    .WithStyle(ButtonStyle.Danger)
            //    .WithDisabled(true);

            //var builder = new ComponentBuilder()
            //    .WithButton(properties);

            //if (props!.Length > 0)
            //    builder.WithButton(newProp);

            //var component = builder
            //    .WithButton(rename)
            //    .WithButton(delete)
            //    .Build();

            await context.UpdateAsync(x => x.Components = component);
        }

        [DoUserCheck]
        [ComponentInteraction("template_return:*", true)]
        public async Task HandleTemplateReturn(string user)
        {
            var context = Context.Interaction as SocketMessageComponent;
            

            var prevEmbed = context!.Message.Embeds.FirstOrDefault();

            Guid.TryParse(prevEmbed!.Footer!.Value.Text, out var id);

            var prop = await _properties.GetMilkshake(id) as global::Milkshake.Models.Topping;

            await context.Message.DeleteAsync();
            await VisualizeTemplate(prop!.TemplateId, false, user);
        }

        public async Task VisualizeTemplate(Guid id, bool create = false, string? user = null)
        {
            
            if(!create)
                await RespondAsync("Fetching template!");

            var template = await _template.GetMilkshake(id) as Template ?? throw new InvalidOperationException();
            var instance = await _instance.GetMilkshake(template.MilkshakeContextId) as MilkshakeInstance;
            var props = await _properties.GetAllMilkshakes(template!.Id) as global::Milkshake.Models.Topping[];
            var sources =
                await _source.GetAllMilkshakes(Guid.Parse("4caf2236-5a55-4665-a0c6-1ed0f9754670")) as Source[];

            var embed = new EmbedBuilder()
                        .WithAuthor("Template")
                        .WithTitle($"{CultureInfo.CurrentCulture.TextInfo.ToTitleCase(template.Name)} [{template.Width}x{template.Height}]")
                        .WithDescription(template.Description)
                        .AddField(field =>
                        {
                            field.IsInline = true;
                            field.Name = "Toppings";
                            field.Value = props!.Length;
                        })
                        .AddField(field =>
                        {
                            field.IsInline = true;
                            field.Name = "Times generated";
                            field.Value = template.TimesUsed;
                        })
                        .AddField(field =>
                        {
                            field.IsInline = true;
                            field.Name = "Sent by";
                            field.Value = template.Creator;
                        })
                        .WithColor(0xf9a7b7)
                        .WithImageUrl($"attachment://{template.GetFilteredFileName()}")
                        .WithTimestamp(template.CreationDateTime)
                        .WithFooter(template.Id.ToString())
                        .Build();


            user ??= Context.Interaction.User.Id.ToString();
            var component = TemplateButtons(props!, template, instance, user).Build();

            await Context.Channel.SendFileAsync(template.Path, null, false, embed, components: component);
            if(!create)
                await Context.Interaction.DeleteOriginalResponseAsync();
        }
    }
}
