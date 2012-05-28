# DtaSpy
Standalone access (as in no BizTalk installation neccessary) to tracked messages and properties in BizTalk tracking databases.

There's been several attempts at exporting tracked messages and properties from BizTalk DTA (Document Tracking and Administration) databases (see [3 ways of programmatically extracting a message body from the BizTalk tracking database](http://connectedthoughts.wordpress.com/2008/04/02/3-ways-of-programatically-extracting-a-message-body-from-the-biztalk-tracking-database/)). Until now there's been no project which did not have a dependency on BizTalk.

The official way of retrieving tracked messages is the [BizTalkOperations.GetTrackedMessage method](http://technet.microsoft.com/en-us/library/aa957609.aspx) but that requires a local BizTalk installation and it's not possible to copy the operations assembly since BizTalk depends on all sorts of COM+ support libraries and classes installed alongside BizTalk.

## What can it do?

 * Retrieve and decode message parts (most notably the message body) [STABLE]
 * Retrieve and decode message contexts (properties) [BETA]
 * Serialize and fragment custom messages (write-support) [EXPERIMENTAL]
 * Serialize custom message contexts [EXPERIMENTAL]

## How does it work?
All the stored procedures and tables necessary to retrieve messages and properties are available in the DTA databases. The problem is that the data returned from theese procs aren't exactly in plain text. The data may or may not be compressed and if the tracked data is large enough it may be split over several fragments.

By reverse-engineering the storage format DtaSpy is able to decode tracked messages without the need for BizTalk.Operations or other BizTalk components. All it needs is System.Data and SharpZipLib.

## Disclaimer
I have never had access to a BizTalk installation. I was approached by a friend who works extensively with BizTalk who asked me to reverse engineer the data in the tracking database. 
He sent me a copy of the tracked data as well as the tracked message in its original format and I worked my way back from there. So while I've accumulated some knowledge about the data formats in DTA databases I'm still a complete idiot when it comes to BizTalk in general. Don't expect any help there :)

The usual disclaimers apply as well, although it won't mess up anything (since it's read-only for now) your mileage may vary as you may encounter corner-cases. Reading of message contexts is, for example, still a bit experimental. If you encounter any bugs of odd behavior please file an [issue](https://github.com/markus-olsson/DtaSpy/issues).

## Documentation
A bit thin at the moment. Check [the wiki](https://github.com/markus-olsson/DtaSpy/wiki) for updates.

There's a sample project included in the solution called DtaSpy.Samples.Export. It's a console application that exports message parts and contexts. It's very much a quick hack to demonstrate the library but it's a good place to start.

### API Sample

    var db = new BizTalkTrackingDb(connectionString);
    var message = db.LoadTrackedMessages().Take(10);
    
    foreach(var message in messages)
    {
        using (var fs = File.OpenWrite(message.MessageId + ".raw"))
            message.BodyPart.WriteTo(fs, false);
    }

## LICENSE
DtaSpy is licensed under the [MIT License](https://github.com/markus-olsson/DtaSpy/blob/master/LICENSE.txt) ([OSI](http://www.opensource.org/licenses/mit-license.php)). Basically you're free to do whatever you want with it. Attribution not necessary but appreciated.

## Dependencies
DtaSpy currently only depends on [SharpZipLib](http://www.icsharpcode.net/opensource/sharpziplib/). DtaSpy resolves this reference using NuGet and the project is set up to use NuGet package restore.

DtaSpy was originally written in .NET 4.0 but since there was so little work being done with .NET 4 specific stuff I backported it to .NET 2.0. It still runs fine on 4.0 though.
