#region License

// 
// Copyright (c) 2012, Saturnus Project Contributors
// 
// Dual-licensed under the Apache License, Version 2.0, and the Microsoft Public License (Ms-PL).
// See the file LICENSE.txt for details.
// 

#endregion

using System;
using System.Runtime.InteropServices;

namespace Saturnus.Indexer
{
    [StructLayout( LayoutKind.Sequential, CharSet = CharSet.Ansi )]
    public struct SHFILEINFOA
    {
        /// HICON->HICON__*
        public IntPtr hIcon;

        /// int
        public int iIcon;

        /// DWORD->unsigned int
        public uint dwAttributes;

        /// CHAR[260]
        [MarshalAs( UnmanagedType.ByValTStr, SizeConst = 260 )]
        public string szDisplayName;

        /// CHAR[80]
        [MarshalAs( UnmanagedType.ByValTStr, SizeConst = 80 )]
        public string szTypeName;
    }
}