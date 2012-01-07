using System;
using System.Configuration;
using System.Data.SqlClient;
using NDesk.Options;

namespace DtaSpy.Samples.Export
{
    public class ExportOptions
    {
        public static string DefaultConnectionString
        {
            get
            {
                var connectionString = ConfigurationManager.ConnectionStrings["default"];

                if (connectionString == null)
                    return null;

                return connectionString.ConnectionString;
            }
        }

        private OptionSet optionSet;
        private SqlConnectionStringBuilder connectionStringBuilder;

        public bool Help { get; set; }
        public bool ExportAll { get; set; }
        public bool ExportContext { get; set; }
        public Guid MessageId { get; set; }
        public bool PrintDebugInfo { get; set; }
        public string ConnectionString { get; set; }

        public ExportOptions()
        {
            this.connectionStringBuilder = new SqlConnectionStringBuilder(DefaultConnectionString ?? string.Empty);

            // If there's no connection string in the config file we'll assume integrated
            // security. Mostly because we have no way of specifiying username and password
            // in the command options.
            if (string.IsNullOrEmpty(DefaultConnectionString))
                this.connectionStringBuilder.IntegratedSecurity = true;

            // Option names partly stolen from http://devlicio.us/blogs/rob_reynolds/archive/2009/11/22/command-line-parsing-with-mono-options.aspx
            this.optionSet = new OptionSet();

            optionSet.Add("?|help|h", "Prints out the options.", option => this.Help = option != null);
            optionSet.Add("debug", "Print debug info.", option => this.PrintDebugInfo = option != null);
            optionSet.Add("d=|db=|database=|databasename=", "The name of the DTA database you want to connect to.", option => this.connectionStringBuilder.InitialCatalog = option);
            optionSet.Add("s=|server=|servername=|instance=|instancename=", string.Format("The server and instance you would like to run on. (local) and (local)\\SQL2008 are both valid values. Defaults to \"{0}\".", this.connectionStringBuilder.DataSource), option => this.connectionStringBuilder.DataSource = option);
            optionSet.Add("c=|connectionstring=", "The connection string to use when connecting to the DTA database", option => this.connectionStringBuilder= new SqlConnectionStringBuilder(option));
            optionSet.Add("id=|message-id=", "The id of the message to export", option => this.MessageId = new Guid(option));
            optionSet.Add("context", "Export the message context as well (experimental)", option => this.ExportContext = option != null);
            optionSet.Add("all", "Export all messages in the tracking database", option => this.ExportAll = option != null);
        }

        public bool Parse(string[] args)
        {
            try
            {
                optionSet.Parse(args);
                this.ConnectionString = this.connectionStringBuilder.ConnectionString;
            }
            catch (OptionException)
            {
                PrintCommandLineHelp("Error - usage is:");
                return false;
            }

            return true;
        }

        public int PrintCommandLineHelp(string message)
        {
            Console.Error.WriteLine(message);
            this.optionSet.WriteOptionDescriptions(Console.Error);

            return -1;
        }
    }
}
