using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.Interactions.Builders;
using Discord.WebSocket;
using Kimi.Services.Helpers;

namespace Kimi.Commands.Modules.Milkshake
{
    public class HelpContext : InteractionModuleBase<SocketInteractionContext>
    {
        //private readonly HelpModule _help;

        //public HelpContext(SocketInteractionContext context)
        //{
        //    _help = new HelpModule(context);
        //}

        [ComponentInteraction("help_template:*,*", true)]
        public async Task HandleTemplateHelp(string user, string button = "true")
        {
            var help = new HelpModule(Context);
            await help.HandleTemplateHelp(user, button);
        }

        [ComponentInteraction("help_tags:*,*", true)]
        public async Task HandleTagsHelp(string user, string button = "true")
        {
            var help = new HelpModule(Context);
            await help.HandleTagsHelp(user, button);
        }

        [ComponentInteraction("help_source:*,*", true)]
        public async Task HandleSourceHelp(string user, string button = "true")
        {
            var help = new HelpModule(Context);
            await help.HandleSourceHelp(user, button);
        }

        [ComponentInteraction("help_topping:*,*", true)]
        public async Task HandleToppingHelp(string user, string button = "true")
        {
            var help = new HelpModule(Context);
            await help.HandleToppingHelp(user, button);
        }

        [ComponentInteraction("help_text:*,*", true)]
        public async Task HandleTextHelp(string user, string button = "true")
        {
            var help = new HelpModule(Context);
            await help.HandleTextHelp(user, button);
        }

        [ComponentInteraction("help_image:*,*", true)]
        public async Task HandleImageHelp(string user, string button = "true")
        {
            var help = new HelpModule(Context);
            await help.HandleImageHelp(user, button);
        }
    }

    public class HelpModule
    {
        private readonly SocketInteractionContext _context;

        public HelpModule(SocketInteractionContext context)
        {
            _context = context;
        }

        private static EmbedBuilder HelpEmbedHelper()
        {
            var author = new EmbedAuthorBuilder()
                .WithName("Milkshake comes in clutch!")
                .WithIconUrl("https://cdn.discordapp.com/emojis/783328274193448981.webp");

            return new EmbedBuilder()
                .WithColor(0xf1c3c7)
                .WithAuthor(author);
        }

        //[DoUserCheck]
        //[ComponentInteraction("help_template:*,*", true)]
        public async Task HandleTemplateHelp(string user, string button = "true")
        {
            var context = _context.Interaction as SocketMessageComponent;

            var isButton = bool.Parse(button);

            if(!isButton)
                await _context.Interaction.DeferAsync(true);

            var embed = HelpEmbedHelper()
                .WithTitle("Template")
                .WithDescription("A Template is the base of the generated shitpost, " +
                                 "which will be modified by Sources based on its Toppings.")
                .Build();

            var topping = new ButtonBuilder()
                .WithCustomId($"help_topping:{user},true")
                .WithLabel("What is a Topping?")
                .WithStyle(ButtonStyle.Primary);

            var source = new ButtonBuilder()
                .WithCustomId($"help_source:{user},true")
                .WithLabel("What is a Source?")
                .WithStyle(ButtonStyle.Primary);

            var component = new ComponentBuilder()
                .WithButton(topping)
                .WithButton(source)
                .Build();

            if(isButton)
                await context!.UpdateAsync(x =>
                {
                    x.Embed = embed;
                    x.Components = component;
                });
            else
                await _context.Interaction.FollowupAsync(embed: embed, components: component);
        }

        //[DoUserCheck]
        //[ComponentInteraction("help_tags:*,*", true)]
        public async Task HandleTagsHelp(string user, string button = "true")
        {
            var context = _context.Interaction as SocketMessageComponent;

            const string description = """
            Tags are categories for Toppings and Sources. They are used for linking both *Milkshakes* within the same context.
            The use of tags is not required, although they may potentially make the generations have more sense, albeit less random.
            ## Tags
            ### Any
            It is the generic and default tag. It doesn't have any special meaning.
            ### Person
            Self explanatory, not necessarily exclusive to humans.
            ### Symbol
            Self explanatory, flags and logos should also be included.
            ### Object
            Inanimate things.
            ### Shitpost
            Self explanatory, any nonsensical content should also be included.
            ### Picture
            Any real life photo or screenshot. Posts and Text tags should *not* be included.
            ### Post
            Social media screenshots, news or Wikipedia.
            ### Text
            Billboards, signs, etc.
            """;

            var embed = HelpEmbedHelper()
                .WithTitle("Tags")
                .WithDescription(description)
                .Build();

            
            var template = new ButtonBuilder()
                .WithCustomId($"help_template:{user},true")
                .WithLabel("What is a Template?")
                .WithStyle(ButtonStyle.Primary);

            var topping = new ButtonBuilder()
                .WithCustomId($"help_topping:{user},true")
                .WithLabel("What is a Topping?")
                .WithStyle(ButtonStyle.Primary);

            var source = new ButtonBuilder()
                .WithCustomId($"help_source:{user},true")
                .WithLabel("What is a Source?")
                .WithStyle(ButtonStyle.Primary);
            

            var component = new ComponentBuilder()
                .WithButton(template)
                .WithButton(topping)
                .WithButton(source)
                .Build();

            await context!.UpdateAsync(x =>
            {
                x.Embed = embed;
                x.Components = component;
            });
        }

