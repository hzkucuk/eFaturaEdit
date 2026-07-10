namespace eFaturaEdit
{
    /// <summary>
    /// Tek bir UBL-TR örnek XML dosyası girdisi.
    /// UI-bağımsız POCO.
    /// </summary>
    public class SampleEntry
    {
        public string FileName { get; }
        public string DisplayName { get; }

        public SampleEntry(string fileName, string displayName)
        {
            FileName = fileName;
            DisplayName = displayName;
        }
    }
}
