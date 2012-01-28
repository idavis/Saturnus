using System;
using System.IO;
using Lucene.Net.Index;
using Moq;
using Xunit;

namespace Saturnus.Indexer.Tests
{
    public class IndexGeneratorTestsIndexUpdates : IndexGeneratorTestContext, IDisposable
    {
        #region IDisposable Members

        public virtual void Dispose()
        {
            MockGenerator.Verify();
            Generator.Dispose();
        }

        #endregion

        [Fact]
        public void WhenAFileIsCreated_AddIsCalledWithTheNameAndFullPath()
        {
            var args = new FileSystemEventArgs( WatcherChangeTypes.Created,
                                                Generator.BaseDirectory.FullName,
                                                "Bar.txt" );
            MockGenerator.Setup(
                    item =>
                    item.Add( It.IsAny<IndexWriter>(),
                              It.Is<string>( value => value == "Bar.txt" ),
                              It.Is<string>(
                                      value => value == Path.Combine( Generator.BaseDirectory.FullName, "Bar.txt" ) ) ) )
                    .Verifiable();
            MockGenerator.Object.OnNext( args );
        }

        [Fact]
        public void WhenAFileIsDeleted_RemoveIsCalledWithTheNameAndFullPath()
        {
            var args = new FileSystemEventArgs( WatcherChangeTypes.Deleted,
                                                Generator.BaseDirectory.FullName,
                                                "Bar.txt" );
            MockGenerator.Setup(
                    item =>
                    item.Remove( It.IsAny<IndexWriter>(),
                                 It.Is<string>( value => value == "Bar.txt" ),
                                 It.Is<string>(
                                         value => value == Path.Combine( Generator.BaseDirectory.FullName, "Bar.txt" ) ) ) )
                    .Verifiable();
            MockGenerator.Object.OnNext( args );
        }

        [Fact]
        public void WhenAFileIsRenamed_AddIsCalledWithTheNewNameAndFullPath()
        {
            var args = new RenamedEventArgs( WatcherChangeTypes.Renamed,
                                             Generator.BaseDirectory.FullName,
                                             "Bar.txt",
                                             "Foo.txt" );
            MockGenerator.Setup(
                    item =>
                    item.Add( It.IsAny<IndexWriter>(),
                              It.Is<string>( value => value == "Bar.txt" ),
                              It.Is<string>(
                                      value => value == Path.Combine( Generator.BaseDirectory.FullName, "Bar.txt" ) ) ) )
                    .Verifiable();
            MockGenerator.Object.OnNext( args );
        }

        [Fact]
        public void WhenAFileIsRenamed_RemoveIsCalledWithTheOldNewNameAndOldFullPath()
        {
            var args = new RenamedEventArgs( WatcherChangeTypes.Renamed,
                                             Generator.BaseDirectory.FullName,
                                             "Bar.txt",
                                             "Foo.txt" );
            MockGenerator.Setup(
                    item =>
                    item.Remove( It.IsAny<IndexWriter>(),
                                 It.Is<string>( value => value == "Foo.txt" ),
                                 It.Is<string>(
                                         value => value == Path.Combine( Generator.BaseDirectory.FullName, "Foo.txt" ) ) ) )
                    .Verifiable();
            MockGenerator.Object.OnNext( args );
        }
    }
}