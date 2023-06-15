using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Kimi.Services.Models;
using Milkshake.Managers;
using Milkshake.Models;
using Milkshake.Models.Interfaces;

namespace Kimi.Commands.Modules.Milkshake
{
    public partial class ActiveMilkshake
    {
        public ComponentBuilder PropAdminButtons(Template template, MilkshakeInstance? instance, string user)
        {
            var creator = new string(template.Creator.Where(char.IsDigit).ToArray());

            var disabled = !Permission.IsPermitted($"{instance!.Vips};{creator}", Context.Interaction.User.Id.ToString());

            var tags = new ButtonBuilder()
                .WithCustomId($"prop_tags:{user}")
                .WithLabel($"Change Tags")
                .WithStyle(ButtonStyle.Success)
                .WithDisabled(disabled);

            var edit = new ButtonBuilder()
                .WithCustomId($"prop_edit:{user}")
                .WithLabel($"Edit")
                .WithStyle(ButtonStyle.Primary)
                .WithDisabled(disabled);

            var delete = new ButtonBuilder()
                .WithCustomId($"prop_delete:{user}")
                .WithLabel("Eliminate")
                .WithStyle(ButtonStyle.Danger)
                .WithDisabled(disabled);

            return new ComponentBuilder()
                .WithButton(tags)
                .WithButton(edit)
                .WithButton(delete);
        }

        public ComponentBuilder TemplateButtons(global::Milkshake.Models.Topping[] props, Template template, MilkshakeInstance? instance, string user)
        {
            var creator = new string(template.Creator.Where(char.IsDigit).ToArray());

            var disabled = !Permission.IsPermitted($"{instance!.Vips};{creator}", Context.Interaction.User.Id.ToString());

            var properties = new ButtonBuilder()
                .WithCustomId($"template_properties:{user}")
                .WithLabel(props!.Length is 0 ? "No Toppings" : "Toppings")
                .WithStyle(props!.Length is 0 ? ButtonStyle.Secondary : ButtonStyle.Success)
                .WithDisabled(props!.Length is 0);

            var newProp = new ButtonBuilder()
                .WithCustomId($"prop_new:{user}")
                .WithLabel("New")
                .WithStyle(ButtonStyle.Success)
                .WithDisabled(disabled);

            var rename = new ButtonBuilder()
                .WithCustomId($"template_rename:{user}")
                .WithLabel("Edit")
                .WithStyle(ButtonStyle.Primary)
                .WithDisabled(disabled);

            var delete = new ButtonBuilder()
                .WithCustomId($"template_delete:{user}")
                .WithLabel("Eliminate")
                .WithStyle(ButtonStyle.Danger)
                .WithDisabled(disabled);

            return new ComponentBuilder()
                .WithButton(properties)
                .WithButton(newProp)
                .WithButton(rename)
                .WithButton(delete);
        }

        public ComponentBuilder SourceButtons(Source source, MilkshakeInstance? instance)
        {
            var creator = new string(source.Creator.Where(char.IsDigit).ToArray());

            var disabled = !Permission.IsPermitted($"{instance!.Vips};{creator}", Context.Interaction.User.Id.ToString());

            var changeTags = new ButtonBuilder()
                .WithCustomId($"source_tag:{Context.User.Id}")
                .WithLabel("Change tags")
                .WithStyle(ButtonStyle.Success)
                .WithDisabled(disabled);

            var rename = new ButtonBuilder()
                .WithCustomId($"source_rename:{Context.User.Id}")
                .WithLabel("Edit")
                .WithStyle(ButtonStyle.Primary)
                .WithDisabled(disabled);

            var delete = new ButtonBuilder()
                .WithCustomId($"source_delete:{Context.User.Id}")
                .WithLabel("Eliminate")
                .WithStyle(ButtonStyle.Danger)
                .WithDisabled(disabled);

            return new ComponentBuilder()
                .WithButton(changeTags)
                .WithButton(rename)
                .WithButton(delete);
        }
    }
}
