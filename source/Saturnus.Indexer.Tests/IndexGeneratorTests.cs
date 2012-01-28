using System;
using System.Diagnostics;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;
using Moq;

namespace Saturnus.Indexer.Tests
{
    public abstract class IndexGeneratorTestContext
    {
        public static int depth;

        public IndexGeneratorTestContext()
        {
            Generator = new IndexGenerator();
            MockGenerator = new Mock<IndexGenerator>();
            MockGenerator.CallBase = true;
        }

        protected IndexGenerator Generator { get; set; }
        protected Mock<IndexGenerator> MockGenerator { get; set; }

        //[Fact]
        public void IndexSystemBenchmark()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            Generator.CreateIndex();

            using ( IndexReader red = Generator.GetIndexReader() )
            {
                Console.WriteLine( "Indexed {0} documents", red.MaxDoc() );
            }

            stopwatch.Stop();
            Console.WriteLine( stopwatch.Elapsed );
            stopwatch.Restart();
            using ( IndexSearcher searcher = Generator.GetIndexSearcher() )
            {
                QueryParser parser = Generator.GetQueryParser( "path" );
                Generator.Search( "build.*", searcher, parser );
            }
            stopwatch.Stop();
            Console.WriteLine( stopwatch.Elapsed );
        }
    }
}