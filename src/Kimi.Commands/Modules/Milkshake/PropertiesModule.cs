using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using ImageMagick;
using Kimi.Services.Commands.Milkshake;
using Kimi.Services.Helpers;
using Milkshake;
using Milkshake.Builders;
using Milkshake.Models;
using Color = Milkshake.Color;

namespace Kimi.Commands.Modules.Milkshake
{
    public partial class ActiveMilkshake
    {
        private Template? _temp;

        //[SlashCommand("properties", "Manages template properties")]
        public async Task HandlePropertiesCommand(Choice action, Template template, string user, string msg)
        {
            _temp = template;

            await RespondWithModalAsync<PropertiesModal>($"properties_modal:{user},{msg}");
            await CreateProperty();

            //await RespondAsync(string.Join(',', list));
        }

        private global::Milkshake.Models.Topping tp = new();
        private Topping? _property = new();
        private bool? isText = null;
        private Queue<(object Type, string Name)> queue = new ();
        private CancellationTokenSource token = new();

        private async Task CreateProperty()
        {
            _modal.OnSent += HandlePropertySentEvent;

            try
            {
                await Wait(token);
            }
            catch (TaskCanceledException)
            {
                _modal.OnSent -= HandlePropertySentEvent;

                if (Font.TryFind(_property!.Text.Font!, out var font))
                    _property!.Text.Font = font.Name;

                if (Color.TryParse(_property!.Text.Color!, out var color))
                    _property.Text.Color = color.ToString();

                if (Color.TryParse(_property!.Text.StrokeColor!, out color))
                    _property.Text.StrokeColor = color.ToString();

                var b = new ToppingBuilder(_service, _temp)
                    .WithName(_property.Props.Name)
                    .WithAnchors(int.Parse(_property.Props.X), int.Parse(_property.Props.Y))
                    .WithDimensions(int.Parse(_property.Props.Width), int.Parse(_property.Props.Height))
                    .WithTags(_property.Tags)
                    .WithLayer(_property.Layer);

                if (isText is true)
                    b.WithText(x =>
                    {
                        x.Color = _property.Text.Color;
                        x.Font = _property.Text.Font;
                        x.Orientation = _property.Orientation;
                        x.StrokeColor = _property.Text.StrokeColor;
                        x.StrokeWidth = int.Parse(_property.Text.StrokeWidth);
                    });

                var p = b.Build();
                await _properties.CreateMilkshake(p);

                await VisualizeTemplate(p.TemplateId, true);
                
            }

        }

        public void HandlePropertySentEvent(object? sender, EventArgs args)
        {
            var sent = args as OnSentArgs;
            if (sent.User == Context.Interaction.User)
            {
                var obj = sent!.CustomId switch
                {
                    "properties_modal" => sender,
                    "text_modal" => sender,
                    "button_image" => sender,
                    "orientation_selec" => sender,
                    "ptag_selec" => sender,
                    "image_layer" => sender,
                    _ => null
                };

                isText ??= sent.CustomId switch
                {
                    "text_modal" => true,
                    "image_layer" => false,
                    _ => null
                };


                if (obj is not null)
                    queue.Enqueue((obj, sent.CustomId));


                //for (int i = 0; i < queue.Count; i++)
                //{

                if (_property is null)
                    return;

                var a = queue.Dequeue();

                _property = a.Name switch
                {
                    "properties_modal" => SetValueToProperties<PropertiesModal>(_property, a.Type),
                    "text_modal" => SetValueToProperties<TextModal>(_property, a.Type),
                    "orientation_selec" => SetValueToProperties<Gravity>(_property, a.Type),
                    "ptag_selec" => SetValueToProperties<ImageTags>(_property, a.Type),
                    "image_layer" => SetValueToProperties<Layer>(_property, a.Type),
                    _ => null
                };
                //}

                if (sent.IsFinished)
                    token.Cancel();
            }
        }

        private static Topping SetValueToProperties<T>(Topping topping, object obj) where T : new()
        {
            var properties = (T)Convert.ChangeType(obj, typeof(T));

            var fields = topping.GetType().GetFields();
            foreach (var field in fields)
            {
                if (field.FieldType != typeof(T)) 
                    continue;

                field.SetValue(topping, properties);
                break;
            }
            return topping;
        }

        [SuppressMessage("ReSharper", "FieldCanBeMadeReadOnly.Local")]
        private class Topping
        {
            public PropertiesModal Props = new();
            public TextModal Text = new();
            public Gravity Orientation = Gravity.West;
            public ImageTags Tags = ImageTags.Any;
            public Layer Layer = Layer.Base;
        }
        
