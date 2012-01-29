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
using System.IO;
using System.Windows.Media;
using Lucene.Net.Documents;

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

        public override string ToString()
        {
            return string.Format( "FileName: {0}, FullPath: {1}, Size: {2}, Modified: {3}",
                                  FileName,
                                  FullPath,
                                  Size,
                                  Modified );
        }

        public static SearchItem FromDocument(Document document)
        {
            return new SearchItem
                   {
                           FileName = document.Get( "name" ),
                           FullPath = document.Get( "path" ),
                           Modified = new DateTime( long.Parse( document.Get( "modified" ) ) )
                   };
        }

        public static Document CreateDocument( string name, string fullName )
        {
            var document = new Document();
            document.Add(new Field("id", Guid.NewGuid().ToString("B"), Field.Store.YES, Field.Index.NO));
            document.Add(new Field("name", name, Field.Store.YES, Field.Index.ANALYZED));
            document.Add(new Field("path",
                                string.Format("{0}", fullName),
                                Field.Store.YES,
                                Field.Index.ANALYZED));
            // TODO: change to NumericField to support date searching
            DateTime modified;
            modified = Directory.Exists( fullName )
                               ? new DirectoryInfo( fullName ).LastWriteTime
                               : new FileInfo( fullName ).LastWriteTime;
            document.Add( new Field( "modified",
                                     modified.Ticks.ToString(),
                                     Field.Store.YES,
                                     Field.Index.ANALYZED ) );
            return document;
        }

        public static string[] Fields
        {
            get { return new[] { "name", "path", "modified" }; }
        }
    }
}