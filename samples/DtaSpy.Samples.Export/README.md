# DtaSpy sample export program.

A sample export utility which attempts to replicate as closely as possible the output file names and formats of the official BizTalk tools.

## Usage and options

   (Option names partly stolen from http://devlicio.us/blogs/rob_reynolds/archive/2009/11/22/command-line-parsing-with-mono-options.aspx)

    DtaSpy.Samples.Export.exe [options]
      -?, --help, -h             Prints out the options.
          --debug                Print debug info.
      -d, --db, --database, --databasename=VALUE
                                 The name of the DTA database you want to connect
                                   to.
      -s, --server, --servername, --instance, --instancename=VALUE
                                 The server and instance you would like to run o-
                                   n. (local) and (local)\SQL2008 are both valid
                                   values. Defaults to ".\SQLEXPRESS".
      -c, --connectionstring=VALUE
                                 The connection string to use when connecting to
                                   the DTA database
          --id, --message-id=VALUE
                                 The id of the message to export
          --context              Export the message context as well (experimental)
          --all                  Export all messages in the tracking database

## Sample usage

### Export all messages in the local Dta db

    > DtaSpy.Samples.Export.exe -s ".\SQLEXPRESS" -d BizTalkDTADb --all
    Exporting {0a8735cf-1152-4ba4-801a-825acf8623a9} part 1 of 1

### Export all messages in the local Dta db including the message contexts

    > DtaSpy.Samples.Export.exe -s ".\SQLEXPRESS" -d BizTalkDTADb --all --context
    Exporting {0a8735cf-1152-4ba4-801a-825acf8623a9} part 1 of 1    
    Exporting {0a8735cf-1152-4ba4-801a-825acf8623a9} context


### Export a single message in the local Dta db

    > DtaSpy.Samples.Export.exe -s ".\SQLEXPRESS" -d BizTalkDTADb --id "{0a8735cf-1152-4ba4-801a-825acf8623a9}" --context
    Exporting {0a8735cf-1152-4ba4-801a-825acf8623a9} part 1 of 1    
    Exporting {0a8735cf-1152-4ba4-801a-825acf8623a9} context

## Configuration

It's possible to configure a default connection string in the the App.config file to avoid having to enter -c -d or -s every time you run the utility.
