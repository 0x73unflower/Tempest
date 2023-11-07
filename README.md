# Tempest

A basic C# tool to execute queries against an MSSQL instance.

## Usage

```
Tempest | Your MSSQL Swiss Army Knife

Usage:

        -h | Prints the help dialogue
        -s | Specify the server e.g. 'dc01.corp1.com'
        -d | Specify the database e.g. 'master' (used by default)
        -u | Specify the user to authenticate as
        -p | Specify the password of the user authenticating as
        -q | Specify the query to execute against the server
        -p | Check for privilege escalation vectors. Possible options: 'impersonation'

Example Usage:

        Using Standard Security (Username + Password)
        .\Tempest -s 'dc01.corp1.com' -d 'master' -u 'sa' -p 'letmein123!' -q 'SELECT SYSTEM_USER;'

        Using Trusted Connection (Kerberos Authentication)
        .\Tempest -s 'dc01.corp1.com' -d 'master' -q 'SELECT SYSTEM_USER;'

        Check for Privilege Escalation]
        .\Tempest -s 'dc01.corp1.com' -d 'master' -p 'impersonation'
```
