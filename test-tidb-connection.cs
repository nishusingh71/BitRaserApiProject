// Quick TiDB Connection Test
// Copy this code and run: dotnet run test-tidb-connection.cs

using MySql.Data.MySqlClient;
using System;

class TiDBConnectionTest
{
    static void Main()
    {
        Console.WriteLine("========================================");
        Console.WriteLine("  TiDB Connection Test");
        Console.WriteLine("========================================\n");

        // Connection string
        var connectionString = "Server=gateway01.ap-southeast-1.prod.aws.tidbcloud.com;" +
              "Port=4000;" +
       "Database=Cloud_Erase;" +
            "User=2tdeFNZMcsWKkDR.root;" +
         "Password=76wtaj1GZkg7Qhek;" +
        "AllowUserVariables=true;" +
          "SslMode=Required;";

        Console.WriteLine("📋 Connection Details:");
   Console.WriteLine("   Host: gateway01.ap-southeast-1.prod.aws.tidbcloud.com");
        Console.WriteLine("   Port: 4000");
        Console.WriteLine("   Database: Cloud_Erase");
        Console.WriteLine("   User: 2tdeFNZMcsWKkDR.root");
      Console.WriteLine("   SSL: Required\n");

        Console.WriteLine("🔌 Connecting to TiDB...");
        
  try
     {
    using (var connection = new MySqlConnection(connectionString))
    {
    var startTime = DateTime.Now;
      connection.Open();
     var elapsed = (DateTime.Now - startTime).TotalMilliseconds;

        Console.WriteLine("✅ CONNECTION SUCCESSFUL!\n");
   Console.WriteLine($"   Server Version: {connection.ServerVersion}");
       Console.WriteLine($"   Database: {connection.Database}");
                Console.WriteLine($"   Response Time: {elapsed:F0} ms");
        Console.WriteLine($"   State: {connection.State}");

    // Test query
          Console.WriteLine("\n📊 Running test query...");
           using (var command = connection.CreateCommand())
                {
           command.CommandText = "SELECT VERSION(), DATABASE(), USER();";
            using (var reader = command.ExecuteReader())
         {
   if (reader.Read())
             {
     Console.WriteLine($"   Version: {reader.GetString(0)}");
             Console.WriteLine($"   Database: {reader.GetString(1)}");
      Console.WriteLine($"   User: {reader.GetString(2)}");
  }
              }
           }

                // List existing tables
     Console.WriteLine("\n📁 Existing Tables:");
      using (var command = connection.CreateCommand())
 {
     command.CommandText = "SHOW TABLES;";
              using (var reader = command.ExecuteReader())
         {
  int count = 0;
           while (reader.Read())
              {
    Console.WriteLine($"   - {reader.GetString(0)}");
        count++;
  }
   if (count == 0)
   {
         Console.WriteLine("   (No tables found)");
}
          }
   }

   connection.Close();
     Console.WriteLine("\n✅ All tests passed!");
            }
        }
        catch (MySqlException ex)
    {
  Console.WriteLine("\n❌ MySQL ERROR!");
      Console.WriteLine($"   Error Code: {ex.Number}");
     Console.WriteLine($"   Error: {ex.Message}");
        Console.WriteLine($"   SQL State: {ex.SqlState}");
            
            // Common errors
            switch (ex.Number)
    {
         case 1045:
         Console.WriteLine("\n💡 Troubleshooting:");
       Console.WriteLine("   - Verify username and password");
  Console.WriteLine("   - Check if user has access to this database");
     break;
     case 2003:
                    Console.WriteLine("\n💡 Troubleshooting:");
     Console.WriteLine("   - Check if host is reachable");
        Console.WriteLine("   - Verify port number (4000)");
        Console.WriteLine("   - Check firewall settings");
    break;
    case 1049:
        Console.WriteLine("\n💡 Troubleshooting:");
              Console.WriteLine("   - Database 'Cloud_Erase' does not exist");
                    Console.WriteLine("   - Check database name spelling");
                    break;
              default:
      Console.WriteLine($"\n💡 MySQL Error #{ex.Number}");
 break;
         }
        }
        catch (Exception ex)
      {
    Console.WriteLine("\n❌ GENERAL ERROR!");
          Console.WriteLine($"   Type: {ex.GetType().Name}");
       Console.WriteLine($"   Message: {ex.Message}");
            if (ex.InnerException != null)
 {
      Console.WriteLine($"   Inner: {ex.InnerException.Message}");
       }
        }

        Console.WriteLine("\n========================================");
  Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }
}
