using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF.Tests
{
    internal class Program
    {
        public static async Task Main(string[] args)
        {
            try
            {
                TestClient client = new("127.0.0.1", 8000);
                client.Connect();
                await client.StartBombing(10, 0.05f);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }
    }
}
