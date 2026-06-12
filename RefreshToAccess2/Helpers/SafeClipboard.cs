using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace RefreshToAccess2.Helpers
{
    /// <summary>
    /// Drop-in replacement for <see cref="System.Windows.Clipboard"/> that is both
    /// crash-safe and UI-freeze-safe.
    ///
    /// We deliberately avoid the WPF/OLE clipboard (<c>OleSetClipboard</c> +
    /// <c>OleFlushClipboard</c>): on some machines the OLE flush, when driven from a
    /// short-lived STA worker thread, either fails silently (paste returns nothing)
    /// or blocks long enough to be wrongly reported as a failure. Instead we use the
    /// raw Win32 clipboard API directly — ownership of the data is handed to the OS
    /// by <c>SetClipboardData</c>, so it survives both this worker thread and the
    /// whole process exiting, with no flush step involved.
    ///
    /// Every operation still runs on a dedicated STA thread with a bounded timeout so
    /// the UI thread is never blocked, and on a retry loop because the Win32 clipboard
    /// can only be held by one process at a time (clipboard managers, IMEs, browsers,
    /// remote-desktop agents). Failures are swallowed (set) or returned as empty
    /// (get) rather than surfaced as crashes.
    /// </summary>
    public static class SafeClipboard
    {
        private const int MaxAttempts   = 6;
        private const int DelayMs       = 80;
        private const int OpTimeoutMs   = 2000;

        private const uint CF_UNICODETEXT = 13;
        private const uint GMEM_MOVEABLE  = 0x0002;

        // ── Win32 ──────────────────────────────────────────────────────

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool OpenClipboard(IntPtr hWndNewOwner);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool CloseClipboard();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool EmptyClipboard();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetClipboardData(uint uFormat, IntPtr hMem);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr GetClipboardData(uint uFormat);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool IsClipboardFormatAvailable(uint format);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GlobalAlloc(uint uFlags, UIntPtr dwBytes);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GlobalLock(IntPtr hMem);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GlobalUnlock(IntPtr hMem);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GlobalFree(IntPtr hMem);

        // ── Public API ─────────────────────────────────────────────────

        /// <summary>Best-effort copy. Never throws; returns false if the clipboard
        /// could not be written within the timeout.</summary>
        public static bool TrySetText(string text)
        {
            string value = text ?? string.Empty;
            return RunSta(() =>
            {
                if (!SetUnicodeText(value))
                    throw new InvalidOperationException("clipboard write failed");
            });
        }

        public static void SetText(string text) => TrySetText(text);

        public static string GetText()
        {
            string result = string.Empty;
            RunSta(() => result = GetUnicodeText());
            return result;
        }

        public static bool ContainsText()
        {
            bool result = false;
            RunSta(() => result = IsClipboardFormatAvailable(CF_UNICODETEXT));
            return result;
        }

        public static void Clear()
        {
            RunSta(() =>
            {
                if (!OpenClipboard(IntPtr.Zero))
                    throw new InvalidOperationException("OpenClipboard failed");
                try { EmptyClipboard(); }
                finally { CloseClipboard(); }
            });
        }

        // ── Win32 set/get implementation ───────────────────────────────

        private static bool SetUnicodeText(string value)
        {
            if (!OpenClipboard(IntPtr.Zero))
                return false;

            IntPtr hGlobal = IntPtr.Zero;
            try
            {
                if (!EmptyClipboard())
                    return false;

                // UTF-16LE bytes + null terminator.
                byte[] bytes = System.Text.Encoding.Unicode.GetBytes(value);
                int size = bytes.Length + 2;

                hGlobal = GlobalAlloc(GMEM_MOVEABLE, (UIntPtr)size);
                if (hGlobal == IntPtr.Zero)
                    return false;

                IntPtr target = GlobalLock(hGlobal);
                if (target == IntPtr.Zero)
                    return false;
                try
                {
                    Marshal.Copy(bytes, 0, target, bytes.Length);
                    // Null terminator (2 bytes) for CF_UNICODETEXT.
                    Marshal.WriteInt16(target, bytes.Length, 0);
                }
                finally
                {
                    GlobalUnlock(hGlobal);
                }

                // On success the OS takes ownership of hGlobal — must not free it.
                if (SetClipboardData(CF_UNICODETEXT, hGlobal) == IntPtr.Zero)
                    return false;

                hGlobal = IntPtr.Zero;
                return true;
            }
            finally
            {
                if (hGlobal != IntPtr.Zero)
                    GlobalFree(hGlobal);
                CloseClipboard();
            }
        }

        private static string GetUnicodeText()
        {
            if (!IsClipboardFormatAvailable(CF_UNICODETEXT))
                return string.Empty;

            if (!OpenClipboard(IntPtr.Zero))
                return string.Empty;
            try
            {
                IntPtr handle = GetClipboardData(CF_UNICODETEXT);
                if (handle == IntPtr.Zero)
                    return string.Empty;

                IntPtr ptr = GlobalLock(handle);
                if (ptr == IntPtr.Zero)
                    return string.Empty;
                try
                {
                    return Marshal.PtrToStringUni(ptr) ?? string.Empty;
                }
                finally
                {
                    GlobalUnlock(handle);
                }
            }
            finally
            {
                CloseClipboard();
            }
        }

        // ── STA worker + bounded wait ──────────────────────────────────

        /// <summary>
        /// Runs <paramref name="action"/> on a dedicated STA thread with retry on
        /// transient clipboard locks, waiting at most <see cref="OpTimeoutMs"/> so the
        /// caller (usually the UI thread) never freezes. Returns true on success,
        /// false on timeout or after exhausting retries. Never rethrows.
        /// </summary>
        private static bool RunSta(Action action, int timeoutMs = OpTimeoutMs)
        {
            bool success = false;

            var worker = new Thread(() =>
            {
                for (int attempt = 1; attempt <= MaxAttempts; attempt++)
                {
                    try
                    {
                        action();
                        success = true;
                        return;
                    }
                    catch (Exception) when (attempt < MaxAttempts)
                    {
                        // Another process holds the clipboard; back off and retry.
                        Thread.Sleep(DelayMs);
                    }
                    catch (Exception)
                    {
                        // Exhausted retries — give up quietly.
                        return;
                    }
                }
            })
            {
                IsBackground = true,
                Name = "SafeClipboard"
            };

            worker.SetApartmentState(ApartmentState.STA);
            worker.Start();

            // Bounded wait: if the clipboard op blocks/contends past the timeout we
            // return rather than hang the UI. The background thread is free to finish
            // (or be abandoned) on its own.
            if (!worker.Join(timeoutMs))
                return false;

            return success;
        }
    }
}
