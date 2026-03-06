using System.Windows.Forms;
using ICSharpCode.TextEditor.Gui.CompletionWindow;

namespace eFaturaEdit
{
    public partial class Form1
    {
        #region Autocomplete (Ctrl+Space / '<' tetiklemeli)

        /// <summary>
        /// XSLT editörüne otomatik tamamlama desteği ekler.
        /// Ctrl+Space ile tam listeyi, '&lt;' karakteriyle etiket önerilerini tetikler.
        /// </summary>
        private void InitAutoComplete()
        {
            textEditorControlEx1.ActiveTextAreaControl.TextArea.KeyEventHandler += TextArea_KeyEventHandler;
            textEditorControlEx1.ActiveTextAreaControl.TextArea.KeyDown += TextArea_AutoCompleteKeyDown;
        }

        /// <summary>
        /// Karakter yazıldığında (&lt;) tamamlama penceresini açar.
        /// </summary>
        private bool TextArea_KeyEventHandler(char ch)
        {
            if (ch == '<')
            {
                ShowCompletionWindow(ch);
            }
            return false;
        }

        /// <summary>
        /// Ctrl+Space ile tamamlama penceresini açar.
        /// </summary>
        private void TextArea_AutoCompleteKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.Space)
            {
                e.SuppressKeyPress = true;
                ShowCompletionWindow('\0');
            }
        }

        /// <summary>
        /// CodeCompletionWindow'u gösterir. Mevcut pencere açıksa yenisini açmaz.
        /// </summary>
        private void ShowCompletionWindow(char ch)
        {
            if (_completionWindow != null && !_completionWindow.IsDisposed)
                return;

            var provider = new XsltCompletionProvider();
            string fileName = textEditorControlEx1.Tag as string ?? "xslt";

            _completionWindow = CodeCompletionWindow.ShowCompletionWindow(
                this,
                textEditorControlEx1,
                fileName,
                provider,
                ch);

            if (_completionWindow != null)
            {
                _completionWindow.Closed += delegate
                {
                    _completionWindow.Dispose();
                    _completionWindow = null;
                };
            }
        }

        #endregion
    }
}
