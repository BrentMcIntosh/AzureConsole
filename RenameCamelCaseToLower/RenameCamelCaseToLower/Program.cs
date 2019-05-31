using Microsoft.Azure.WebJobs;

namespace RenameCamelCaseToLower
{
    class Program
    {
        static void Main()
        {
            var config = new JobHostConfiguration();

            var host = new JobHost(config);

            host.RunAndBlock();
        }
    }
}
