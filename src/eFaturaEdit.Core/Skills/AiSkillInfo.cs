namespace eFaturaEdit
{
    /// <summary>
    /// AI asistanına eklenebilen adlandırılmış talimat paketi ("yetenek").
    /// UI-bağımsız POCO — DataExport bunu <c>skills.json</c>'a aktarır.
    ///
    /// <para>
    /// Seçilen yeteneklerin <see cref="Prompt"/> metinleri, asistanın sabit sistem
    /// promptunun sonuna eklenir. Sağlayıcıdan bağımsızdır: tool-calling gerektirmez,
    /// desteklenen altı sağlayıcının hepsinde aynı şekilde çalışır.
    /// </para>
    /// </summary>
    public class AiSkillInfo
    {
        public string Id { get; }
        public string Category { get; }
        public string DisplayName { get; }
        public string Description { get; }
        public string Prompt { get; }

        public AiSkillInfo(string id, string displayName, string description, string prompt,
            string category = "Teknik")
        {
            Id = id;
            Category = category;
            DisplayName = displayName;
            Description = description;
            Prompt = prompt;
        }
    }
}
