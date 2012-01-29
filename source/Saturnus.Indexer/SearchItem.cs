#region License

// 
// Copyright (c) 2012, Saturnus Project Contributors
// 
// Dual-licensed under the Apache License, Version 2.0, and the Microsoft Public License (Ms-PL).
// See the file LICENSE.txt for details.
// 

#endregion

using System;
using System.IO;
using System.Windows.Media;

namespace Saturnus.Indexer
{
    public class SearchItem
    {
        public ImageSource Icon
        {
            get
            {
                return IsDirectory
                               ? IconExtractor.GetFolderIcon( FullPath )
                               : IconExtractor.GetIcon( FullPath );
            }
        }

        private bool IsDirectory
        {
            get { return Directory.Exists( FullPath ); }
        }

        public string FileName { get; set; }
        public string FullPath { get; set; }
        public int Size { get; set; }
        public DateTime Modified { get; set; }
        public float Score { get; set; }

        public override string ToString()
        {
            return string.Format( "FileName: {0}, FullPath: {1}, Size: {2}, Modified: {3}",
                                  FileName,
                                  FullPath,
                                  Size,
                                  Modified );
        }
    }
}