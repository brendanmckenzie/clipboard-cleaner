using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Runtime.InteropServices;
using System.Text;
using System.Web;
using System.Windows.Forms;

namespace ClipboardCleaner
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            var form = new EmptyForm();

            NativeMethods.SetParent(form.Handle, NativeMethods.HWND_MESSAGE);
            NativeMethods.AddClipboardFormatListener(form.Handle);

            Application.Run();
        }
    }

    class EmptyForm : Form
    {
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == NativeMethods.WM_CLIPBOARDUPDATE)
            {
                if (Clipboard.ContainsText())
                {
                    var text = Clipboard.GetText();
                    Uri uri;
                    if (Uri.TryCreate(text, UriKind.Absolute, out uri))
                    {
                        var queryString = HttpUtility.ParseQueryString(uri.Query ?? string.Empty);

                        if (uri.Host.Contains("youtube") && !string.IsNullOrEmpty(queryString["v"]))
                        {
                            Clipboard.SetText(uri.Scheme + "://youtu.be/" + queryString["v"]);
                        }
                        else if (uri.Host.Contains("google") && !string.IsNullOrEmpty(queryString["imgurl"]))
                        {
                            Clipboard.SetText(HttpUtility.UrlDecode(queryString["imgurl"]));
                        }
                        else
                        {
                            var nullables = new List<string>();
                            var queryStringChanged = false;
                            foreach (string key in queryString.Keys)
                            {
                                if (key.StartsWith("utm"))
                                {
                                    queryStringChanged = true;
                                    nullables.Add(key);
                                }
                            }
                            foreach (var key in nullables)
                            {
                                queryString.Remove(key);
                            }

                            if (queryStringChanged)
                            {
                                var builder = new UriBuilder(uri);

                                builder.Query = ConstructQueryString(queryString);

                                Clipboard.SetText(builder.Uri.ToString());
                            }

                        }
                    }
                }
            }

            base.WndProc(ref m);
        }

        public static string ConstructQueryString(NameValueCollection parameters)
        {
            var sb = new StringBuilder();

            foreach (String name in parameters)
                sb.Append(String.Concat(name, "=", System.Web.HttpUtility.UrlEncode(parameters[name]), "&"));

            if (sb.Length > 0)
                return sb.ToString(0, sb.Length - 1);

            return String.Empty;
        }
    }

    internal static class NativeMethods
    {
        // See http://msdn.microsoft.com/en-us/library/ms649021%28v=vs.85%29.aspx
        public const int WM_CLIPBOARDUPDATE = 0x031D;
        public static IntPtr HWND_MESSAGE = new IntPtr(-3);

        // See http://msdn.microsoft.com/en-us/library/ms632599%28VS.85%29.aspx#message_only
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool AddClipboardFormatListener(IntPtr hwnd);

        // See http://msdn.microsoft.com/en-us/library/ms633541%28v=vs.85%29.aspx
        // See http://msdn.microsoft.com/en-us/library/ms649033%28VS.85%29.aspx
        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);
    }
}