        // Source section

        //[DoUserCheck]
        //[ComponentInteraction("help_source:*,*", true)]
        public async Task HandleSourceHelp(string user, string button = "true")
        {

            var context = _context.Interaction as SocketMessageComponent;

            var isButton = bool.Parse(button);

            if (!isButton)
                await _context.Interaction.DeferAsync(true);

            const string description = """
            A Source is any image sent by the user with the purpose of filling the Template's Toppings.
            In other words, Sources are the randomly selected images on each meme.

            Just like Toppings, a Source can have tags as well.
            """;

            var embed = HelpEmbedHelper()
                .WithTitle("Source")
                .WithDescription(description)
                .Build();


            var template = new ButtonBuilder()
                .WithCustomId($"help_template:{user},true")
                .WithLabel("What is a Template?")
                .WithStyle(ButtonStyle.Primary);

            var topping = new ButtonBuilder()
                .WithCustomId($"help_topping:{user},true")
                .WithLabel("What is a Topping?")
                .WithStyle(ButtonStyle.Primary);

            var source = new ButtonBuilder()
                .WithCustomId($"help_source:{user},true")
                .WithLabel("What is a Source?")
                .WithStyle(ButtonStyle.Primary);


            var component = new ComponentBuilder()
                .WithButton(template)
                .WithButton(topping)
                .WithButton(source)
                .Build();

            if (isButton)
                await context!.UpdateAsync(x =>
                {
                    x.Embed = embed;
                    x.Components = component;
                });
            else
                await _context.Interaction.FollowupAsync(embed: embed, components: component);
        }

        // Toppings section

        //[DoUserCheck]
        //[ComponentInteraction("help_topping:*,*", true)]
        public async Task HandleToppingHelp(string user, string button = "true")
        {
            var context = _context.Interaction as SocketMessageComponent;

            var isButton = bool.Parse(button);

            if (!isButton)
                await _context.Interaction.DeferAsync(true);

            const string description = """
            Toppings are the characteristics a Source must have on a Template, including, but not limited to:
            - Tags
            - Size
            - If it is a text, and in that case:
             - Font
             - Orientation (alignment)
            ### Every topping must have the following properties:
            1. Position (x, y). They always are the top-left coordinate for the generated Source.
            2. Size (width, height).
            ### Fun fact
            Different Toppings can be connected when they have the same name. This way you can make Image and Text Toppings share the same source.
            """;

            var embed = HelpEmbedHelper()
                .WithTitle("Toppings")
                .WithDescription(description)
                .Build();
            

            var text = new ButtonBuilder()
                .WithCustomId($"help_text:{user},true")
                .WithLabel("About Text Toppings")
                .WithStyle(ButtonStyle.Success);

            var image = new ButtonBuilder()
                .WithCustomId($"help_image:{user},true")
                .WithLabel("About Image Toppings")
                .WithStyle(ButtonStyle.Success);

            var tags = new ButtonBuilder()
                .WithCustomId($"help_tags:{user},true")
                .WithLabel("About Tags")
                .WithStyle(ButtonStyle.Success);


            var template = new ButtonBuilder()
                .WithCustomId($"help_template:{user},true")
                .WithLabel("What is a Template?")
                .WithStyle(ButtonStyle.Primary);

            var source = new ButtonBuilder()
                .WithCustomId($"help_source:{user},true")
                .WithLabel("What is a Source?")
                .WithStyle(ButtonStyle.Primary);

            var row = new ActionRowBuilder()
                .WithButton(template)
                .WithButton(source);

            var component = new ComponentBuilder()
                .WithButton(text)
                .WithButton(image)
                .WithButton(tags)
                .AddRow(row)
                .Build();

            if (isButton)
                await context!.UpdateAsync(x =>
                {
                    x.Embed = embed;
                    x.Components = component;
                });
            else
                await _context.Interaction.FollowupAsync(embed: embed, components: component);
        }

