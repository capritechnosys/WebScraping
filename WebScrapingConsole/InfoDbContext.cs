using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebScrapingConsole
{
    public class InfoDbContext : DbContext
    {
        public InfoDbContext() : base("DefaultConnection")
        {

        }

        public DbSet<Info> Info { get; set; }
    }
}
