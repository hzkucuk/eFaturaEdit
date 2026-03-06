using ICSharpCode.TextEditor;

namespace eFaturaEdit
{
    /// <summary>
    /// XSLT editöründe WYSIWYG biçimlendirme işlemleri.
    /// Seçili metni HTML etiketleriyle sarar veya imleç pozisyonuna etiket çifti ekler.
    /// </summary>
    internal static class WysiwygHelper
    {
        /// <summary>
        /// Seçili metni belirtilen HTML etiketiyle sarar.
        /// Seçim yoksa boş etiket çifti ekler ve imleci arasına konumlar.
        /// Örn: "test" → "&lt;b&gt;test&lt;/b&gt;"
        /// </summary>
        public static void WrapSelection(TextEditorControlEx editor, string tag)
        {
            string openTag = "<" + tag + ">";
            string closeTag = "</" + tag + ">";
            WrapSelectionRaw(editor, openTag, closeTag);
        }

        /// <summary>
        /// Seçili metni inline CSS style özniteliğiyle &lt;span&gt; içine sarar.
        /// Örn: "test" → "&lt;span style='font-weight:bold'&gt;test&lt;/span&gt;"
        /// </summary>
        public static void WrapWithStyle(TextEditorControlEx editor, string cssStyle)
        {
            string openTag = "<span style=\"" + cssStyle + "\">";
            string closeTag = "</span>";
            WrapSelectionRaw(editor, openTag, closeTag);
        }

        /// <summary>
        /// Seçili metni div ile sarak text-align stili uygular.
        /// Örn: "test" → "&lt;div style='text-align:center'&gt;test&lt;/div&gt;"
        /// </summary>
        public static void WrapWithAlignment(TextEditorControlEx editor, string alignment)
        {
            string openTag = "<div style=\"text-align:" + alignment + "\">";
            string closeTag = "</div>";
            WrapSelectionRaw(editor, openTag, closeTag);
        }

        /// <summary>
        /// İmleç pozisyonuna ham HTML kodu ekler (seçim sarmalamaz).
        /// </summary>
        public static void InsertAtCursor(TextEditorControlEx editor, string html)
        {
            var doc = editor.Document;
            var caret = editor.ActiveTextAreaControl.Caret;
            int offset = doc.PositionToOffset(caret.Position);

            doc.Insert(offset, html);
            editor.Refresh();

            var newPos = doc.OffsetToPosition(offset + html.Length);
            caret.Position = newPos;
        }

        /// <summary>
        /// Seçili metni verilen açma/kapama etiketleriyle sarar.
        /// Seçim yoksa boş etiket çifti ekleyip imleci arasına konumlar.
        /// </summary>
        private static void WrapSelectionRaw(TextEditorControlEx editor, string openTag, string closeTag)
        {
            var textArea = editor.ActiveTextAreaControl.TextArea;
            var selection = textArea.SelectionManager;
            var doc = editor.Document;
            var caret = editor.ActiveTextAreaControl.Caret;

            if (selection.HasSomethingSelected)
            {
                string selectedText = selection.SelectedText;
                int selOffset = doc.PositionToOffset(selection.SelectionCollection[0].StartPosition);
                int selLength = selectedText.Length;

                // Seçimi kaldır, metni değiştir
                selection.ClearSelection();
                doc.Remove(selOffset, selLength);

                string wrapped = openTag + selectedText + closeTag;
                doc.Insert(selOffset, wrapped);
                editor.Refresh();

                // İmleci kapama etiketinden sonraya konumla
                var newPos = doc.OffsetToPosition(selOffset + wrapped.Length);
                caret.Position = newPos;
            }
            else
            {
                // Seçim yok: etiket çifti ekle, imleci arasına al
                int offset = doc.PositionToOffset(caret.Position);
                string empty = openTag + closeTag;
                doc.Insert(offset, empty);
                editor.Refresh();

                // İmleci açma etiketinin hemen sonrasına konumla
                var midPos = doc.OffsetToPosition(offset + openTag.Length);
                caret.Position = midPos;
            }
        }
    }
}
