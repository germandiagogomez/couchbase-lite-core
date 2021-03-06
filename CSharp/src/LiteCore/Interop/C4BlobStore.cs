﻿//
// Types.cs
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

using LiteCore.Util;

namespace LiteCore.Interop
{
    public unsafe struct C4BlobKey
    {
        public static readonly int Size = _Size;
        private const int _Size = 20;
        public fixed byte bytes[_Size];

        public override int GetHashCode()
        {
            int hash = 17;
            unchecked {
                fixed(byte* b = bytes) {
                    for(int i = 0; i < _Size; i++) {
                        hash = hash * 23 + b[i];
                    }
                }
            }

            return hash;
        }

        public override bool Equals(object obj)
        {
            if(!(obj is C4BlobKey)) {
                return false;
            }

            var other = (C4BlobKey)obj;
            fixed(byte* b = bytes) {
                for(int i = 0; i < _Size; i++) {
                    if(b[i] != other.bytes[i]) {
                        return false;
                    }
                }
            }

            return true;
        }
    }

    public struct C4BlobStore
    {
        
    }

    public struct C4ReadStream
    {
        
    }

    public struct C4WriteStream
    {
        
    }

    public unsafe static partial class Native
    {
        public static bool c4blob_keyFromString(string str, C4BlobKey *key)
        {
            using(var str_ = new C4String(str)) {
                return NativeRaw.c4blob_keyFromString(str_.AsC4Slice(), key);
            }
        }

        public static string c4blob_keyToString(C4BlobKey key)
        {
            using(var retVal = NativeRaw.c4blob_keyToString(key)) {
                return ((C4Slice)retVal).CreateString();
            }
        }

        public static C4BlobStore* c4blob_openStore(string dirPath,
                                                    C4DatabaseFlags flags,
                                                    C4EncryptionKey* encryptionKey,
                                                    C4Error* outError)
        {
            using(var dirPath_ = new C4String(dirPath)) {
                return NativeRaw.c4blob_openStore(dirPath_.AsC4Slice(), flags, encryptionKey, outError);
            }
        }

        [DllImport(Constants.DllName, CallingConvention=CallingConvention.Cdecl)]
        public static extern void c4blob_freeStore(C4BlobStore* store);

        [DllImport(Constants.DllName, CallingConvention=CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.U1)]
        public static extern bool c4blob_deleteStore(C4BlobStore* store, C4Error* outError);

        [DllImport(Constants.DllName, CallingConvention=CallingConvention.Cdecl)]
        public static extern long c4blob_getSize(C4BlobStore* store, C4BlobKey key);

        public static byte[] c4blob_getContents(C4BlobStore *store, C4BlobKey key, C4Error *outError)
        {
            using(var retVal = NativeRaw.c4blob_getContents(store, key, outError)) {
                return ((C4Slice)retVal).ToArrayFast();
            }
        }

        public static bool c4blob_create(C4BlobStore *store, byte[] content, C4BlobKey *outKey, C4Error *outError)
        {
            fixed(byte* p = content) {
                var c4Slice = new C4Slice(p, (ulong)content.Length);
                return NativeRaw.c4blob_create(store, c4Slice, outKey, outError);
            }
        }

        [DllImport(Constants.DllName, CallingConvention=CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.U1)]
        public static extern bool c4blob_delete(C4BlobStore* store, C4BlobKey key, C4Error* outError);

        [DllImport(Constants.DllName, CallingConvention=CallingConvention.Cdecl)]
        public static extern C4ReadStream* c4blob_openReadStream(C4BlobStore* store, C4BlobKey key, C4Error* outError);

        public static long c4stream_read(C4ReadStream *stream, byte[] buffer, C4Error *outError)
        {
            return NativeRaw.c4stream_read(stream, buffer, (UIntPtr)buffer.Length, outError).ToInt64();
        }

        public static long c4stream_read(C4ReadStream *stream, byte[] buffer, int count, C4Error *outError)
        {
            return NativeRaw.c4stream_read(stream, buffer, (UIntPtr)count, outError).ToInt64();
        }

        [DllImport(Constants.DllName, CallingConvention=CallingConvention.Cdecl)]
        public static extern long c4stream_getLength(C4ReadStream* stream, C4Error* outError);

        [DllImport(Constants.DllName, CallingConvention=CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.U1)]
        public static extern bool c4stream_seek(C4ReadStream* stream, ulong position, C4Error* outError);

        [DllImport(Constants.DllName, CallingConvention=CallingConvention.Cdecl)]
        public static extern void c4stream_close(C4ReadStream* stream);

        [DllImport(Constants.DllName, CallingConvention=CallingConvention.Cdecl)]
        public static extern C4WriteStream* c4blob_openWriteStream(C4BlobStore* store, C4Error* outError);

        public static bool c4stream_write(C4WriteStream *stream, byte[] bytes, C4Error *outError)
        {
            return NativeRaw.c4stream_write(stream, bytes, (UIntPtr)bytes.Length, outError);
        }

        [DllImport(Constants.DllName, CallingConvention=CallingConvention.Cdecl)]
        public static extern C4BlobKey c4stream_computeBlobKey(C4WriteStream* stream);

        [DllImport(Constants.DllName, CallingConvention=CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.U1)]
        public static extern bool c4stream_install(C4WriteStream* stream, C4Error* outError);

        [DllImport(Constants.DllName, CallingConvention=CallingConvention.Cdecl)]
        public static extern void c4stream_closeWriter(C4WriteStream* stream);
    }

    public unsafe static partial class NativeRaw
    {
        [DllImport(Constants.DllName, CallingConvention=CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.U1)]
        public static extern bool c4blob_keyFromString(C4Slice str, C4BlobKey* key);

        [DllImport(Constants.DllName, CallingConvention=CallingConvention.Cdecl)]
        public static extern C4SliceResult c4blob_keyToString(C4BlobKey key);

        [DllImport(Constants.DllName, CallingConvention=CallingConvention.Cdecl)]
        public static extern C4BlobStore* c4blob_openStore(C4Slice dirPath,
                                                           C4DatabaseFlags flags,
                                                           C4EncryptionKey* encryptionKey,
                                                           C4Error* outError);

        [DllImport(Constants.DllName, CallingConvention=CallingConvention.Cdecl)]
        public static extern C4SliceResult c4blob_getContents(C4BlobStore* store, C4BlobKey key, C4Error* outError);

        [DllImport(Constants.DllName, CallingConvention=CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.U1)]
        public static extern bool c4blob_create(C4BlobStore* store, C4Slice contents, C4BlobKey* outKey, C4Error* outError);

        [DllImport(Constants.DllName, CallingConvention=CallingConvention.Cdecl)]
        public static extern IntPtr c4stream_read(C4ReadStream* stream, [Out]byte[] buffer, UIntPtr maxBytesToRead, C4Error* outError);

        [DllImport(Constants.DllName, CallingConvention=CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.U1)]
        public static extern bool c4stream_write(C4WriteStream* stream, byte[] bytes, UIntPtr length, C4Error* outError);
    }

}