        [ModalInteraction("properties_modal:*,*", true)]
        public async Task HandlePropertiesModal(string user, string msg, PropertiesModal modal)
        {
            MessageComponent component;

            var wSuccess = int.TryParse(modal.Width, out var width);
            var hSuccess = int.TryParse(modal.Height, out var height);
            var xSuccess = int.TryParse(modal.X, out var x);
            var ySuccess = int.TryParse(modal.Y, out var y);

            bool invalid = false;

            string errorMsg = string.Empty;

            if (!wSuccess || !hSuccess || !xSuccess || !ySuccess)
            {
                errorMsg = "Width, Height, X and Y fields must be natural numbers (1, 64, 500...)\n";

                invalid = true;
            }

            if (width < 1 || height < 1)
            {
                if(invalid)
                {
                    errorMsg += "Width and height cannot be negative numbers or zero\n";
                }
                else
                    errorMsg = "Width and height cannot be negative numbers or zero\n";

                invalid = true;
            }

            if (x < 0 || y < 0)
            {
                if (invalid)
                {
                    errorMsg += "X and Y cannot be negative numbers\n";
                }
                else
                    errorMsg = "X and Y cannot be negative numbers\n";

                invalid = true;
            }

            if (invalid)
            {
                var errors = errorMsg.Split('\n').ToList();
                errors.Remove(errors.Last());

                if (errors.Count > 1)
                {
                    errors[0] = string.Concat("- ", errors[0]);
                    errorMsg = string.Join("\n- ", errors);
                }

                var startover = new ButtonBuilder()
                    .WithCustomId($"button_startover:{user}")
                    .WithLabel("Gotcha")
                    .WithStyle(ButtonStyle.Danger);

                component = new ComponentBuilder()
                    .WithButton(startover)
                    .Build();

                var embed = new EmbedBuilder()
                    .WithAuthor("Toppings Creator")
                    .WithTitle("Invalid properties")
                    .WithDescription(errorMsg)
                    .WithColor(0xFF0000)
                    .Build();

                await RespondAsync(embed: embed, components: component);
                return;
            }

            var yes = new ButtonBuilder()
                .WithCustomId($"button_image:{user},{msg}")
                .WithLabel("Image")
                .WithStyle(ButtonStyle.Primary);

            var no = new ButtonBuilder()
                .WithCustomId($"button_text:{user},{msg}")
                .WithLabel("Text")
                .WithStyle(ButtonStyle.Primary);

            component = new ComponentBuilder()
                .WithButton(yes)
                .WithButton(no)
                .Build();

            await _modal.OnMilkshakeSent(modal, new OnSentArgs { CustomId = "properties_modal", User = Context.Interaction.User});

            await RespondAsync("What is the type of the topping?", components: component);
        }

        [DoUserCheck]
        [ComponentInteraction("button_image:*,*", true)]
        public async Task HandleImageButton(string user, string msg)
        {
            var context = Context.Interaction as SocketMessageComponent;

            var foreground = new ButtonBuilder()
                .WithCustomId($"button_layer:{user},{msg},1")
                .WithLabel("Over")
                .WithStyle(ButtonStyle.Success);

            var middle = new ButtonBuilder()
                .WithCustomId($"button_layer:{user},{msg},0")
                .WithLabel("Base")
                .WithStyle(ButtonStyle.Success);

            var background = new ButtonBuilder()
                .WithCustomId($"button_layer:{user},{msg},-1")
                .WithLabel("Behind")
                .WithStyle(ButtonStyle.Success);

            var component = new ComponentBuilder()
                .WithButton(foreground)
                .WithButton(middle)
                .WithButton(background)
                .Build();

            await context.UpdateAsync(x =>
            {
                x.Content = "Where the image should relatively be?";
                x.Components = component;
            });
        }

        //[ComponentInteraction("button_over", true)]
        //public async Task HandleOverButton() => await HandleLayerButtons(1);
        //[ComponentInteraction("button_base", true)]
        //public async Task HandleBaseButton() => await HandleLayerButtons(0);
        //[ComponentInteraction("button_behind", true)]
        //public async Task HandleBehindButton() => await HandleLayerButtons(-1);

