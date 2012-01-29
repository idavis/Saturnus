#region License

// 
// Copyright (c) 2012, Saturnus Project Contributors
// 
// Dual-licensed under the Apache License, Version 2.0, and the Microsoft Public License (Ms-PL).
// See the file LICENSE.txt for details.
// 

#endregion

using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace Saturnus.Indexer
{
    internal class SafeNativeMethods
    {
        public static Icon GetSmallFolderIcon( string fileName, int index )
        {
            return GetFolderIcon( fileName, index, true );
        }

        public static Icon GetLargeFolderIcon( string fileName, int index )
        {
            return GetFolderIcon( fileName, index, false );
        }

        public static Icon GetFolderIcon( string fileName, int index, bool isSmall )
        {
            const uint SHGFI_ICON = 0x000000100;
            const uint SHGFI_SMALLICON = 0x000000001;
            const uint SHGFI_LARGEICON = 0x000000000;
            const uint FILE_ATTRIBUTE_DIRECTORY = 0x00000010;

            uint flags = SHGFI_ICON + ( isSmall ? SHGFI_SMALLICON : SHGFI_LARGEICON );

            var fileInfo = new SHFILEINFOW();
            uint size = (uint) Marshal.SizeOf( fileInfo );
            uint result = NativeMethods.SHGetFileInfoW( fileName, FILE_ATTRIBUTE_DIRECTORY, ref fileInfo, size, flags );

            if ( result == 0 )
            {
                return null;
            }

            var icon = (Icon) Icon.FromHandle( fileInfo.hIcon ).Clone();
            DestroyIcon( fileInfo.hIcon );
            return icon;
        }

        public static Icon GetSmallIcon( string fileName, int index )
        {
            return GetIcon( fileName, index, true );
        }

        public static Icon GetLargeIcon( string fileName, int index )
        {
            return GetIcon( fileName, index, false );
        }

        internal static Icon GetIcon( string fileName, int index, bool isSmall )
        {
            IntPtr small = IntPtr.Zero;
            IntPtr large = IntPtr.Zero;
            Icon icon;
            try
            {
                uint found = NativeMethods.ExtractIconExW( fileName, index, ref large, ref small, 1 );
                if ( found == 0 ||
                     found > 2 ||
                     small == IntPtr.Zero )
                {
                    return null;
                }
                icon = (Icon) Icon.FromHandle( isSmall ? small : large ).Clone();
            }
            finally
            {
                DestroyIcon( small );
                DestroyIcon( large );
            }
            return icon;
        }

        /// <summary>
        /// Destroys an icon and frees any memory the icon occupied.
        /// </summary>
        /// <param name="hIcon">A handle to the icon to be destroyed. The icon must not be in use.</param>
        /// <returns>
        /// If the function succeeds, the return value is true.
        /// If the function fails, the return value is false.
        /// </returns>
        public static bool DestroyIcon( IntPtr hIcon )
        {
            return hIcon != IntPtr.Zero && NativeMethods.DestroyIcon( hIcon );
        }
    }
}