using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;

namespace RefreshToAccess2.Services
{
    /// <summary>
    /// Handles DLL injection into Minecraft processes and the local HTTP
    /// listener that receives port-registration callbacks from injected DLLs.
    /// All platform-invoke declarations live here so nothing else needs them.
    /// </summary>
    public static class TokenInjectionService
    {
        // ── Win32 ──────────────────────────────────────────────────────

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(
            uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr VirtualAllocEx(
            IntPtr hProcess, IntPtr lpAddress,
            uint dwSize, uint flAllocationType, uint flProtect);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool WriteProcessMemory(
            IntPtr hProcess, IntPtr lpBaseAddress,
            byte[] lpBuffer, int nSize, out IntPtr lpNumberOfBytesWritten);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetProcAddress(
            IntPtr hModule, string lpProcName);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("kernel32.dll")]
        private static extern IntPtr CreateRemoteThread(
            IntPtr hProcess, IntPtr lpThreadAttributes,
            uint dwStackSize, IntPtr lpStartAddress,
            IntPtr lpParameter, uint dwCreationFlags, IntPtr lpThreadId);

        // ── State ──────────────────────────────────────────────────────

        /// <summary>
        /// Maps a Minecraft process ID → the port its injected DLL is
        /// listening on.
        /// </summary>
        public static readonly ConcurrentDictionary<int, int> PidPortMap = new();

        // Reuse a single HttpClient for all outgoing injection calls.
        private static readonly HttpClient _http = new();

        // ── Injection ──────────────────────────────────────────────────

        /// <summary>
        /// Injects <paramref name="dllPath"/> into the process identified by
        /// <paramref name="pid"/> using the classic LoadLibraryA remote-thread
        /// technique.
        /// </summary>
        /// <returns>
        /// <c>true</c> when the remote thread was created successfully.
        /// </returns>
        public static bool InjectDll(int pid, string dllPath)
        {
            if (!File.Exists(dllPath))
                return false;

            const uint PROCESS_ALL_ACCESS = 0x001F0FFF;
            const uint MEM_COMMIT_RESERVE = 0x1000 | 0x2000;
            const uint PAGE_READWRITE     = 0x04;

            IntPtr hProcess = OpenProcess(PROCESS_ALL_ACCESS, false, pid);
            if (hProcess == IntPtr.Zero) return false;

            IntPtr loadLibAddr = GetProcAddress(
                GetModuleHandle("kernel32.dll"), "LoadLibraryA");
            if (loadLibAddr == IntPtr.Zero) return false;

            byte[] pathBytes = Encoding.Default.GetBytes(dllPath);
            IntPtr remoteMem = VirtualAllocEx(
                hProcess, IntPtr.Zero,
                (uint)(pathBytes.Length + 1),
                MEM_COMMIT_RESERVE, PAGE_READWRITE);
            if (remoteMem == IntPtr.Zero) return false;

            WriteProcessMemory(hProcess, remoteMem,
                pathBytes, pathBytes.Length, out _);

            IntPtr hThread = CreateRemoteThread(
                hProcess, IntPtr.Zero, 0,
                loadLibAddr, remoteMem, 0, IntPtr.Zero);

            return hThread != IntPtr.Zero;
        }

        // ── Token swap ─────────────────────────────────────────────────

