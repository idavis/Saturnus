#region License

// 
// Copyright (c) 2011, Saturnus Project Contributors
// 
// Dual-licensed under the Apache License, Version 2.0, and the Microsoft Public License (Ms-PL).
// See the file LICENSE.txt for details.
// 

#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
                        //FileInfo fileInfo = new FileInfo( value.FullPath );
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
            //throw new NotImplementedException();
        }

        public virtual void OnCompleted()
        {
            Console.WriteLine( "finished observing" );
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

        public virtual void ClearIndex()
        {
            using ( IndexWriter writer = GetIndexWriter() )
            {
                writer.DeleteAll();
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
                var watcher = new FileSystemWatcher( root.FullName );
                watcher.IncludeSubdirectories = true;

                //watcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName;
                watchers.Add( watcher );
                watcher.Created += ( sender, args ) => OnNext( args );
                watcher.Deleted += ( sender, args ) => OnNext( args );
                watcher.Renamed += ( sender, args ) => OnNext( args );
                watcher.EnableRaisingEvents = true;

                /*
                IObservable<FileSystemEventArgs> additiveChanges =
                        Observable.FromEventPattern<FileSystemEventArgs>( watcher, "Created" )
                                .Select( e => e.EventArgs );

                additiveChanges.Subscribe( this );

                IObservable<FileSystemEventArgs> subtractiveChanges =
                        Observable.FromEventPattern<FileSystemEventArgs>( watcher, "Deleted" )
                                .Select( e => e.EventArgs );

                subtractiveChanges.Subscribe(this);

                IObservable<FileSystemEventArgs> alteringChanges =
                        Observable.FromEventPattern<FileSystemEventArgs>( watcher, "Renamed" )
                                .Select( e => e.EventArgs );

                alteringChanges.Subscribe( this );
                 */
            }
            _Watchers = watchers;
        }

        public virtual IEnumerable<DirectoryInfo> GetRoots()
        {
            //return new[] { new DirectoryInfo( @"C:\" ) };

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

            // Now find all the subdirectories under this directory.
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
            return parser;
        }

        public virtual void Search( string text, IndexSearcher searcher, QueryParser parser )
        {
            //Supply conditions
            Query query = parser.Parse( text );

            //Do the search
            Hits hits = searcher.Search( query );

            //Display results
            Console.WriteLine( "Searching for '" + text + "'" );
            int results = hits.Length();
            Console.WriteLine( "Found {0} results", results );
            for ( int i = 0; i < results; i++ )
            {
                Document doc = hits.Doc( i );
                float score = hits.Score( i );
                Console.WriteLine( "--Result num {0}, score {1}", i + 1, score );
                Console.WriteLine( "--ID: {0}", doc.Get( "id" ) );
                Console.WriteLine( "--Text found: {0}, {1}" + Environment.NewLine, doc.Get( "name" ), doc.Get( "path" ) );
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
        }

        public virtual void Remove( IndexWriter writer, string name, string fullName )
        {
            QueryParser parser = GetQueryParser( "path" );
            Query query = parser.Parse( fullName );
            writer.DeleteDocuments( query );
        }
    }
}