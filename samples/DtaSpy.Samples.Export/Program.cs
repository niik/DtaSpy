using System;
using System.IO;
using System.Text;
using System.Xml;

namespace DtaSpy.Samples.Export
{
    public static class Program
    {
        private static BizTalkTrackingDb db;

        private static ExportOptions options;

        public static int Main(string[] args)
        {
            options = new ExportOptions();

            if (!options.Parse(args))
                return -1;
            
            if (options.Help || args.Length == 0)
            {
                Console.Error.WriteLine("DtaSpy Export Sample");
                return options.PrintCommandLineHelp("DtaSpy.Samples.Export.exe [options]");
            }

            Debug("Using connection string " + options.ConnectionString);

            db = new BizTalkTrackingDb(options.ConnectionString);

            if (options.ExportAll)
            {
                foreach (var message in db.LoadTrackedMessages())
                {
                    Export(message, options.ExportContext);
                }
            }
            else
            {
                if (options.MessageId == Guid.Empty)
                    return options.PrintCommandLineHelp("Must specify either message id (--message-id) or --all");

                var message = db.LoadTrackedMessage(options.MessageId);

                if (message == null)
                {
                    Console.Error.WriteLine("No message found for id " + options.MessageId);
                    return -1;
                }

                Export(message, options.ExportContext);
            }

            return 0;
        }

        private static void Debug(string message)
        {
            if (!options.PrintDebugInfo)
                return;

            Console.Error.WriteLine(message);
        }

        private static void Export(BizTalkTrackedMessage message, bool exportContext)
        {
            ExportMessage(message);

            if (exportContext)
                ExportContext(message);
        }

        private static void ExportMessage(BizTalkTrackedMessage message)
        {
            int partNo = 1;
            foreach (var part in message.Parts)
            {
                Console.Error.WriteLine("Exporting {" + message.MessageId + "} part " + partNo + " of " + message.PartCount);

                using (var fs = File.OpenWrite(GetMessagePartExportFilename(message, part)))
                    part.WriteTo(fs, false);

                partNo++;
            }
        }

        /// <summary>
        /// Attempts to produce the same context xml format as the built-in tooling in BizTalk. Note
        /// that it is not complete and highly experimental.
        /// </summary>
        private static void ExportContext(BizTalkTrackedMessage message)
        {
            Console.Error.WriteLine("Exporting {" + message.MessageId + "} context");

            var context = db.LoadTrackedMessageContext(message.MessageId, message.SpoolId);

            using (var writer = new XmlTextWriter(GetMessageContextExportFilename(message), Encoding.UTF8))
            {
                writer.Formatting = Formatting.Indented;

                writer.WriteStartElement("MessageInfo");

                writer.WriteStartElement("ContextInfo");
                writer.WriteAttributeString("PropertiesCount", context.Properties.Count.ToString());

                foreach (var property in context.Properties)
                {
                    var arr = property.Value as Array;
                    if (arr != null)
                    {
                        writer.WriteStartElement("ArrayProperty");
                        writer.WriteAttributeString("Name", property.Name);
                        writer.WriteAttributeString("Namespace", property.Namespace);

                        for (int i = 0; i < arr.Length; i++)
                        {
                            writer.WriteStartElement("ArrayElement" + (i + 1));
                            writer.WriteAttributeString("Value", arr.GetValue(i).ToString());

                            writer.WriteEndElement();
                        }

                        writer.WriteEndElement();
                    }
                    else
                    {
                        writer.WriteStartElement("Property");

                        writer.WriteAttributeString("Name", property.Name);
                        writer.WriteAttributeString("Namespace", property.Namespace);
                        writer.WriteAttributeString("Value", property.Value.ToString());

                        writer.WriteEndElement();
                    }
                }

                writer.WriteEndElement();

                writer.WriteStartElement("PartInfo");
                writer.WriteAttributeString("PartsCount", message.PartCount.ToString());

                foreach (var part in message.Parts)
                {
                    writer.WriteStartElement("MessagePart");

                    writer.WriteAttributeString("ID", "{" + part.PartId + "}");
                    writer.WriteAttributeString("Name", part.PartName);
                    writer.WriteAttributeString("FileName", Path.GetFullPath(GetMessagePartExportFilename(message, part)));

                    writer.WriteAttributeString("Charset", "");
                    writer.WriteAttributeString("ContentType", "");

                    writer.WriteEndElement();
                }

                writer.WriteEndElement();

                writer.WriteEndElement();
            }
        }

        private static string GetMessagePartExportFilename(BizTalkTrackedMessage message, BizTalkTrackedMessagePart part)
        {
            return "{" + message.MessageId + "}_{" + part.PartId + "}" + (part.PartName == "" ? "" : "_" + part.PartName) + ".out";
        }

        private static string GetMessageContextExportFilename(BizTalkTrackedMessage message)
        {
            return "{" + message.MessageId + "}_context.xml";
        }
    }
}
