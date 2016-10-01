using System.Configuration;
using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.IO;

namespace Bardock.Flowont.Tests
{
    public class TicketsContext : DbContext
    {
        public TicketsContext(DbConnection connection)
            : base(connection, true) { }

        public TicketsContext()
            : base("DataContext") { }

        public static DbConnection EffortConnection()
        {
            var path = Directory.GetCurrentDirectory() + "\\" + ConfigurationManager.AppSettings["Data.Path"];
            var loader = new Effort.DataLoaders.CsvDataLoader(path);
            return Effort.DbConnectionFactory.CreateTransient(loader);
        }

        public DbSet<Ticket> Tickets { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();
            modelBuilder.Properties<string>().Configure(p => p.IsUnicode(false));
        }
    }
}