        //[DoUserCheck]
        //[ComponentInteraction("help_text:*,*", true)]
        public async Task HandleTextHelp(string user, string button = "true")
        {
            var context = _context.Interaction as SocketMessageComponent;

            const string description = """
            A Text Topping has several properties to help detailing the Template:
            ### Font
            *Default*: Arial
            ### Color
            *Default*: Black `#000000`
            ### Orientation (alignment)
            *Default*: Left
            ### Stroke Width
            *Default*: 0
            ### Stoke Color
            *Default*: Black `#000000`
            """;

            var embed = HelpEmbedHelper()
                .WithTitle("Text Toppings")
                .WithDescription(description)
                .Build();


            var image = new ButtonBuilder()
                .WithCustomId($"help_image:{user},true")
                .WithLabel("About Image Toppings")
                .WithStyle(ButtonStyle.Success);

            var tags = new ButtonBuilder()
                .WithCustomId($"help_tags:{user},true")
                .WithLabel("About Tags")
                .WithStyle(ButtonStyle.Success);

            var topping = new ButtonBuilder()
                .WithCustomId($"help_topping:{user},true")
                .WithLabel("Return to Toppings")
                .WithEmote(new Emoji("\u21AA"))
                .WithStyle(ButtonStyle.Success);


            var template = new ButtonBuilder()
                .WithCustomId($"help_template:{user},true")
                .WithLabel("What is a Template?")
                .WithStyle(ButtonStyle.Primary);

            var source = new ButtonBuilder()
                .WithCustomId($"help_source:{user},true")
                .WithLabel("What is a Source?")
                .WithStyle(ButtonStyle.Primary);

            var row = new ActionRowBuilder()
                .WithButton(template)
                .WithButton(source);

            var component = new ComponentBuilder()
                .WithButton(image)
                .WithButton(tags)
                .WithButton(topping)
                .AddRow(row)
                .Build();

            await context!.UpdateAsync(x =>
            {
                x.Embed = embed;
                x.Components = component;
            });
        }

        //[DoUserCheck]
        //[ComponentInteraction("help_image:*,*", true)]
        public async Task HandleImageHelp(string user, string button = "true")
        {
            var context = _context.Interaction as SocketMessageComponent;

            const string description = """
            A Image Topping is very simple to set up. The only extra property is the Layer selection.
            ## Layers
            ### Foreground
            On the generation, the Source will lay *over* the base image.
            ### Base
            On the generation, the Source will be pasted on the depicted area on the same layer as the base image.
            Effectively, there's no practical difference between the Base and Foreground layers, besides a Foreground Topping will always have be over Base Toppings.
            Text Toppings are always laid on the Base layer.
            ### Background
            On the generation, the Source will lay *under* the base image.
            That means it will only be visible on the final generation if the depicted area on the base image is transparent.
            """;

            var embed = HelpEmbedHelper()
                .WithTitle("Image Toppings")
                .WithDescription(description)
                .Build();


            var image = new ButtonBuilder()
                .WithCustomId($"help_text:{user},true")
                .WithLabel("About Text Toppings")
                .WithStyle(ButtonStyle.Success);

            var tags = new ButtonBuilder()
                .WithCustomId($"help_tags:{user},true")
                .WithLabel("About Tags")
                .WithStyle(ButtonStyle.Success);

            var topping = new ButtonBuilder()
                .WithCustomId($"help_topping:{user},true")
                .WithLabel("Return to Toppings")
                .WithEmote(new Emoji("\u21AA"))
                .WithStyle(ButtonStyle.Success);


            var template = new ButtonBuilder()
                .WithCustomId($"help_template:{user},true")
                .WithLabel("What is a Template?")
                .WithStyle(ButtonStyle.Primary);

            var source = new ButtonBuilder()
                .WithCustomId($"help_source:{user},true")
                .WithLabel("What is a Source?")
                .WithStyle(ButtonStyle.Primary);

            var row = new ActionRowBuilder()
                .WithButton(template)
                .WithButton(source);

            var component = new ComponentBuilder()
                .WithButton(image)
                .WithButton(tags)
                .WithButton(topping)
                .AddRow(row)
                .Build();

            await context!.UpdateAsync(x =>
            {
                x.Embed = embed;
                x.Components = component;
            });
        }

        public async Task HandleEmptyHelp()
        {
            await _context.Interaction.RespondAsync("This command for now doesn't have any definition.");
        }

        public async Task HandleUnknownHelp()
        {
            await _context.Interaction.RespondAsync("Unknown command.");
        }
    }
}
