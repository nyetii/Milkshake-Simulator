using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kimi.Services.Models;
using Milkshake.Models;
using Milkshake.Models.Interfaces;

namespace Kimi.Services.CRUD
{
    public class MediaCrud<T> where T : class
    {
        private readonly ApplicationDbContext _context;

        public MediaCrud(ApplicationDbContext context)
        {
            _context = context;
        }

        public virtual async Task<MediaCrud<T>> Add(T media)
        {
            await _context.Set<T>().AddAsync(media);
            await _context.SaveChangesAsync();
            return this;
        }
    }
}
