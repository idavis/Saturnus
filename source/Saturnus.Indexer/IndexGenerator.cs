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
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;
using System.Threading;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;
using Lucene.Net.Store;
using NLog;
using Directory = Lucene.Net.Store.Directory;
using Version = Lucene.Net.Util.Version;

namespace Saturnus.Indexer
{
    public class IndexGenerator : IObserver<FileSystemEventArgs>, IDisposable
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private static readonly Logger IndexLogger = LogManager.GetLogger( "IndexLogger" );
        private readonly ReaderWriterLockSlim _CacheLock = new ReaderWriterLockSlim();

        private IEnumerable<FileSystemWatcher> _Watchers;

        private Version Version
        {
            get { return Version.LUCENE_29; }
        }
        public IndexGenerator()
        {
            Analyzer = new StandardAnalyzer(Version);
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
                    Logger.Info( "File was created: \"{0}\"", value.FullPath );
                    _CacheLock.EnterWriteLock();
                    try
                    {
                        using ( IndexWriter writer = GetIndexWriter( false ) )
                        {
                            string name = value.FullPath.Substring( value.FullPath.LastIndexOf( '\\' ) + 1 );
                            Add( writer, name, value.FullPath );
                        }
                        break;
                    }
                    finally
                    {
                        _CacheLock.ExitWriteLock();
                    }
                case WatcherChangeTypes.Deleted:
                    Logger.Info( "File was deleted: \"{0}\"", value.FullPath );
                    _CacheLock.EnterWriteLock();
                    try
                    {
                        using ( IndexWriter writer = GetIndexWriter( false ) )
                        {
                            string name = value.FullPath.Substring( value.FullPath.LastIndexOf( '\\' ) + 1 );
                            Remove( writer, name, value.FullPath );
                        }
                        break;
                    }
                    finally
                    {
                        _CacheLock.ExitWriteLock();
                    }
                case WatcherChangeTypes.Changed:
                    Logger.Info( "File was changed: \"{0}\"", value.FullPath );
                    _CacheLock.EnterWriteLock();
                    try
                    {
                        using ( IndexWriter writer = GetIndexWriter( false ) )
                        {
                            string name = value.FullPath.Substring( value.FullPath.LastIndexOf( '\\' ) + 1 );
                            Remove( writer, name, value.FullPath );
                            Add( writer, name, value.FullPath );
                        }
                        break;
                    }
                    finally
                    {
                        _CacheLock.ExitWriteLock();
                    }
            }
        }

        public virtual void OnError( Exception error )
        {
            Logger.ErrorException( "Failed to index something", error );
        }

        public virtual void OnCompleted()
        {
        }

        #endregion

        public event EventHandler IndexChanged;

        public virtual void OnNext( RenamedEventArgs value )
        {
            Logger.Info( "File was renamed from \"{0}\" to \"{1}\"", value.OldFullPath, value.FullPath );
            _CacheLock.EnterWriteLock();
            try
            {
                using ( IndexWriter writer = GetIndexWriter( false ) )
                {
                    Remove( writer, value.OldName, value.OldFullPath );
                    Add( writer, value.Name, value.FullPath );
                }
            }
            finally
            {
                _CacheLock.ExitWriteLock();
            }
        }

        protected void RaiseIndexChanged()
        {
            EventHandler handler = IndexChanged;
            if ( handler != null )
            {
                handler( this, EventArgs.Empty );
            }
        }

        public virtual void ClearIndex()
        {
            Logger.Info( "Clearing Index" );
            using ( IndexWriter writer = GetIndexWriter() )
            {
                writer.DeleteAll();
                RaiseIndexChanged();
            }
        }

        public virtual void CreateIndex()
        {
            Logger.Info( "Creating Index" );
            using ( IndexWriter writer = GetIndexWriter() )
            {
                foreach ( DirectoryInfo root in GetRoots() )
                {
                    Index( root, writer );
                    writer.Optimize();
                }
            }
            Logger.Info( "Index Created" );
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
                var watcher = new FileSystemWatcher( root.FullName ) { IncludeSubdirectories = true };
                watchers.Add( watcher );
                SubscribeToFileSystemEvents( watcher );
                watcher.EnableRaisingEvents = true;
            }
            _Watchers = watchers;
        }

        private void SubscribeToFileSystemEvents( FileSystemWatcher watcher )
        {
            TimeSpan delay = TimeSpan.FromSeconds( 2 );
            IObservable<FileSystemEventArgs> additiveChanges =
                    Observable.FromEventPattern<FileSystemEventArgs>( watcher, "Created" )
                            .Buffer( delay )
                            .SelectMany( events => events.Select( item => item.EventArgs ) );

            additiveChanges.Subscribe( this );

            IObservable<FileSystemEventArgs> subtractiveChanges =
                    Observable.FromEventPattern<FileSystemEventArgs>( watcher, "Deleted" )
                            .Buffer( delay )
                            .SelectMany( events => events.Select( item => item.EventArgs ) );

            subtractiveChanges.Subscribe( this );

            IObservable<FileSystemEventArgs> attributeChanges =
                    Observable.FromEventPattern<FileSystemEventArgs>( watcher, "Changed" )
                            .Buffer( delay )
                            .SelectMany( events => events.Select( item => item.EventArgs ) );

            attributeChanges.Subscribe( this );

            IObservable<FileSystemEventArgs> alteringChanges =
                    Observable.FromEventPattern<FileSystemEventArgs>( watcher, "Renamed" )
                            .Buffer( delay )
                            .SelectMany( events => events.Select( item => item.EventArgs ) );

            alteringChanges.Subscribe( this );
        }

        public virtual IEnumerable<DirectoryInfo> GetRoots()
        {
            //return new[] { new DirectoryInfo( @"C:\dev" ) };
            
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

            foreach ( FileInfo item in files )
            {
                Add( root, writer, item );
            }

            try
            {
                DirectoryInfo[] subDirs = root.GetDirectories();

                foreach ( DirectoryInfo dirInfo in subDirs )
                {
                    Index( dirInfo, writer );
                }
            }
            catch ( UnauthorizedAccessException )
            {
                // happens on directories like \Program/ Files
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
            var parser = new QueryParser( Version, field, Analyzer );
            parser.SetAllowLeadingWildcard( true );
            return parser;
        }

        public virtual IEnumerable<SearchItem> Search( string text )
        {
            var parser = new MultiFieldQueryParser( Version.LUCENE_29, SearchItem.Fields, Analyzer );
            return Search( text, GetIndexSearcher(), parser );
        }

        public IEnumerable<SearchItem> Search( string text, IndexSearcher searcher, QueryParser parser )
        {
            var query = new BooleanQuery();

            string[] terms = text.Split( new[] { " " }, StringSplitOptions.RemoveEmptyEntries );
            foreach ( string term in terms )
            {
                try
                {
                    Query termQuery = parser.Parse( term.Replace( "~", "" ) + "~" );
                    query.Add(termQuery, BooleanClause.Occur.MUST);
                }
                catch ( ParseException )
                {
                    // search is freeform, so we expect this often
                }
            }

            Hits hits = searcher.Search( query );
            int results = hits.Length();
            for ( int i = 0; i < results; i++ )
            {
                Document document = hits.Doc( i );
                yield return SearchItem.FromDocument( document );
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
            Document document = SearchItem.CreateDocument( name, fullName );
            writer.AddDocument( document );
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