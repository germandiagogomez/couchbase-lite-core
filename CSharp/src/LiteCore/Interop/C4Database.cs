﻿//
// Database.cs
//
// Author:
// 	Jim Borden  <jim.borden@couchbase.com>
//
// Copyright (c) 2016 Couchbase, Inc All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
using System;
using System.Runtime.InteropServices;
using C4SequenceNumber = System.UInt64;

using LiteCore.Util;
using System.Threading;

namespace LiteCore
{
    public struct C4StorageEngine
    {
        public static readonly string SQLite = "SQLite";

        public static readonly string ForestDB = "ForestDB";
    }
}

namespace LiteCore.Interop
{
    [Flags]
    public enum C4DatabaseFlags : uint
    {
        Create = 1,
        ReadOnly = 2,
        AutoCompact = 4,
        Bundled = 8
    }

    public enum C4DocumentVersioning : uint
    {
        RevisionTrees,
        VersionVectors
    }

    public enum C4EncryptionAlgorithm : uint
    {
        None = 0,
        AES256 = 1
    }

    public unsafe struct C4EncryptionKey
    {
        private const int _Size = 32;

        public static readonly int Size = 32;

        public C4EncryptionAlgorithm algorithm;
        public fixed byte bytes[_Size];
    }

    public unsafe struct C4DatabaseConfig : IDisposable
    {
        public C4DatabaseFlags flags;
        private IntPtr _storageEngine;
        public C4DocumentVersioning versioning;
        public C4EncryptionKey encryptionKey;

        public static C4DatabaseConfig Clone(C4DatabaseConfig *source)
        {
            var retVal = new C4DatabaseConfig();
            retVal.flags = source->flags;
            retVal.versioning = source->versioning;
            retVal.encryptionKey = source->encryptionKey;
            retVal.storageEngine = source->storageEngine;

            return retVal;
        }

        public static C4DatabaseConfig Get(C4DatabaseConfig *source)
        {
            var retVal = new C4DatabaseConfig();
            retVal.flags = source->flags;
            retVal.versioning = source->versioning;
            retVal.encryptionKey = source->encryptionKey;
            retVal._storageEngine = source->_storageEngine;

            return retVal;
        }

        public string storageEngine
        {
            get {
                return Marshal.PtrToStringAnsi(_storageEngine);
            }
            set {
                var old = Interlocked.Exchange(ref _storageEngine, Marshal.StringToHGlobalAnsi(value));
                Marshal.FreeHGlobal(old);
            }
        }

        public void Dispose()
        {
            storageEngine = null;
        }
    }

    public struct C4Database
    {
        
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public unsafe delegate void C4OnCompactCallback(void* context, [MarshalAs(UnmanagedType.U1)]bool compacting);

    public struct C4RawDocument
    {
        public C4Slice key;
        public C4Slice meta;
        public C4Slice body;
    }

    public unsafe static partial class Native
    {
        public static C4Database* c4db_open(string path, C4DatabaseConfig *config, C4Error *outError)
        {
            using(var path_ = new C4String(path)) {
                return NativeRaw.c4db_open(path_.AsC4Slice(), config, outError);
            }
        }

        [DllImport(Constants.DllName, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.U1)]
        public static extern bool c4db_free(C4Database* database);

        [DllImport(Constants.DllName, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.U1)]
        public static extern bool c4db_close(C4Database* database, C4Error* outError);

        [DllImport(Constants.DllName, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.U1)]
        public static extern bool c4db_delete(C4Database* database, C4Error* outError);

        public static bool c4db_deleteAtPath(string dbPath, C4DatabaseConfig* config, C4Error* outError)
        {
            using(var dbPath_ = new C4String(dbPath)) {
                return NativeRaw.c4db_deleteAtPath(dbPath_.AsC4Slice(), config, outError);
            }
        }

        [DllImport(Constants.DllName, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.U1)]
        public static extern bool c4db_rekey(C4Database* database, C4EncryptionKey* newKey, C4Error* outError);

        [DllImport(Constants.DllName, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.U1)]
        public static extern bool c4_shutdown(C4Error* error);

        public static string c4db_getPath(C4Database *database)
        {
            using(var retVal = NativeRaw.c4db_getPath(database)) {
                return ((C4Slice)retVal).CreateString();
            }
        }

        [DllImport(Constants.DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern C4DatabaseConfig* c4db_getConfig(C4Database* database);

        [DllImport(Constants.DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern ulong c4db_getDocumentCount(C4Database* database);

        [DllImport(Constants.DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern C4SequenceNumber c4db_getLastSequence(C4Database* database);

        [DllImport(Constants.DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern ulong c4db_nextDocExpiration(C4Database* database);

        [DllImport(Constants.DllName, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.U1)]
        public static extern bool c4db_compact(C4Database* database, C4Error* outError);

        [DllImport(Constants.DllName, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.U1)]
        public static extern bool c4db_isCompacting(C4Database* database);

        [DllImport(Constants.DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void c4db_setOnCompactCallback(C4Database* database, C4OnCompactCallback cb, void* context);

        [DllImport(Constants.DllName, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.U1)]
        public static extern bool c4db_beginTransaction(C4Database* database, C4Error* outError);

        [DllImport(Constants.DllName, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.U1)]
        public static extern bool c4db_endTransaction(C4Database* database, [MarshalAs(UnmanagedType.U1)]bool commit, C4Error* outError);

        [DllImport(Constants.DllName, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.U1)]
        public static extern bool c4db_isInTransaction(C4Database* database);

        [DllImport(Constants.DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void c4raw_free(C4RawDocument* rawDoc);

        public static C4RawDocument* c4raw_get(C4Database *database, string storeName, string docID, C4Error *outError)
        {
            using(var storeName_ = new C4String(storeName))
            using(var docID_ = new C4String(docID)) {
                return NativeRaw.c4raw_get(database, storeName_.AsC4Slice(), docID_.AsC4Slice(), outError);
            }
        }

        public static bool c4raw_put(C4Database *database,
                                    string storeName,
                                    string key,
                                    string meta,
                                    string body,
                                    C4Error *outError)
        {
            using(var storeName_ = new C4String(storeName))
            using(var key_ = new C4String(key)) 
            using(var meta_ = new C4String(meta))
            using(var body_ = new C4String(body)) {
                return NativeRaw.c4raw_put(database, storeName_.AsC4Slice(), key_.AsC4Slice(), meta_.AsC4Slice(),
                                           body_.AsC4Slice(), outError);
            }
        }


    }

    public unsafe static partial class NativeRaw
    {
        [DllImport(Constants.DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern C4Database* c4db_open(C4Slice path, C4DatabaseConfig* config, C4Error* outError);

        [DllImport(Constants.DllName, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.U1)]
        public static extern bool c4db_deleteAtPath(C4Slice dbPath, C4DatabaseConfig* config, C4Error* outError);

        [DllImport(Constants.DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern C4SliceResult c4db_getPath(C4Database* database);

        [DllImport(Constants.DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern C4RawDocument* c4raw_get(C4Database* database, C4Slice storeName, C4Slice docID, C4Error* outError);

        [DllImport(Constants.DllName, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.U1)]
        public static extern bool c4raw_put(C4Database* database,
                                            C4Slice storeName,
                                            C4Slice key,
                                            C4Slice meta,
                                            C4Slice body,
                                            C4Error* outError);
    }
}
