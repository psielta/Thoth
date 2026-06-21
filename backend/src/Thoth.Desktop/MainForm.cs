using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace Thoth.Desktop;

internal sealed class MainForm : Form
{
    private const int DwmwaUseImmersiveDarkMode = 20;
    private const int DwmwaUseImmersiveDarkModeBefore20H1 = 19;
    private static readonly Color ShellBackground = Color.FromArgb(19, 18, 17);
    private static readonly Color ShellCard = Color.FromArgb(27, 25, 23);
    private static readonly Color ShellForeground = Color.FromArgb(236, 233, 228);
    private static readonly Color ShellMutedForeground = Color.FromArgb(168, 162, 153);
    private static readonly Color ShellPrimary = Color.FromArgb(255, 185, 0);
    private static readonly Color ShellPrimaryForeground = Color.FromArgb(34, 26, 0);
    private static readonly Color ShellBorder = Color.FromArgb(50, 46, 41);
    private static readonly Color ShellDangerForeground = Color.FromArgb(244, 169, 159);
    private static readonly Uri AppUri = new("http://localhost:8091/");
    private static readonly string AppRevision =
        typeof(MainForm).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? "unknown";
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(2) };

    private readonly WebView2 webView;
    private readonly Panel statusPanel;
    private readonly LoadingSpinner loadingSpinner;
    private readonly Label loadingTitleLabel;
    private readonly Label statusLabel;
    private readonly Label loadingHintLabel;
    private readonly Button retryButton;
    private readonly ApiProcessHost apiHost = new();

    private CancellationTokenSource? startupCancellation;
    private bool loading;

    public MainForm()
    {
        Text = "Thoth";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(960, 640);
        Size = new Size(1280, 800);
        WindowState = FormWindowState.Maximized;
        BackColor = ShellBackground;
        ForeColor = ShellForeground;
        var appIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        if (appIcon is not null)
        {
            Icon = appIcon;
        }

        webView = new WebView2
        {
            Dock = DockStyle.Fill,
            DefaultBackgroundColor = ShellBackground,
            Visible = false
        };
        webView.NavigationCompleted += OnNavigationCompleted;

        loadingSpinner = new LoadingSpinner
        {
            Anchor = AnchorStyles.None,
            Margin = new Padding(0, 0, 0, 10)
        };

        loadingTitleLabel = new Label
        {
            AutoSize = false,
            Dock = DockStyle.Fill,
            Font = new Font(FontFamily.GenericSansSerif, 20, FontStyle.Bold),
            ForeColor = ShellForeground,
            Text = "Thoth",
            TextAlign = ContentAlignment.MiddleCenter
        };

        statusLabel = new Label
        {
            Anchor = AnchorStyles.None,
            AutoSize = false,
            Dock = DockStyle.Fill,
            Font = new Font(FontFamily.GenericSansSerif, 11),
            ForeColor = ShellMutedForeground,
            MaximumSize = new Size(720, 0),
            Text = "Iniciando Thoth...",
            TextAlign = ContentAlignment.MiddleCenter
        };

        loadingHintLabel = new Label
        {
            AutoSize = false,
            Dock = DockStyle.Fill,
            Font = new Font(FontFamily.GenericSansSerif, 8.75f),
            ForeColor = ShellMutedForeground,
            Text = "Preparando a interface e conectando em http://localhost:8091",
            TextAlign = ContentAlignment.MiddleCenter
        };

        retryButton = new Button
        {
            Anchor = AnchorStyles.None,
            AutoSize = true,
            BackColor = ShellPrimary,
            FlatStyle = FlatStyle.Flat,
            ForeColor = ShellPrimaryForeground,
            Padding = new Padding(14, 6, 14, 6),
            Text = "Tentar novamente",
            Visible = false
        };
        retryButton.FlatAppearance.BorderColor = ShellPrimary;
        retryButton.FlatAppearance.MouseDownBackColor = ShellPrimary;
        retryButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(230, 167, 0);
        retryButton.Click += async (_, _) => await LoadApplicationAsync();

        var cardLayout = new TableLayoutPanel
        {
            BackColor = ShellCard,
            ColumnCount = 1,
            Dock = DockStyle.Fill,
            RowCount = 5
        };
        cardLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        cardLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        cardLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
        cardLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 76));
        cardLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        cardLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        cardLayout.Controls.Add(loadingTitleLabel, 0, 0);
        cardLayout.Controls.Add(loadingSpinner, 0, 1);
        cardLayout.Controls.Add(statusLabel, 0, 2);
        cardLayout.Controls.Add(loadingHintLabel, 0, 3);
        cardLayout.Controls.Add(retryButton, 0, 4);

        var loadingCard = new Panel
        {
            Anchor = AnchorStyles.None,
            BackColor = ShellCard,
            BorderStyle = BorderStyle.FixedSingle,
            Padding = new Padding(28, 22, 28, 20),
            Size = new Size(500, 300)
        };
        loadingCard.Controls.Add(cardLayout);

        var layout = new TableLayoutPanel
        {
            BackColor = ShellBackground,
            ColumnCount = 3,
            Dock = DockStyle.Fill,
            RowCount = 3
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        layout.Controls.Add(loadingCard, 1, 1);

        statusPanel = new Panel
        {
            BackColor = ShellBackground,
            Dock = DockStyle.Fill
        };
        statusPanel.Controls.Add(layout);

        Controls.Add(webView);
        Controls.Add(statusPanel);
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        ApplyDarkTitleBar();
    }

    protected override async void OnShown(EventArgs e)
    {
        base.OnShown(e);
        await LoadApplicationAsync();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        startupCancellation?.Cancel();
        startupCancellation?.Dispose();
        apiHost.Stop();
        base.OnFormClosing(e);
    }

    private async Task LoadApplicationAsync()
    {
        if (loading)
        {
            return;
        }

        loading = true;
        ShowLoading();

        startupCancellation?.Cancel();
        startupCancellation?.Dispose();
        startupCancellation = new CancellationTokenSource();

        try
        {
            SetStatus("Iniciando Thoth...");
            await apiHost.EnsureRunningAsync(startupCancellation.Token);
            await WaitForApiAsync(startupCancellation.Token);
            await EnsureWebViewAsync();

            SetStatus("Abrindo Thoth...");
            webView.Visible = true;
            loadingSpinner.Stop();
            statusPanel.Visible = false;
            webView.BringToFront();
            webView.CoreWebView2.Navigate(GetStartupUri().ToString());
        }
        catch (OperationCanceledException) when (IsDisposed || Disposing)
        {
        }
        catch (WebView2RuntimeNotFoundException)
        {
            ShowError(
                "Microsoft Edge WebView2 Runtime nao foi encontrado.\r\n\r\n" +
                "Reinstale o Thoth ou instale o WebView2 Runtime e tente novamente.");
        }
        catch (Exception ex)
        {
            ShowError(
                "Nao foi possivel abrir o Thoth em http://localhost:8091.\r\n\r\n" +
                ex.Message);
        }
        finally
        {
            loading = false;
        }
    }

    private async Task WaitForApiAsync(CancellationToken cancellationToken)
    {
        const int maxAttempts = 60;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            SetStatus($"Iniciando Thoth em http://localhost:8091... ({attempt}/{maxAttempts})");

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, AppUri);
                using var response = await Http.SendAsync(
                    request,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken);

                if ((int)response.StatusCode < 500)
                {
                    return;
                }
            }
            catch (HttpRequestException)
            {
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
            }

            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
        }

        throw new TimeoutException("A API do Thoth nao respondeu dentro do tempo esperado.");
    }

    private static Uri GetStartupUri()
    {
        return new Uri(AppUri, $"?v={Uri.EscapeDataString(AppRevision)}");
    }

    private void ShowLoading()
    {
        retryButton.Visible = false;
        loadingSpinner.Visible = true;
        loadingSpinner.Start();
        loadingHintLabel.Text = "Preparando a interface e conectando em http://localhost:8091";
        loadingHintLabel.ForeColor = ShellMutedForeground;
        statusLabel.ForeColor = ShellMutedForeground;
        webView.Visible = false;
        statusPanel.Visible = true;
        statusPanel.BringToFront();
    }

    private async Task EnsureWebViewAsync()
    {
        if (webView.CoreWebView2 is not null)
        {
            return;
        }

        SetStatus("Inicializando WebView2...");

        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var userDataFolder = Path.Combine(localAppData, "Thoth", "WebView2");
        Directory.CreateDirectory(userDataFolder);

        var environment = await CoreWebView2Environment.CreateAsync(userDataFolder: userDataFolder);
        await webView.EnsureCoreWebView2Async(environment);

        var coreWebView = webView.CoreWebView2
            ?? throw new InvalidOperationException("WebView2 nao foi inicializado.");

        coreWebView.Profile.PreferredColorScheme = CoreWebView2PreferredColorScheme.Dark;
        coreWebView.Settings.IsZoomControlEnabled = true;
        coreWebView.NewWindowRequested += (_, args) =>
        {
            args.Handled = true;
            Process.Start(new ProcessStartInfo(args.Uri) { UseShellExecute = true });
        };
    }

    private void OnNavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        if (e.IsSuccess)
        {
            return;
        }

        ShowError($"Nao foi possivel carregar o Thoth. Status do WebView2: {e.WebErrorStatus}.");
    }

    private void SetStatus(string message)
    {
        statusLabel.Text = message;
    }

    private void ShowError(string message)
    {
        loadingSpinner.Stop();
        loadingSpinner.Visible = false;
        statusLabel.Text = message;
        statusLabel.ForeColor = ShellDangerForeground;
        loadingHintLabel.Text = "Feche qualquer instancia antiga do Thoth, confirme a porta 8091 e tente novamente.";
        loadingHintLabel.ForeColor = ShellMutedForeground;
        retryButton.Visible = true;
        webView.Visible = false;
        statusPanel.Visible = true;
        statusPanel.BringToFront();
    }

    private void ApplyDarkTitleBar()
    {
        if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 17763))
        {
            return;
        }

        var enabled = 1;
        if (DwmSetWindowAttribute(Handle, DwmwaUseImmersiveDarkMode, ref enabled, sizeof(int)) != 0)
        {
            _ = DwmSetWindowAttribute(Handle, DwmwaUseImmersiveDarkModeBefore20H1, ref enabled, sizeof(int));
        }
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int attributeValue, int attributeSize);

    private sealed class LoadingSpinner : Control
    {
        private readonly System.Windows.Forms.Timer timer = new() { Interval = 35 };
        private int angle;

        public LoadingSpinner()
        {
            DoubleBuffered = true;
            Size = new Size(42, 42);
            MinimumSize = Size;
            MaximumSize = Size;
            timer.Tick += (_, _) =>
            {
                angle = (angle + 12) % 360;
                Invalidate();
            };
        }

        public void Start()
        {
            timer.Start();
            Invalidate();
        }

        public void Stop()
        {
            timer.Stop();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                timer.Dispose();
            }

            base.Dispose(disposing);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            var diameter = Math.Min(ClientSize.Width, ClientSize.Height) - 8;
            var x = (ClientSize.Width - diameter) / 2;
            var y = (ClientSize.Height - diameter) / 2;
            var bounds = new Rectangle(x, y, diameter, diameter);

            using var track = new Pen(ShellBorder, 4)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round
            };
            using var active = new Pen(ShellPrimary, 4)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round
            };

            e.Graphics.DrawArc(track, bounds, 0, 360);
            e.Graphics.DrawArc(active, bounds, angle, 105);
        }
    }
}
