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

namespace Saturnus.Indexer
{
    internal class SafeNativeMethods
    {
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