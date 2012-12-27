using System;
using System.Collections.Generic;
using System.Configuration;

namespace CreateSampleData
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                Console.WriteLine("This application creates MongoDB collection with sample data that can be exposed using MongOData service.");
                Console.WriteLine("MongOData supports OData protocol version 3, however not all OData client tools support it.");
                Console.WriteLine("Do you want to create sample data that require OData v.3? (y/n or Enter to support most commonly used v.2 protocol): ");
                var response = Console.ReadLine().ToLower();
                int protocolVersion = (response.ToLower() == "y" || response == "yes") ? 3 : 2;
                var connectionString = ConfigurationManager.ConnectionStrings["MongoDB"].ConnectionString;
                Console.WriteLine("Creating sample MongoDB database at {0}...", connectionString);
                var database = Database.Create(protocolVersion, connectionString);
                Console.WriteLine("Populating with Categories/Products samples...");
                database.PopulateWithCategoriesAndProducts();
                if (protocolVersion == 3)
                {
                    Console.WriteLine("Populating with JSON samples...");
                    database.PopulateWithJsonSamples();
                }
                Console.WriteLine("Successfully created sample database.");
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error: {0}", exception.Message);
            }
        }
    }
}
