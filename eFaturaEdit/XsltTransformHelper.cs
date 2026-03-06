using System;
using System.IO;
using Saxon.Api;

namespace eFaturaEdit
{
    /// <summary>
    /// Saxon-HE tabanlı XSLT dönüşüm yardımcı sınıfı.
    /// XSLT 1.0, 2.0 ve 3.0 destekler.
    /// </summary>
    internal static class XsltTransformHelper
    {
        private static readonly Processor SaxonProcessor = new Processor();

        /// <summary>
        /// Saxon-HE ile XSLT dosyasını XML dosyasına uygulayıp sonucu dosyaya yazar.
        /// </summary>
        internal static void TransformXslFile(string xsltPath, string xmlPath, string outputPath)
        {
            var compiler = SaxonProcessor.NewXsltCompiler();
            var executable = compiler.Compile(new Uri(Path.GetFullPath(xsltPath)));

            var docBuilder = SaxonProcessor.NewDocumentBuilder();
            var input = docBuilder.Build(new Uri(Path.GetFullPath(xmlPath)));

            var transformer = executable.Load();
            transformer.InitialContextNode = input;

            var serializer = SaxonProcessor.NewSerializer();
            serializer.SetOutputFile(outputPath);
            transformer.Run(serializer);
        }

        /// <summary>
        /// XML ve XSLT verilerini string olarak alıp dönüştürülmüş sonucu döndürür.
        /// </summary>
        public static string TransformXml(string xmlData, string xslData)
        {
            var documentBuilder = SaxonProcessor.NewDocumentBuilder();
            documentBuilder.BaseUri = new Uri("file://");
            var xdmNode = documentBuilder.Build(new StringReader(xmlData));

            var xsltCompiler = SaxonProcessor.NewXsltCompiler();
            var xsltExecutable = xsltCompiler.Compile(new StringReader(xslData));
            var xsltTransformer = xsltExecutable.Load();
            xsltTransformer.InitialContextNode = xdmNode;

            var results = new XdmDestination();
            xsltTransformer.Run(results);
            return results.XdmNode.OuterXml;
        }
    }
}
