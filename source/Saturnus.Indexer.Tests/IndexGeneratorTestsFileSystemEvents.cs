using System;
using System.IO;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Moq;
using Xunit;

namespace Saturnus.Indexer.Tests
{
    public class IndexGeneratorTestsFileSystemEvents : IndexGeneratorTestContext, IDisposable
    {
        public IndexGeneratorTestsFileSystemEvents()
        {
            FileName = "Foo.txt";
            NewFileName = "Bar.txt";
            DeleteFiles();
            MockGenerator.Setup( item => item.GetRoots() ).Returns( new[] { new DirectoryInfo( "." ), } );
        }

        protected string FileName { get; set; }
        protected string NewFileName { get; set; }

        #region IDisposable Members

        public virtual void Dispose()
        {
            MockGenerator.Verify();
            DeleteFiles();
            Generator.Dispose();
        }

        #endregion

        [Fact]
        public void WhenANewFileIsCreated_ThenItIsProcessed()
        {
            WatcherChangeTypes toWatch = WatcherChangeTypes.Created;
            Action action = CreateFile;
            AssertFileSystemOperation<FileSystemEventArgs>( action, toWatch );
        }

        [Fact]
        public void WhenANewFileIsDeleted_ThenItIsProcessed()
        {
            CreateFile();
            WatcherChangeTypes toWatch = WatcherChangeTypes.Deleted;
            Action action = DeleteFiles;
            AssertFileSystemOperation<FileSystemEventArgs>( action, toWatch );
        }

        [Fact]
        public void WhenANewFileIsRenamed_ThenItIsProcessed()
        {
            CreateFile();
            Expression<Func<RenamedEventArgs, bool>> predicate =
                    value => ( value.ChangeType == WatcherChangeTypes.Renamed &&
                               value.Name.EndsWith( NewFileName ) );
            Action action = RenameFile;

            AssertFileSystemOperation( action, predicate );
        }

        protected virtual void AssertFileSystemOperation<T>( Action action, WatcherChangeTypes toWatch )
                where T : FileSystemEventArgs
        {
            AssertFileSystemOperation<T>( action, toWatch, FileName );
        }

        protected virtual void AssertFileSystemOperation<T>( Action action, WatcherChangeTypes toWatch, string fileName )
                where T : FileSystemEventArgs
        {
            Expression<Func<T, bool>> predicate = value => value.ChangeType == toWatch &&
                                                           value.Name.EndsWith( FileName );
            AssertFileSystemOperation( action, predicate );
        }

        protected virtual void AssertFileSystemOperation<T>( Action action, Expression<Func<T, bool>> predicate )
                where T : FileSystemEventArgs
        {
            var task = new Task( () => { } );
            MockGenerator.Setup(
                    item => item.OnNext( It.Is( predicate ) ) )
                    .Callback( task.Start )
                    .Verifiable();

            MockGenerator.Object.Watch();

            action();

            bool hit = Task.WaitAll( new[] { task }, TimeSpan.FromSeconds( 9 ) );

            Assert.True( hit );
        }

        protected virtual void AssertFileSystemOperation( Action action,
                                                          Expression<Func<RenamedEventArgs, bool>> predicate )
        {
            var task = new Task( () => { } );
            MockGenerator.Setup(
                    item => item.OnNext( It.Is( predicate ) ) )
                    .Callback( task.Start )
                    .Verifiable();

            MockGenerator.Object.Watch();

            action();

            bool hit = Task.WaitAll( new[] { task }, TimeSpan.FromSeconds( 9 ) );

            Assert.True( hit );
        }

        protected virtual void CreateFile()
        {
            File.WriteAllText( FileName, "foo" );
        }

        protected virtual void RenameFile()
        {
            var fileInfo = new FileInfo( FileName );
            fileInfo.MoveTo( NewFileName );
        }

        protected virtual void DeleteFiles()
        {
            if ( File.Exists( FileName ) )
            {
                File.Delete( FileName );
            }
            if ( File.Exists( NewFileName ) )
            {
                File.Delete( NewFileName );
            }
        }
    }
}