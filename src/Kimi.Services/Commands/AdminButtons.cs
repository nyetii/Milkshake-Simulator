using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Milkshake.Managers;
using Milkshake.Models;
using Milkshake.Models.Interfaces;

namespace Kimi.Services.Commands
{
    public static class AdminButtons
    {
        public static bool IsPermitted<T>(T milkshake, string vips, string caller) where T : IStats
        {
            var creator = new string(milkshake.Creator.Where(char.IsDigit).ToArray());

            return Permission.IsPermitted($"{vips};{creator}", caller);
        }
    }
}
