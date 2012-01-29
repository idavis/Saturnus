using Moq;

namespace Saturnus.Indexer.Tests
{
    public abstract class IndexGeneratorTestContext
    {
        public static int depth;

        protected IndexGeneratorTestContext()
        {
            Generator = new IndexGenerator();
            MockGenerator = new Mock<IndexGenerator>();
            MockGenerator.CallBase = true;
        }

        protected IndexGenerator Generator { get; set; }
        protected Mock<IndexGenerator> MockGenerator { get; set; }
    }
}