        /// <summary>
        /// Sends a token-swap request to the injected DLL listening on
        /// <paramref name="port"/>.
        /// </summary>
        public static async Task SendSwapTokenAsync(int port, string accessToken)
        {
            try
            {
                using var stream = new MemoryStream();
                using (var writer = new Utf8JsonWriter(stream))
                {
                    writer.WriteStartObject();
                    writer.WriteString("access_token", accessToken);
                    writer.WriteEndObject();
                }

                var content = new StringContent(
                    Encoding.UTF8.GetString(stream.ToArray()),
                    Encoding.UTF8, "application/json");

                var response = await _http.PostAsync(
                    $"http://localhost:{port}/token/swap", content);

                string body = await response.Content.ReadAsStringAsync();
                using var doc  = JsonDocument.Parse(body);
                var root       = doc.RootElement;

                bool success = root.TryGetProperty("success", out var sp)
                               && sp.GetBoolean();

                string message = root.TryGetProperty("message", out var mp)
                    ? mp.GetString() ?? "(no message)"
                    : "(no message)";

                MessageBox.Show(
                    success
                        ? "Token successfully injected."
                        : $"Injection returned failure:\n{message}",
                    success ? "Success" : "Injection error",
                    MessageBoxButton.OK,
                    success ? MessageBoxImage.Information : MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to send token:\n{ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ── HTTP listener handler ──────────────────────────────────────

        /// <summary>
        /// Handles a single inbound HTTP request from the injection listener.
        /// Call this method from a <c>Task.Run</c> for every context returned
        /// by <c>HttpListener.GetContextAsync()</c>.
        /// </summary>
        public static async Task HandleRequestAsync(HttpListenerContext context)
        {
            if (context.Request.HttpMethod != "POST"
                || context.Request.Url?.AbsolutePath != "/client/online")
            {
                context.Response.StatusCode = 404;
                context.Response.Close();
                return;
            }

            try
            {
                using var reader = new StreamReader(
                    context.Request.InputStream,
                    context.Request.ContentEncoding);

                string json = await reader.ReadToEndAsync();
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                // ── pid ────────────────────────────────────────────────
                if (!root.TryGetProperty("pid", out var pidEl)
                    || !pidEl.TryGetInt32(out int pid))
                {
                    await WriteResponseAsync(context, 400, "Missing or invalid 'pid'");
                    return;
                }

                // ── optional error report from the DLL ─────────────────
                if (root.TryGetProperty("error", out var errEl)
                    && errEl.ValueKind == JsonValueKind.String
                    && !string.IsNullOrEmpty(errEl.GetString()))
                {
                    MessageBox.Show(
                        errEl.GetString(),
                        "Injected DLL error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }

                // ── port registration ──────────────────────────────────
                if (root.TryGetProperty("port", out var portEl)
                    && portEl.TryGetInt32(out int port)
                    && port > 0)
                {
                    bool firstTime = !PidPortMap.ContainsKey(pid);
                    PidPortMap[pid] = port;

                    if (firstTime)
                        await TryInitHandshakeAsync(port);
                }

                await WriteResponseAsync(context, 200, "OK");
            }
            catch (Exception ex)
            {
                await WriteResponseAsync(context, 500, $"Error: {ex.Message}");
            }
        }

        // ── Handshake ──────────────────────────────────────────────────

        private static async Task TryInitHandshakeAsync(int port)
        {
            try
            {
                var content = new StringContent(
                    "{}", Encoding.UTF8, "application/json");

                var response = await _http.PostAsync(
                    $"http://localhost:{port}/handshake/init", content);

                string body = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(body);
                var root      = doc.RootElement;

                bool success = root.TryGetProperty("success", out var sp)
                               && sp.GetBoolean();

                string message = root.TryGetProperty("message", out var mp)
                    ? mp.GetString() ?? ""
                    : "";

                MessageBox.Show(
                    success
                        ? "Found an injected Minecraft process – ready to swap tokens."
                        : $"Handshake failed on port {port}: {message}",
                    "Token Injector",
                    MessageBoxButton.OK,
                    success ? MessageBoxImage.Information : MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Handshake error on port {port}:\n{ex.Message}",
                    "Token Injector",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        // ── Helpers ────────────────────────────────────────────────────

        private static async Task WriteResponseAsync(
            HttpListenerContext ctx, int status, string body)
        {
            ctx.Response.StatusCode = status;
            byte[] buf = Encoding.UTF8.GetBytes(body);
            await ctx.Response.OutputStream.WriteAsync(buf, 0, buf.Length);
            ctx.Response.Close();
        }
    }
}