        [DoUserCheck]
        [ComponentInteraction("button_layer:*,*,*", true)]
        public async Task HandleLayerButtons(string user, string msg, string pos)
        {
            var context = Context.Interaction as SocketMessageComponent;

            var layer = (Layer)int.Parse(pos);
            await _modal.OnMilkshakeSent(layer, new OnSentArgs { CustomId = "image_layer", User = Context.Interaction.User, IsFinished = false});

            var menu = new SelectMenuBuilder()
                .WithPlaceholder("Select up to 3 tags")
                .WithCustomId($"ptag_selec:{user},{msg}")
                .WithMinValues(1)
                .WithMaxValues(7)
                .AddOption("Any", $"0", "Generic tag")
                .AddOption("Person", $"1", "Real or fictional, human or animal")
                .AddOption("Symbol", $"2", "Scripts, logos, flags")
                .AddOption("Object", $"4", "Inanimate things")
                .AddOption("Shitpost", $"8", "Memes")
                .AddOption("Picture", $"16", "Any real life photo or screenshot")
                .AddOption("Post", $"32", "Social media posts")
                .AddOption("Text", $"64", "Billboards, signs, etc.");

            var component = new ComponentBuilder()
                .WithSelectMenu(menu)
                .Build();

            await context.UpdateAsync(x =>
            {
                x.Content = "Select tags?";
                x.Components = component;
            });
        }

        [DoUserCheck]
        [ComponentInteraction("button_text:*,*", true)]
        public async Task HandleTextButton(string user, string msg)
        {
            var context = Context.Interaction as SocketMessageComponent;

            await context.Message.DeleteAsync();
            await RespondWithModalAsync<TextModal>($"text_modal:{user},{msg}");
        }
        
        [ModalInteraction("text_modal:*,*", true)]
        public async Task HandleTextModal(string user, string msg, TextModal modal)
        {
            modal.Font = modal.Font.Fallback("Arial");
            modal.Color = modal.Color.Fallback("#000000");
            modal.StrokeColor = modal.StrokeColor.Fallback("#000000");
            modal.StrokeWidth = modal.StrokeWidth.Fallback("0");
            
            await _modal.OnMilkshakeSent(modal, new OnSentArgs { CustomId = "text_modal", User = Context.Interaction.User});

            var menu = new SelectMenuBuilder()
                .WithPlaceholder("Select the text orientation (anchor)")
                .WithCustomId($"orientation_selec:{user},{msg}")
                .WithMinValues(1)
                .WithMaxValues(1)
                .AddOption("Top Left", $"1", "North-west", new Emoji("\u2196"))
                .AddOption("Top", $"2", "North", new Emoji("\u2B06"))
                .AddOption("Top Right", $"3", "North-east", new Emoji("\u2197"))
                .AddOption("Left", $"4", "West", new Emoji("\u2B05"))
                .AddOption("Center", $"5", "Center", new Emoji("\u2B55"))
                .AddOption("Right", $"6", "East", new Emoji("\u27A1"))
                .AddOption("Bottom Left", $"7", "South-west", new Emoji("\u2199"))
                .AddOption("Bottom", $"8", "South", new Emoji("\u2B07"))
                .AddOption("Bottom Right", $"9", "Southeast", new Emoji("\u2198"));

            var builder = new ComponentBuilder()
                .WithSelectMenu(menu)
                .Build();

            await RespondAsync(modal.Font, components: builder);
        }

        [DoUserCheck]
        [ComponentInteraction("orientation_selec:*,*", true)]
        public async Task HandleOrientationDropdown(string user, string msg, string orientationStr)
        {
            var context = Context.Interaction as SocketMessageComponent;

            await DeferAsync();
            await context!.DeleteOriginalResponseAsync();
            int orientation = int.Parse(orientationStr);

            var gravity = (Gravity)orientation;

            await _modal.OnMilkshakeSent(gravity,
                new OnSentArgs { CustomId = "orientation_selec", User = Context.Interaction.User, IsFinished = false });
            

            var menu = new SelectMenuBuilder()
                .WithPlaceholder("Select up to 3 tags")
                .WithCustomId($"ptag_selec:{user},{msg}")
                .WithMinValues(1)
                .WithMaxValues(7)
                .AddOption("Any", $"0", "Generic tag")
                .AddOption("Person", $"1", "Real or fictional, human or animal")
                .AddOption("Symbol", $"2", "Scripts, logos, flags")
                .AddOption("Object", $"4", "Inanimate things")
                .AddOption("Shitpost", $"8", "Memes")
                .AddOption("Picture", $"16", "Any real life photo or screenshot")
                .AddOption("Post", $"32", "Social media posts")
                .AddOption("Text", $"64", "Billboards, signs, etc.");

            var component = new ComponentBuilder()
                .WithSelectMenu(menu)
                .Build();

            await FollowupAsync("a", components: component);
        }

        [DoUserCheck]
        [ComponentInteraction("button_startover:*,*", true)]
        public async Task HandleStartOverButton(string user, string msg)
        {
            var context = Context.Interaction as SocketMessageComponent;

            await context.Message.DeleteAsync();
            await RespondWithModalAsync<PropertiesModal>($"properties_modal:{user},{msg}");
        }

