/*
 * Copyright (c) 2012 Markus Olsson
 * var mail = string.Join(".", new string[] {"j", "markus", "olsson"}) + string.Concat('@', "gmail.com");
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this 
 * software and associated documentation files (the "Software"), to deal in the Software without
 * restriction, including without limitation the rights to use, copy, modify, merge, publish, 
 * distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING 
 * BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, 
 * DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, 
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;

namespace DtaSpy
{
    /// <summary>
    /// Database wrapper for retrieval of tracked messages, parts, fragments and context.
    /// Normally you'd use an instance of this object to retrieve one or more messages and
    /// then use the message instances to access all related objects (parts, fragments and contexts).
    /// For all methods to work the connection needs to have EXECUTE permissions for the following
    /// stored procedures in the BizTalk DTA database: ops_LoadTrackedMessages, ops_LoadTrackedPartByID,
    /// ops_LoadTrackedParts, ops_LoadTrackedMessageContext and ops_LoadTrackedPartFragment.
    /// </summary>
    public class BizTalkTrackingDb : IDisposable
    {
        /// <summary>
        /// Gets the connection string to use when creating connections to the BizTalk DTA database.
        /// </summary>
        protected string ConnectionString { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BizTalkTrackingDb"/> class with
        /// which you can retrieve tracked messages, parts, fragments and contexts.
        /// </summary>
        /// <param name="connectionString">The connection string to the BizTalk DTA database.</param>
        public BizTalkTrackingDb(string connectionString)
        {
            this.ConnectionString = connectionString;
        }

        /// <summary>
        /// Loads all tracked messages in the current database (in both pools).
        /// </summary>
        public IEnumerable<BizTalkTrackedMessage> LoadTrackedMessages()
        {
            foreach (var message in LoadTrackedMessages(1))
                yield return message;

            foreach (var message in LoadTrackedMessages(2))
                yield return message;
        }

        /// <summary>
        /// Loads all tracked messages in the given spool.
        /// </summary>
        /// <param name="spoolId">The spool id.</param>
        public IEnumerable<BizTalkTrackedMessage> LoadTrackedMessages(int spoolId)
        {
            if (spoolId < 1 || spoolId > 2)
                throw new ArgumentOutOfRangeException("spoolId", "must be 1 or 2");

            // Guid.Empty is the key here
            return LoadTrackedMessages(Guid.Empty, spoolId);
        }

        /// <summary>
        /// Attempts to load the tracked message regardless of which spool it resides in by
        /// first testing spool 1 then spool 2. Returns null if no message was found.
        /// </summary>
        /// <param name="messageId">The message id.</param>
        public BizTalkTrackedMessage LoadTrackedMessage(Guid messageId)
        {
            return LoadTrackedMessage(messageId, 1) ?? LoadTrackedMessage(messageId, 2);
        }

        /// <summary>
        /// Attempts to load the tracked message regardless from the given spool. 
        /// Returns null if no message was found.
        /// </summary>
        /// <param name="messageId">The message id.</param>
        /// <param name="spoolId">The spool id.</param>
        public BizTalkTrackedMessage LoadTrackedMessage(Guid messageId, int spoolId)
        {
            if (spoolId < 1 || spoolId > 2)
                throw new ArgumentOutOfRangeException("spoolId", "must be 1 or 2");

            if (messageId == Guid.Empty)
                throw new ArgumentException("Message Id cannot be Guid.Empty; use LoadTrackedMessages to load all messages", "messageId");

            BizTalkTrackedMessage message = null;

            foreach (var m in LoadTrackedMessages(messageId, spoolId))
            {
                if (message != null)
                    throw new InvalidOperationException("Got more than one message for guid");

                message = m;
            }

            return message;
        }

        /// <summary>
        /// Internal message access method; accepts Guid.Empty
        /// </summary>
        private IEnumerable<BizTalkTrackedMessage> LoadTrackedMessages(Guid messageGuid, int spoolId)
        {
            using (var connection = new SqlConnection(this.ConnectionString))
            using (var cmd = CreateStoredProcedureCommand(connection, "ops_LoadTrackedMessages"))
            {
                AddInParameter(cmd, "@uidMessageID", SqlDbType.UniqueIdentifier, messageGuid);
                AddInParameter(cmd, "@nSpoolID", SqlDbType.Int, spoolId);

                connection.Open();

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                        yield return DeserializeMessageFromRecord(reader, spoolId);
                }
            }
        }

        /// <summary>
        /// Given a data record; tranform that record into a message.
        /// </summary>
        protected BizTalkTrackedMessage DeserializeMessageFromRecord(IDataRecord record, int spoolId)
        {
            //// ? uidMsgID                             ?    nNumParts uidBodyPartID
            //// 1 354AD565-9816-4D58-B55F-0B0A65792B1D NULL 1         2B38B8C9-4A4F-4343-8DE7-6549AECD7D4B

            var message = new BizTalkTrackedMessage(this, spoolId);

            message.MessageId = record.GetGuid(1);
            message.PartCount = record.GetInt32(3);
            message.BodyPartId = record.GetGuid(4);

            return message;
        }

        /// <summary>
        /// Loads the tracked part. Returns null if no such part was found in the specified spool.
        /// </summary>
        /// <param name="messageId">The message id.</param>
        /// <param name="partId">The part id.</param>
        /// <param name="spoolId">The spool id.</param>
        public BizTalkTrackedMessagePart LoadTrackedPart(Guid messageId, Guid partId, int spoolId)
        {
            using (var connection = new SqlConnection(this.ConnectionString))
            using (var cmd = CreateStoredProcedureCommand(connection, "ops_LoadTrackedPartByID"))
            {
                AddInParameter(cmd, "@uidMessageID", SqlDbType.UniqueIdentifier, messageId);
                AddInParameter(cmd, "@uidPartID", SqlDbType.UniqueIdentifier, partId);
                AddInParameter(cmd, "@nSpoolID", SqlDbType.Int, spoolId);

                connection.Open();

                using (var reader = cmd.ExecuteReader())
                {
                    if (!reader.Read())
                        return null;

                    return DeserializeMessagePartFromRecord(reader, spoolId);
                }
            }
        }

        /// <summary>
        /// Given a data record; tranform that record into a message part.
        /// </summary>
        protected BizTalkTrackedMessagePart DeserializeMessagePartFromRecord(IDataReader record, int spoolId)
        {
            var part = new BizTalkTrackedMessagePart(this, spoolId);

            part.PartName = record.GetString(0);
            part.PartId = record.GetGuid(1);
            part.FragmentCount = record.GetInt32(2);
            part.ImagePart = (byte[])record.GetValue(3);
            part.ImagePropBag = (byte[])record.GetValue(4);
            part.OldPartId = record.GetGuid(5);

            return part;
        }

        /// <summary>
        /// Loads all tracked parts for the given message id.
        /// </summary>
        /// <param name="messageId">The message id.</param>
        /// <param name="spoolId">The spool id.</param>
        public IEnumerable<BizTalkTrackedMessagePart> LoadTrackedParts(Guid messageId, int spoolId)
        {
            using (var connection = new SqlConnection(this.ConnectionString))
            using (var cmd = CreateStoredProcedureCommand(connection, "ops_LoadTrackedParts"))
            {
                AddInParameter(cmd, "@uidMessageID", SqlDbType.UniqueIdentifier, messageId);
                AddInParameter(cmd, "@nSpoolID", SqlDbType.Int, spoolId);

                connection.Open();

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                        yield return DeserializeMessagePartFromRecord(reader, spoolId);
                }
            }
        }

        /// <summary>
        /// Loads the tracked message context. Returns null if no part fragment was found.
        /// </summary>
        /// <param name="messageId">The message id.</param>
        /// <param name="spoolId">The spool id.</param>
        public BizTalkTrackedMessageContext LoadTrackedMessageContext(Guid messageId, int spoolId)
        {
            using (var connection = new SqlConnection(this.ConnectionString))
            using (var cmd = CreateStoredProcedureCommand(connection, "ops_LoadTrackedMessageContext"))
            {
                AddInParameter(cmd, "@uidMessageID", SqlDbType.UniqueIdentifier, messageId);
                AddInParameter(cmd, "@nSpoolID", SqlDbType.Int, spoolId);

                connection.Open();

                using (var reader = cmd.ExecuteReader())
                {
                    if (!reader.Read())
                        return null;

                    var imgContext = (byte[])reader.GetValue(0);

                    using (var ms = new MemoryStream(imgContext))
                    using (var ctxReader = new BizTalkMessageContextReader(ms))
                    {
                        var properties = ctxReader.ReadContext();

                        return new BizTalkTrackedMessageContext(this, spoolId, new List<BizTalkTrackedMessageContextProperty>(properties));
                    }
                }
            }
        }

        /// <summary>
        /// Loads a tracked part fragment. Returns null if no part fragment was found.
        /// </summary>
        /// <param name="partId">The part id.</param>
        /// <param name="fragmentNumber">The fragment number.</param>
        /// <param name="spoolId">The spool id.</param>
        public BizTalkTrackedMessagePartFragment LoadTrackedPartFragment(Guid partId, int fragmentNumber, int spoolId)
        {
            using (var connection = new SqlConnection(this.ConnectionString))
            using (var cmd = CreateStoredProcedureCommand(connection, "ops_LoadTrackedPartFragment"))
            {
                AddInParameter(cmd, "@uidPartID", SqlDbType.UniqueIdentifier, partId);
                AddInParameter(cmd, "@nFragmentNumber", SqlDbType.Int, fragmentNumber);
                AddInParameter(cmd, "@nSpoolID", SqlDbType.Int, spoolId);

                connection.Open();

                using (var reader = cmd.ExecuteReader())
                {
                    if (!reader.Read())
                        return null;

                    var fragment = new BizTalkTrackedMessagePartFragment(this, spoolId);

                    fragment.ImagePart = (byte[])reader.GetValue(0);

                    return fragment;
                }
            }
        }

        private static SqlParameter AddInParameter(SqlCommand cmd, string parameterName, SqlDbType type, object value)
        {
            var parameter = new SqlParameter(parameterName, type);
            parameter.Value = value;
            cmd.Parameters.Add(parameter);

            return parameter;
        }

        private static SqlParameter AddInParameter(SqlCommand cmd, string parameterName, SqlDbType type, int size, object value)
        {
            var parameter = AddInParameter(cmd, parameterName, type, value);
            parameter.Size = size;

            return parameter;
        }

        private static SqlCommand CreateStoredProcedureCommand(SqlConnection connection, string storedProcedureName)
        {
            return new SqlCommand(storedProcedureName, connection)
            {
                CommandType = CommandType.StoredProcedure
            };
        }

        void IDisposable.Dispose()
        {
        }
    }
}
