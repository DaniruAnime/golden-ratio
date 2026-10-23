using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace FunctionAnalyzerMono
{
    /// <summary>
    /// Mono/X11 can try to BinaryFormatter.Deserialize unrelated clipboard formats
    /// when TextBoxBase handles Ctrl+V. Read only X11 UTF-8 text through xclip.
    /// This prevents Mono from entering Clipboard.GetDataObject() on paste.
    /// </summary>
    public sealed class SafePasteTextBox : TextBox
    {
        private const int WmKeyDown = 0x0100;
        private const int WmSysKeyDown = 0x0104;
        private const int WmPaste = 0x0302;
        private const int ClipboardTimeoutMs = 1500;
        private readonly bool onUnix = Environment.OSVersion.Platform == PlatformID.Unix ||
                                        Environment.OSVersion.Platform == PlatformID.MacOSX;

        public SafePasteTextBox()
        {
            ContextMenuStrip menu = new ContextMenuStrip();
            menu.Items.Add("Вставить", null, delegate { PasteText(false); });
            menu.Items.Add("Копировать", null, delegate { Copy(); });
            menu.Items.Add("Вырезать", null, delegate { if (!ReadOnly) Cut(); });
            menu.Items.Add("Выделить всё", null, delegate { SelectAll(); });
            ContextMenuStrip = menu;
        }

        // On some WinForms versions shortcut preprocessing occurs before WndProc.
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (IsPasteShortcut(keyData))
            {
                PasteText(false);
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        protected override void WndProc(ref Message m)
        {
            // Critical: do not call base for a paste key, because Mono's
            // TextBoxBase.WndProc calls Clipboard.GetDataObject() directly.
            if (((m.Msg == WmKeyDown || m.Msg == WmSysKeyDown) &&
                    IsPasteShortcut(((Keys)m.WParam.ToInt32() & Keys.KeyCode) | ModifierKeys)) ||
                m.Msg == WmPaste)
            {
                PasteText(false);
                m.Result = IntPtr.Zero;
                return;
            }
            base.WndProc(ref m);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            // Mono's own middle-button MouseDown handler also calls
            // Clipboard.GetDataObject(true). Do not invoke it on Unix.
            if (onUnix && e.Button == MouseButtons.Middle)
            {
                PasteText(true);
                return;
            }
            base.OnMouseDown(e);
        }

        public static bool IsPasteShortcut(Keys keyData)
        {
            Keys key = keyData & Keys.KeyCode;
            return (key == Keys.V && (keyData & Keys.Control) != 0) ||
                   (key == Keys.Insert && (keyData & Keys.Shift) != 0);
        }

        private void PasteText(bool primarySelection)
        {
            if (ReadOnly) return;
            try
            {
                string clipboardText = onUnix
                    ? ReadX11Text(primarySelection ? "primary" : "clipboard")
                    : Clipboard.GetText(TextDataFormat.UnicodeText);

                if (clipboardText.Length == 0) return;
                // The form only has one-line inputs; avoid inserting hidden newlines.
                clipboardText = clipboardText.Replace("\r\n", " ").Replace('\n', ' ').Replace('\r', ' ');
                int free = MaxLength - (TextLength - SelectionLength);
                if (free <= 0) return;
                if (clipboardText.Length > free) clipboardText = clipboardText.Substring(0, free);
                SelectedText = clipboardText;
            }
            catch (Exception ex)
            {
                MessageBox.Show(FindForm(),
                    "Не удалось вставить текст из буфера обмена: " + ex.Message +
                    "\nНа Arch Linux установите xclip: sudo pacman -S xclip",
                    "Ошибка вставки", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private static string ReadX11Text(string selection)
        {
            // Only fixed arguments; clipboard content is not a shell command.
            // Do not use Mono Clipboard.GetText() here: it also enumerates all
            // available formats and can trigger the same deserialization failure.
            try
            {
                using (Process process = new Process())
                {
                    process.StartInfo = new ProcessStartInfo {
                        FileName = "xclip",
                        Arguments = "-selection " + selection + " -out -t UTF8_STRING",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = false,
                        StandardOutputEncoding = Encoding.UTF8
                    };
                    process.Start();
                    // This is a short, single-line text field. A large or blocked
                    // clipboard is rejected instead of waiting indefinitely.
                    if (!process.WaitForExit(ClipboardTimeoutMs))
                    {
                        try { process.Kill(); } catch (InvalidOperationException) { }
                        throw new IOException("Буфер обмена не ответил за 1,5 секунды.");
                    }
                    if (process.ExitCode != 0)
                        throw new IOException("В буфере обмена нет текста в формате UTF-8.");
                    return process.StandardOutput.ReadToEnd();
                }
            }
            catch (Win32Exception)
            {
                throw new IOException("Утилита xclip не найдена в PATH.");
            }
        }
    }
}
