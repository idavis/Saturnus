﻿#region License

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
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Directory = Lucene.Net.Store.Directory;

namespace Saturnus.Indexer
{
    public class IndexGenerator : IObserver<FileSystemEventArgs>, IDisposable
    {
        public event EventHandler IndexChanged;

        private IEnumerable<FileSystemWatcher> _Watchers;

        public IndexGenerator()
        {
            Analyzer = new StandardAnalyzer();
        }

        private bool IsWatching { get; set; }

        public virtual DirectoryInfo BaseDirectory
        {
            get
            {
                string codeBase = Assembly.GetExecutingAssembly().CodeBase;
                var codeBaseUri = new Uri( codeBase );
                string localPath = codeBaseUri.LocalPath;
                string baseDirectory = System.IO.Directory.GetParent( localPath ).FullName;
                return new DirectoryInfo( baseDirectory );
            }
        }

        public virtual DirectoryInfo IndexDirectory
        {
            get
            {
                const string indexDirectory = "index";
                string path = Path.Combine( BaseDirectory.FullName, indexDirectory );
                return new DirectoryInfo( path );
            }
        }

        public virtual Directory LuceneDirectory
        {
            get
            {
                FSDirectory directory = FSDirectory.Open( IndexDirectory );
                return directory;
            }
        }

        public virtual Analyzer Analyzer { get; private set; }

        #region IDisposable Members

        public void Dispose()
        {
            if ( _Watchers != null )
            {
                foreach ( FileSystemWatcher watcher in _Watchers )
                {
                    watcher.Dispose();
                }
                _Watchers = null;
            }
            if ( Analyzer != null )
            {
                Analyzer.Close();
                Analyzer = null;
            }
        }

        #endregion

        #region IObserver<FileSystemEventArgs> Members

        public virtual void OnNext( FileSystemEventArgs value )
        {
            switch ( value.ChangeType )
            {
                case WatcherChangeTypes.Created:
                    using ( IndexWriter writer = GetIndexWriter( false ) )
                    {
                        string name = value.FullPath.Substring( value.FullPath.LastIndexOf( '\\' ) + 1 );
                        Add( writer, name, value.FullPath );
                    }
                    break;
                case WatcherChangeTypes.Deleted:
                    using ( IndexWriter writer = GetIndexWriter( false ) )
                    {
                        string name = value.FullPath.Substring( value.FullPath.LastIndexOf( '\\' ) + 1 );
                        Remove( writer, name, value.FullPath );
                    }
                    break;
            }
        }

        public virtual void OnError( Exception error )
        {
        }

        public virtual void OnCompleted()
        {
        }

        #endregion

        public virtual void OnNext( RenamedEventArgs value )
        {
            using ( IndexWriter writer = GetIndexWriter( false ) )
            {
                Remove( writer, value.OldName, value.OldFullPath );
                Add( writer, value.Name, value.FullPath );
            }
        }

        protected void RaiseIndexChanged()
        {
            var handler = IndexChanged;
            if(handler != null)
            {
                handler( this, EventArgs.Empty );
            }
        }

        public virtual void ClearIndex()
        {
            using ( IndexWriter writer = GetIndexWriter() )
            {
                writer.DeleteAll();
                RaiseIndexChanged();
            }
        }

        public virtual void CreateIndex()
        {
            using ( IndexWriter writer = GetIndexWriter() )
            {
                foreach ( DirectoryInfo root in GetRoots() )
                {
                    Index( root, writer );
                    writer.Optimize();
                }
            }
        }

        protected virtual IndexWriter GetIndexWriter()
        {
            IndexWriter writer = GetIndexWriter( true );
            return writer;
        }

        protected virtual IndexWriter GetIndexWriter( bool create )
        {
            var writer = new IndexWriter( LuceneDirectory, Analyzer, create, IndexWriter.MaxFieldLength.UNLIMITED );
            return writer;
        }

