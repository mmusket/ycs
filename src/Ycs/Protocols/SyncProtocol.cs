﻿// ------------------------------------------------------------------------------
//  <copyright company="Microsoft Corporation">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
//  </copyright>
// ------------------------------------------------------------------------------

using System;
using System.IO;

namespace Ycs
{
    public static class SyncProtocol
    {
        public const int MessageYjsSyncStep1 = 0;
        public const int MessageYjsSyncStep2 = 1;
        public const int MessageYjsUpdate = 2;

        public static void WriteSyncStep1(BinaryWriter writer, YDoc doc)
        {
            writer.WriteVarUint(MessageYjsSyncStep1);
            var sv = doc.EncodeStateVectorV2();
            writer.WriteVarUint8Array(sv);
        }

        public static void WriteSyncStep2(BinaryWriter writer, YDoc doc, byte[] encodedStateVector)
        {
            writer.WriteVarUint(MessageYjsSyncStep2);
            var update = doc.EncodeStateAsUpdateV2(encodedStateVector);
            writer.WriteVarUint8Array(update);
        }

        public static void ReadSyncStep1(BinaryReader reader, BinaryWriter writer, YDoc doc)
        {
            var encodedStateVector = reader.ReadVarUint8Array();
            WriteSyncStep2(writer, doc, encodedStateVector);
        }

        public static void ReadSyncStep2(BinaryReader reader, YDoc doc, object transactionOrigin)
        {
            var update = reader.ReadVarUint8Array();
            doc.ApplyUpdateV2(update, transactionOrigin);
        }

        public static void WriteUpdate(BinaryWriter writer, byte[] update)
        {
            writer.WriteVarUint(MessageYjsUpdate);
            writer.WriteVarUint8Array(update);
        }

        public static void ReadUpdate(BinaryReader reader, YDoc doc, object transactionOrigin)
        {
            ReadSyncStep2(reader, doc, transactionOrigin);
        }

        public static int ReadSyncMessage(BinaryReader reader, BinaryWriter writer, YDoc doc, object transactionOrigin)
        {
            var messageType = (int)reader.ReadVarUint();

            switch (messageType)
            {
                case MessageYjsSyncStep1:
                    ReadSyncStep1(reader, writer, doc);
                    break;
                case MessageYjsSyncStep2:
                    ReadSyncStep2(reader, doc, transactionOrigin);
                    break;
                case MessageYjsUpdate:
                    ReadUpdate(reader, doc, transactionOrigin);
                    break;
                default:
                    throw new Exception($"Unknown message type: {messageType}");
            }

            return messageType;
        }
    }
}