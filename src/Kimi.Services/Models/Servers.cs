using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Milkshake.Models;

namespace Kimi.Services.Models
{
    public class Servers
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public ulong Id { get; set; }
        public string Name { get; set; }

        public Guid? MilkshakeContextId { get; set; }
        public MilkshakeInstance? Milkshake { get; set; }
    }
}
