using System;
using System.Data.SqlClient;

namespace Tempest {
    internal class Program {
        static void printHelp() {
            Console.WriteLine("\nTempest | Your MSSQL Swiss Army Knife\n\nUsage:\n\n\t" +
                        "-h | Prints the help dialogue\n\t" +
                        "-s | Specify the server e.g. 'dc01.corp1.com'\n\t" +
                        "-d | Specify the database e.g. 'master' (used by default)\n\t" +
                        "-u | Specify the user to authenticate as\n\t" +
                        "-p | Specify the password of the user authenticating as\n\t" +
                        "-q | Specify the query to execute against the server\n\n" +
                        "Example Usage:\n\n" +
                        "\tUsing Standard Security (Username + Password)\n\t.\\Tempest -s 'dc01.corp1.com' -d 'master' -u 'sa' -p 'letmein123!' -q 'SELECT SYSTEM_USER;'\n\n" +
                        "\tUsing Trusted Connection (Kerberos Authentication)\n\t.\\Tempest -s 'dc01.corp1.com' -d 'master' -q 'SELECT SYSTEM_USER;'\n");
            Environment.Exit(0);
        }
        static void execQuery(string sqlQuery, SqlConnection con) {
            try {
                SqlCommand command = new SqlCommand(sqlQuery, con);
                SqlDataReader exec = command.ExecuteReader();
                exec.Read();
                Console.WriteLine("[+] Query Output: \n" + exec[0]);
                exec.Close();
            }
            catch {
                Console.WriteLine("[!] Error: Failed to execute query");
            }
        }

        static SqlConnection connectDB(string sqlServer, string database, string sqlUser = "", string sqlPass = "") {
            string userRole = "";
            string conString = "";
            // Connect to the database
            if (sqlUser == "") {
                conString = "Server = " + sqlServer + "; Database = " + database + "; Integrated Security = True;";
            }
            else {
                conString = "Server = " + sqlServer + "; Database = " + database + "; User Id = " + sqlUser + "; Password = " + sqlPass + ";";
            }

            SqlConnection con = new SqlConnection(conString);

            // Check the connection status
            try {
                con.Open();
                SqlCommand command = new SqlCommand("SELECT IS_SRVROLEMEMBER('public');", con);
                SqlDataReader exec = command.ExecuteReader();
                exec.Read();
                Int32 role = Int32.Parse(exec[0].ToString());
                if (role == 1) {
                    userRole = "public";
                }
                else {
                    userRole = "sysadmin";
                }
                Console.WriteLine("[+] Connection: SUCCESS | Server: " + sqlServer + " | User Role: " + userRole);
                exec.Close();
            }
            catch {
                Console.WriteLine("[!] Connection: FAILURE");
                Environment.Exit(0);
            }
            return con;
        }

        static void Main(string[] args) {
            string sqlServer = "";
            string database = "";
            string sqlUser = "";
            string sqlPass = "";
            string sqlQuery = "";

            for (int i = 0; i < args.Length; i++) {
                if (args[i] == "-h") {
                    printHelp();
                }
                // Get the server
                if (args[i] == "-s") {
                    sqlServer = args[i + 1];
                }
                // Get the database, otherwise use master
                if (args[i] == "-d") {
                    database = args[i + 1];
                }
                // Get credentials for trusted connection
                if (args[i] == "-u") {
                    sqlUser = args[i + 1];
                }
                if (args[i] == "-p") {
                    sqlPass = args[i + 1];
                }
                // Get the query to execute against the server
                if (args[i] == "-q") {
                    sqlQuery = args[i + 1];
                }
            }

            // Connect to the database
            SqlConnection con = connectDB(sqlServer, database, sqlUser, sqlPass);

            // Execute a query against the server
            if (sqlQuery != "") {
                execQuery(sqlQuery, con);
            }

            con.Close();
            Environment.Exit(0);
        }
    }
}
