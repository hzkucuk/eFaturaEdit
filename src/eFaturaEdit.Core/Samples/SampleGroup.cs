using System.Collections.Generic;

namespace eFaturaEdit
{
    /// <summary>
    /// UBL-TR örnek XML dosyalarını kategoriye göre gruplayan veri sınıfı.
    /// UI-bağımsız POCO.
    /// </summary>
    public class SampleGroup
    {
        public string CategoryName { get; }
        public List<SampleEntry> Entries { get; }

        public SampleGroup(string categoryName, List<SampleEntry> entries)
        {
            CategoryName = categoryName;
            Entries = entries;
        }
    }
}