        [DoUserCheck]
        [ComponentInteraction("ptag_selec:*,*", true)]
        public async Task HandleTagsDropdown(string user, string msg, string[] tagsStr)
        {
            var context = Context.Interaction as SocketMessageComponent;
            await DeferAsync();

            int flags = tagsStr.Aggregate(0, (current, item) => current | int.Parse(item));

            var tags = (ImageTags)flags;

            await _modal.OnMilkshakeSent(tags,
                new OnSentArgs { CustomId = "ptag_selec", User = Context.Interaction.User, IsFinished = true });

            await Context.Channel.DeleteMessageAsync(ulong.Parse(msg));
            await context!.DeleteOriginalResponseAsync();

            //await FollowupAsync("*at this point the template embed should be invoked back*");
        }

        [DoUserCheck]
        [ComponentInteraction("prop_delete:*", true)]
        public async Task HandlePropDelete(string user)
        {
            var context = Context.Interaction as SocketMessageComponent;

            var prevEmbed = context!.Message.Embeds.FirstOrDefault();

            var id = Guid.Parse(prevEmbed!.Footer!.Value.Text);

            var property = await _properties.GetMilkshake(id);

            var template = await _template.GetMilkshake(property.TemplateId);

            await _properties.DeleteMilkshake(id);

            var tags = new ButtonBuilder()
                .WithCustomId($"prop_tags:{user}")
                .WithLabel($"Change Tags")
                .WithStyle(ButtonStyle.Success)
                .WithDisabled(true);

            var edit = new ButtonBuilder()
                .WithCustomId($"prop_edit:{user}")
                .WithLabel($"Edit")
                .WithStyle(ButtonStyle.Primary)
                .WithDisabled(true);

            var delete = new ButtonBuilder()
                .WithCustomId($"prop_delete:{user}")
                .WithLabel("Eliminate")
                .WithStyle(ButtonStyle.Danger)
                .WithDisabled(true);

            var templateReturn = new ButtonBuilder()
                .WithCustomId($"template_return:{user}")
                .WithLabel($"Return to {template.Name}")
                .WithEmote(new Emoji("\u21AA"))
                .WithStyle(ButtonStyle.Secondary);

            var component = new ComponentBuilder()
                .WithButton(tags)
                .WithButton(edit)
                .WithButton(delete)
                .WithButton(templateReturn)
                .Build();

            await context.UpdateAsync(x => x.Components = component);
        }

        public class PropertiesModal : IModal
        {
            public string Title => "Toppings Creator";

            [RequiredInput(true)]
            [InputLabel("Name")]
            [ModalTextInput("prop_name", TextInputStyle.Short, "Nazubeans", 1, 16)]
            public string Name { get; set; }

            //[RequiredInput(false)]
            //[InputLabel("Description")]
            //[ModalTextInput("prop_description", TextInputStyle.Paragraph, "Nazubeans is absolutely awesome!", 1, 64)]
            //public string? Description { get; set; }

            [RequiredInput(true)]
            [InputLabel("X Anchor")]
            [ModalTextInput("prop_x", TextInputStyle.Short, "200", 1, 64)]
            public string? X { get; set; }

            [RequiredInput(true)]
            [InputLabel("Y Anchor")]
            [ModalTextInput("prop_y", TextInputStyle.Short, "200", 1, 64)]
            public string? Y { get; set; }

            [RequiredInput(true)]
            [InputLabel("Width")]
            [ModalTextInput("prop_width", TextInputStyle.Short, "250", 1, 4)]
            public string? Width { get; set; }

            [RequiredInput(true)]
            [InputLabel("Height")]
            [ModalTextInput("prop_height", TextInputStyle.Short, "100", 1, 4)]
            public string? Height { get; set; }
        }

        public class TextModal : IModal
        {
            public string Title => "Text Toppings";

            [RequiredInput(false)]
            [InputLabel("Font")]
            [ModalTextInput("prop_font", TextInputStyle.Short, "Arial", 1, 32)]
            public string? Font { get; set; } = "Arial";

            [RequiredInput(false)]
            [InputLabel("Text Color")]
            [ModalTextInput("prop_color", TextInputStyle.Short, "#BC9A7F", 6, 7)]
            public string? Color { get; set; } = "#BC9A7F";

            [RequiredInput(false)]
            [InputLabel("Stroke Width")]
            [ModalTextInput("prop_swidth", TextInputStyle.Short, "1", 1, 1)]
            public string? StrokeWidth { get; set; } = "1";

            [RequiredInput(false)]
            [InputLabel("Stroke Color")]
            [ModalTextInput("prop_scolor", TextInputStyle.Short, "#5C4241", 6, 7)]
            public string? StrokeColor { get; set; } = "#5C4241";
        }

        public enum Choice
        {
            Add,
            Edit,
            Eliminate
        }
    }
}
