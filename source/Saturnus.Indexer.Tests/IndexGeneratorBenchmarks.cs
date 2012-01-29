using System;
using System.Collections.Generic;
using System.Diagnostics;
using Lucene.Net.Index;
using Xunit;

namespace Saturnus.Indexer.Tests
{
    public class IndexGeneratorBenchmarks : IndexGeneratorTestContext
    {
        [Fact]
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

            IEnumerable<SearchItem> results = Generator.Search( "build.*" );
            foreach ( SearchItem result in results )
            {
                Console.WriteLine( result );
            }

            stopwatch.Stop();
            Console.WriteLine( stopwatch.Elapsed );
        }
    }
}