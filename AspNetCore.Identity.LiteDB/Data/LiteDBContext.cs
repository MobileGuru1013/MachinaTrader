using LiteDB;
using Microsoft.AspNetCore.Hosting;

namespace AspNetCore.Identity.LiteDB.Data
{
    public class LiteDbContext : ILiteDbContext
    {
        private IHostingEnvironment HostingEnvironment { get; set; }
        public LiteDatabase LiteDatabase { get; set; }

        public LiteDbContext(IHostingEnvironment environment, string fileName)
        {
            HostingEnvironment = environment;
            LiteDatabase = new LiteDatabase(fileName);
        }
    }
}
