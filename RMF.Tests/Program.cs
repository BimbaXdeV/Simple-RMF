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
            TestClient client = new("127.0.0.1", 8000);
            client.Connect();

            try
            {
                await client.StartBombing(5, 0.001f);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
            finally
            {
                client.Disconnect();
            }
        }
    }
}
