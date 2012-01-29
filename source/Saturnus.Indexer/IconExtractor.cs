#region License

// 
// Copyright (c) 2012, Saturnus Project Contributors
// 
// Dual-licensed under the Apache License, Version 2.0, and the Microsoft Public License (Ms-PL).
// See the file LICENSE.txt for details.
// 

#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace Saturnus.Indexer
{
    public static class IconExtractor
    {
        private static readonly Dictionary<string, IconSource> ExtensionToIconLookupTable =
                new Dictionary<string, IconSource>( StringComparer.OrdinalIgnoreCase );

        private static bool _IsInitialized;

        public static void Initialize()
        {
            if ( _IsInitialized )
            {
                return;
            }

            RegistryKey root = Registry.ClassesRoot;
            IEnumerable<string> extensions = root.GetSubKeyNames().Where( item => item.StartsWith( "." ) );
            foreach ( string extension in extensions )
            {
                RegistryKey extensionKey = root.OpenSubKey( extension );
                if ( extensionKey == null )
                {
                    continue;
                }
                object value = extensionKey.GetValue( string.Empty );
                if ( value == null )
                {
                    continue;
                }
                RegistryKey redirectKey = root.OpenSubKey( value + @"\DefaultIcon" );
                if ( redirectKey == null )
                {
                    continue;
                }
                var result = (string) redirectKey.GetValue( string.Empty );
                if ( result == null )
                {
                    continue;
                }
                IconSource source = GetIconSource( result );
                ExtensionToIconLookupTable.Add( extension, source );
            }
            _IsInitialized = true;
        }

        private static IconSource GetIconSource( string result )
        {
            string[] parts = result.Split( ',' );
            if ( parts.Length != 2 )
            {
                parts = new[] { parts[0], "0" };
            }
            string fileName = parts[0];
            int index = Math.Max( int.Parse( parts[1] ), 0 );

            var source = new IconSource { FileName = fileName, Index = index };
            return source;
        }

        public static ImageSource GetFolderIcon( string folderName )
        {
            using ( Icon icon = SafeNativeMethods.GetSmallFolderIcon( folderName, 0 ) )
            {
                ImageSource imageSource = FromIcon( icon );
                return imageSource;
            }
        }

        public static ImageSource GetIcon( string fileName )
        {
            if ( !_IsInitialized )
            {
                throw new InvalidOperationException( "This class must be initialized prior to use." );
            }

            if ( string.IsNullOrWhiteSpace( fileName ) )
            {
                throw new ArgumentNullException( "fileName" );
            }

            var file = new FileInfo( fileName );
            IconSource source;
            if ( ExtensionToIconLookupTable.TryGetValue( file.Extension, out source ) )
            {
                ImageSource result = GetImageByExtension( file.Extension );
                if ( result != null )
                {
                    return result;
                }
            }

            if ( !file.Exists )
            {
                return null;
            }

            return ExtractAssociatedIcon( fileName );
        }

        public static ImageSource ExtractAssociatedIcon( string fileName )
        {
            using ( Icon icon = Icon.ExtractAssociatedIcon( fileName ) )
            {
                ImageSource imageSource = FromIcon( icon );
                return imageSource;
            }
        }

        private static ImageSource GetImageByExtension( string extension )
        {
            IconSource item = ExtensionToIconLookupTable[extension];
            using ( Icon icon = SafeNativeMethods.GetSmallIcon( item.FileName, item.Index ) )
            {
                if ( icon == null )
                {
                    // bad registry setting
                    return null;
                }
                ImageSource imageSource = FromIcon( icon );
                return imageSource;
            }
        }

        private static ImageSource FromIcon( Icon icon )
        {
            var bitmapSource = (ImageSource) Imaging.CreateBitmapSourceFromHIcon(
                    icon.Handle,
                    new Int32Rect( 0, 0, 16, 16 ),
                    BitmapSizeOptions.FromEmptyOptions() );
            return bitmapSource;
        }

        #region Nested type: IconSource

        private class IconSource
        {
            public string FileName { get; set; }
            public int Index { get; set; }
        }

        #endregion
    }
}