using MySql.Data.MySqlClient;

public class DatabaseInitializer
{
    private readonly string _connectionString;

    public DatabaseInitializer(string connectionString)
    {
        _connectionString = connectionString;
    }

    public void InitializeDatabase()
    {
        using var connection = new MySqlConnection(_connectionString);
        connection.Open();

        // Check if the database exists
        string dbCheckQuery = "SELECT SCHEMA_NAME FROM INFORMATION_SCHEMA.SCHEMATA WHERE SCHEMA_NAME = 'Cloud_Erase__App';";
        using var checkDbCommand = new MySqlCommand(dbCheckQuery, connection);
        var dbExists = checkDbCommand.ExecuteScalar() != null;

        if (!dbExists)
        {
            // Create the database if it doesn't exist
            string createDbQuery = @"CREATE DATABASE `Cloud_Erase__App` /*!40100 DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci */ /*!80016 DEFAULT ENCRYPTION='N' */;";
            using var createDbCommand = new MySqlCommand(createDbQuery, connection);
            createDbCommand.ExecuteNonQuery();
        }

        // Switch to the `Cloud_Erase__App` database
        string useDbQuery = "USE Cloud_Erase__App;";
        using var useDbCommand = new MySqlCommand(useDbQuery, connection);
        useDbCommand.ExecuteNonQuery();

        // Create the machines table

        //CreateTableIfNotExists(connection, "machines", @"
        //    CREATE TABLE `machines` (
        //        `machine_id` INT AUTO_INCREMENT PRIMARY KEY,
        //        `fingerprint_hash` VARCHAR(255) NOT NULL UNIQUE,
        //        `mac_address` VARCHAR(255) NOT NULL,
        //        `physical_drive_id` VARCHAR(255) NOT NULL,
        //        `cpu_id` VARCHAR(255) NOT NULL,
        //        `bios_serial` VARCHAR(255) NOT NULL,
        //        `vm_status` ENUM('physical', 'vm') NOT NULL,
        //        `os_version` VARCHAR(255) NOT NULL,
        //        `user_email` VARCHAR(255),
        //        `license_activated` BOOLEAN DEFAULT FALSE,
        //        `license_activation_date` TIMESTAMP NULL,
        //        `license_days_valid` INT DEFAULT 0,
        //        `license_details_json` JSON,
        //        `demo_usage_count` INT DEFAULT 0,
        //        `created_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
        //        `updated_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
        //    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
        //");

        CreateTableIfNotExists(connection, "machines", @"
            CREATE TABLE `machines` (
                `fingerprint_hash` VARCHAR(255) NOT NULL UNIQUE,
                `mac_address` VARCHAR(255) NOT NULL,
                `physical_drive_id` VARCHAR(255) NOT NULL,
                `cpu_id` VARCHAR(255) NOT NULL,
                `bios_serial` VARCHAR(255) NOT NULL,
                `os_version` VARCHAR(255) NOT NULL,
                `user_email` VARCHAR(255),
                `license_activation_date` TIMESTAMP NULL,
                `license_days_valid` INT DEFAULT 0,
                `license_details_json` JSON
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
        ");

        // Create the audit_reports table
        CreateTableIfNotExists(connection, "auditreports", @"
            CREATE TABLE `auditreports` (
                `report_id` INT AUTO_INCREMENT PRIMARY KEY,
                `client_email` VARCHAR(255) NOT NULL,
                `report_name` VARCHAR(255) NOT NULL,
                `erasure_method` VARCHAR(255) NOT NULL,
                `report_datetime` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                `report_details_json` JSON NOT NULL,
                `synced` BOOLEAN DEFAULT FALSE
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
        ");

        // Create the users table
        CreateTableIfNotExists(connection, "users", @"
            CREATE TABLE `users` (
                `user_id` INT AUTO_INCREMENT PRIMARY KEY,
                `user_name` VARCHAR(255) NOT NULL,
                `user_email` VARCHAR(255) NOT NULL UNIQUE,
                `user_password` VARCHAR(255) NOT NULL,
                `phone_number` VARCHAR(20),
                `payment_details_json` JSON,
                `license_details_json` JSON
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
        ");

        CreateTableIfNotExists(connection, "updates", @"
            CREATE TABLE `updates` (
                `version_id` INT PRIMARY KEY,
                `version_number` VARCHAR(20) NOT NULL UNIQUE,
                `changelog` TEXT NOT NULL,
                `download_link` VARCHAR(500) NOT NULL,
                `release_date` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                `is_mandatory_update` TINYINT(1) NOT NULL DEFAULT 0
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
        ");

    }

    private void CreateTableIfNotExists(MySqlConnection connection, string tableName, string createTableQuery)
    {
        string checkTableQuery = $"SHOW TABLES LIKE '{tableName}';";
        using var checkTableCommand = new MySqlCommand(checkTableQuery, connection);
        var tableExists = checkTableCommand.ExecuteScalar() != null;

        if (!tableExists)
        {
            using var createTableCommand = new MySqlCommand(createTableQuery, connection);
            createTableCommand.ExecuteNonQuery();
        }
    }
}
