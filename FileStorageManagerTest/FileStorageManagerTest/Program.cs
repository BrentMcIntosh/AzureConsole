using log4net;
using Model;
using Model.Services;


namespace FileStorageManagerTest
{
    class Program
    {
        static void Main()
        {

            var folderRoot = @"C:\development\CHDAV2\Web\";

            var cloudStorage = true;

            log4net.Config.BasicConfigurator.Configure();

            var log = LogManager.GetLogger(typeof(Program));

            var fileStorageManager = new FileStorageManager(folderRoot, cloudStorage, log);

            var doc = new Document
            {
                IsAzure = true,
                FileName = "test.txt"
            };

            fileStorageManager.Exists()


        }
    }
}