        public virtual void Watch()
        {
            if ( IsWatching )
            {
                return;
            }
            IsWatching = true;

            var watchers = new List<FileSystemWatcher>();

            foreach ( DirectoryInfo root in GetRoots() )
            {
                var watcher = new FileSystemWatcher( root.FullName ){IncludeSubdirectories = true};
                watchers.Add( watcher );
                
                TimeSpan delay = TimeSpan.FromSeconds( 2 );
                IObservable<FileSystemEventArgs> additiveChanges =
                        Observable.FromEventPattern<FileSystemEventArgs>( watcher, "Created" )
                                .Buffer( delay )
                                .SelectMany( item => item.Select( foo => foo.EventArgs ) );

                additiveChanges.Subscribe( this );

                IObservable<FileSystemEventArgs> subtractiveChanges =
                        Observable.FromEventPattern<FileSystemEventArgs>( watcher, "Deleted" )
                                .Buffer( delay )
                                .SelectMany( item => item.Select( foo => foo.EventArgs ) );

                subtractiveChanges.Subscribe(this);

                IObservable<FileSystemEventArgs> alteringChanges =
                        Observable.FromEventPattern<FileSystemEventArgs>( watcher, "Renamed" )
                                .Buffer( delay )
                                .SelectMany( item => item.Select( foo => foo.EventArgs ) );

                alteringChanges.Subscribe( this );
                 
                watcher.EnableRaisingEvents = true;
            }
            _Watchers = watchers;
        }

        public virtual IEnumerable<DirectoryInfo> GetRoots()
        {
            //return new[] { new DirectoryInfo(@"C:\dev") };
            
            string[] drives = Environment.GetLogicalDrives();

            foreach ( string drive in drives )
            {
                var driveInfo = new DriveInfo( drive );

                if ( !driveInfo.IsReady ||
                     driveInfo.DriveType != DriveType.Fixed )
                {
                    Console.WriteLine( "The drive {0} could not be read", driveInfo.Name );
                    continue;
                }
                DirectoryInfo rootDir = driveInfo.RootDirectory;
                yield return rootDir;
            }
        }

        protected virtual void Index( DirectoryInfo root, IndexWriter writer )
        {
            Add( root, writer );

            IEnumerable<FileInfo> files = GetFiles( root );

            if ( !files.Any() )
            {
                return;
            }

            foreach ( FileInfo item in files )
            {
                Add( root, writer, item );
            }

            DirectoryInfo[] subDirs = root.GetDirectories();

            foreach ( DirectoryInfo dirInfo in subDirs )
            {
                Index( dirInfo, writer );
            }
        }

        protected virtual IEnumerable<FileInfo> GetFiles( DirectoryInfo root )
        {
            try
            {
                FileInfo[] files = root.GetFiles();
                return files;
            }
            catch ( UnauthorizedAccessException )
            {
                // we don't have permission, skip
            }
            catch ( DirectoryNotFoundException )
            {
                // directory was deleted after we got the list, but before we parsed it
            }
            return Enumerable.Empty<FileInfo>();
        }

        public virtual IndexSearcher GetIndexSearcher()
        {
            return new IndexSearcher( LuceneDirectory, true );
        }

        public virtual IndexReader GetIndexReader()
        {
            return IndexReader.Open( LuceneDirectory, true );
        }

        public virtual QueryParser GetQueryParser( string field )
        {
            var parser = new QueryParser( field, Analyzer );
            parser.SetAllowLeadingWildcard( true );
            return parser;
        }

        public virtual IEnumerable<SearchItem> Search( string text )
        {
            return Search( text, GetIndexSearcher(), GetQueryParser( "path" ) );
        }

        public virtual IEnumerable<SearchItem> Search( string text, IndexSearcher searcher, QueryParser parser )
        {
            Query query = parser.Parse( text );
            Hits hits = searcher.Search( query );
            int results = hits.Length();
            for ( int i = 0; i < results; i++ )
            {
                Document doc = hits.Doc( i );
                float score = hits.Score( i );
                yield return new SearchItem() { FileName = doc.Get( "name" ), FullPath = doc.Get( "path" ), Score = score };
            }
        }

        public virtual void Add( DirectoryInfo root, IndexWriter writer, FileInfo file )
        {
            Add( writer, file.Name, file.FullName );
        }

        public virtual void Add( DirectoryInfo root, IndexWriter writer )
        {
            Add( writer, root.Name, root.FullName );
        }

        public virtual void Add( IndexWriter writer, string name, string fullName )
        {
            var doc = new Document();
            doc.Add( new Field( "id", Guid.NewGuid().ToString( "B" ), Field.Store.YES, Field.Index.NO ) );
            doc.Add( new Field( "name", name, Field.Store.YES, Field.Index.TOKENIZED ) );
            doc.Add( new Field( "path",
                                string.Format( "{0}", fullName ),
                                Field.Store.YES,
                                Field.Index.TOKENIZED ) );
            writer.AddDocument( doc );
            RaiseIndexChanged();
        }

        public virtual void Remove( IndexWriter writer, string name, string fullName )
        {
            QueryParser parser = GetQueryParser( "path" );
            Query query = parser.Parse( fullName );
            writer.DeleteDocuments( query );
            RaiseIndexChanged();
        }
    }
}