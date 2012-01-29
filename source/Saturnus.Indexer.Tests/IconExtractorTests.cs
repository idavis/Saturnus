using System;
using System.Diagnostics;
using Xunit;

namespace Saturnus.Indexer.Tests
{
    public class IconExtractorTestsGetIcon
    {
        public IconExtractorTestsGetIcon()
        {
            IconExtractor.Initialize();
        }

        [Fact]
        public void WhenGivenAFileThatDoesNotExists_ThenItsIconIsReturned()
        {
            var icon = IconExtractor.GetIcon( "Foo.txt" );
            Assert.NotNull( icon );
        }

        [Fact]
        public void WhenGivenAFileThatDoesNotExists_ThenItUsesTheSmallIcon()
        {
            var icon = IconExtractor.GetIcon("Foo.txt");
            Assert.Equal( 16, icon.Width );
        }

        [Fact]
        public void NonExistingFilesWithUnknownExtensionReturnNull()
        {
            var icon = IconExtractor.GetIcon("Foo.zzz");
            Assert.Null( icon );
        }

        [Fact]
        public void GivenAnExistingFile_TheIconWillBeExtracted()
        {
            var icon = IconExtractor.GetIcon(GetType().Namespace + ".dll");
            Assert.NotNull(icon);
        }

        [Fact]
        public void GivenAnExistingFile_TheIconWillBeSmall()
        {
            var icon = IconExtractor.GetIcon(GetType().Namespace + ".dll");
            Assert.Equal( 16, icon.Width );
        }

        [Fact]
        public void GivenAnEmptyString_ThenItThrows()
        {
            Assert.Throws<ArgumentNullException>(()=>IconExtractor.GetIcon(string.Empty));            
        }

        [Fact]
        public void GivenANullString_ThenItThrows()
        {
            Assert.Throws<ArgumentNullException>(() => IconExtractor.GetIcon(null));
        }

        [Fact]
        public void GivenAStringOfWhiteSpace_ThenItThrows()
        {
            Assert.Throws<ArgumentNullException>(() => IconExtractor.GetIcon("  "));
        }
    }
}