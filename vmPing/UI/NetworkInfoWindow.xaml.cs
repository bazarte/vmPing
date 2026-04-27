using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace vmPing.UI
{
    /// <summary>
    /// One-click panel showing every active local adapter's IPv4 address,
    /// subnet mask, default gateway, DNS servers, and MAC address, plus the
    /// machine's current external (public) IP address fetched asynchronously.
    /// </summary>
    public partial class NetworkInfoWindow : Window
    {
        // Only one instance may be open at a time.
        public static NetworkInfoWindow _OpenWindow;

        public NetworkInfoWindow()
        {
            InitializeComponent();
        }

        // ── event handlers ────────────────────────────────────────────────

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _OpenWindow = this;
            Populate();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            Populate();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            _OpenWindow = null;
            base.OnClosed(e);
        }

        // ── main populate ─────────────────────────────────────────────────

        /// <summary>
        /// Clear and rebuild the entire content panel.
        /// </summary>
        private void Populate()
        {
            MainPanel.Children.Clear();

            // ── Local adapters ────────────────────────────────────────────
            AddSectionHeader("Local Network Adapters");

            var adapters = GetLocalAdapters();
            if (adapters.Count == 0)
            {
                AddNoDataRow("No active adapters with an IPv4 address were found.");
            }
            else
            {
                foreach (var info in adapters)
                    AddAdapterCard(info);
            }

            // ── Spacer ────────────────────────────────────────────────────
            MainPanel.Children.Add(new Border { Height = 4 });

            // ── External IP ───────────────────────────────────────────────
            AddSectionHeader("External / Public IP");
            AddExternalIpCard();
        }

        // ── local adapter retrieval ───────────────────────────────────────

        private sealed class AdapterInfo
        {
            public string Name         { get; set; }
            public string Description  { get; set; }
            public string MacAddress   { get; set; }
            public string IPv4         { get; set; }
            public string SubnetMask   { get; set; }
            public string Gateway      { get; set; }
            public List<string> Dns    { get; set; } = new List<string>();
            public bool   IsDhcp       { get; set; }
        }

        private static List<AdapterInfo> GetLocalAdapters()
        {
            var result = new List<AdapterInfo>();

            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus != OperationalStatus.Up)
                    continue;
                if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                    continue;

                var props = ni.GetIPProperties();

                // Must have at least one IPv4 unicast address.
                var unicast = props.UnicastAddresses
                    .FirstOrDefault(a => a.Address.AddressFamily == AddressFamily.InterNetwork);
                if (unicast == null)
                    continue;

                var gateway = props.GatewayAddresses
                    .FirstOrDefault(g => g.Address.AddressFamily == AddressFamily.InterNetwork);

                var dns = props.DnsAddresses
                    .Where(d => d.AddressFamily == AddressFamily.InterNetwork)
                    .Select(d => d.ToString())
                    .ToList();

                var subnetMask = unicast.IPv4Mask != null
                    ? unicast.IPv4Mask.ToString()
                    : string.Empty;

                result.Add(new AdapterInfo
                {
                    Name        = ni.Name,
                    Description = ni.Description,
                    MacAddress  = FormatMac(ni.GetPhysicalAddress()),
                    IPv4        = unicast.Address.ToString(),
                    SubnetMask  = subnetMask,
                    Gateway     = gateway?.Address.ToString() ?? string.Empty,
                    Dns         = dns,
                    IsDhcp      = props.GetIPv4Properties()?.IsDhcpEnabled ?? false,
                });
            }

            return result;
        }

        private static string FormatMac(PhysicalAddress mac)
        {
            var bytes = mac?.GetAddressBytes();
            if (bytes == null || bytes.Length == 0)
                return string.Empty;
            return string.Join("-", bytes.Select(b => b.ToString("X2")));
        }

        // ── UI builders ───────────────────────────────────────────────────

        private void AddSectionHeader(string text)
        {
            MainPanel.Children.Add(new TextBlock
            {
                Text            = text,
                Style           = (Style)FindResource("SectionHeader"),
            });
        }

        private void AddNoDataRow(string message)
        {
            var border = new Border { Style = (Style)FindResource("AdapterCard") };
            border.Child = new TextBlock
            {
                Text       = message,
                FontFamily = new FontFamily("Segoe UI"),
                FontSize   = 12,
                Foreground = Brushes.Gray,
            };
            MainPanel.Children.Add(border);
        }

        private void AddAdapterCard(AdapterInfo info)
        {
            var card = new Border { Style = (Style)FindResource("AdapterCard") };
            var stack = new StackPanel();

            // ── Adapter name + DHCP badge ─────────────────────────────────
            var nameRow = new DockPanel { Margin = new Thickness(0, 0, 0, 8) };

            var nameTb = new TextBlock
            {
                Text  = info.Name,
                Style = (Style)FindResource("AdapterName"),
            };
            DockPanel.SetDock(nameTb, Dock.Left);
            nameRow.Children.Add(nameTb);

            if (info.IsDhcp)
            {
                var badge = new Border
                {
                    Background      = new SolidColorBrush(Color.FromRgb(0x1a, 0x73, 0xa7)),
                    CornerRadius    = new CornerRadius(2),
                    Padding         = new Thickness(6, 2, 6, 2),
                    Margin          = new Thickness(8, 0, 0, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                    Child           = new TextBlock
                    {
                        Text       = "DHCP",
                        Foreground = Brushes.White,
                        FontFamily = new FontFamily("Segoe UI"),
                        FontSize   = 10,
                        FontWeight = FontWeights.SemiBold,
                    }
                };
                DockPanel.SetDock(badge, Dock.Left);
                nameRow.Children.Add(badge);
            }

            stack.Children.Add(nameRow);

            // ── Description ───────────────────────────────────────────────
            if (!string.IsNullOrEmpty(info.Description))
            {
                stack.Children.Add(new TextBlock
                {
                    Text            = info.Description,
                    FontFamily      = new FontFamily("Segoe UI"),
                    FontSize        = 11,
                    Foreground      = Brushes.Gray,
                    TextWrapping    = TextWrapping.Wrap,
                    Margin          = new Thickness(0, 0, 0, 8),
                });
            }

            // ── Divider ───────────────────────────────────────────────────
            stack.Children.Add(new Border
            {
                Height          = 1,
                Background      = new SolidColorBrush(Color.FromRgb(0xd8, 0xd8, 0xd8)),
                Margin          = new Thickness(0, 0, 0, 8),
            });

            // ── Data rows ─────────────────────────────────────────────────
            AddDataRow(stack, "IPv4 Address",    info.IPv4,       true);
            AddDataRow(stack, "Subnet Mask",     info.SubnetMask, false);
            AddDataRow(stack, "Default Gateway", info.Gateway,    true);

            if (info.Dns.Count > 0)
                AddDataRow(stack, "DNS Servers", string.Join(",  ", info.Dns), true);

            if (!string.IsNullOrEmpty(info.MacAddress))
                AddDataRow(stack, "MAC Address", info.MacAddress, true);

            card.Child = stack;
            MainPanel.Children.Add(card);
        }

        /// <summary>
        /// Add the external IP card with an async fetch and a refresh button inside.
        /// </summary>
        private void AddExternalIpCard()
        {
            var card  = new Border { Style = (Style)FindResource("AdapterCard") };
            var stack = new StackPanel();

            // Value row: label + TextBlock (updated async) + copy button
            var row = new StackPanel { Orientation = Orientation.Horizontal };

            var label = new TextBlock
            {
                Text  = "Public IP",
                Style = (Style)FindResource("RowLabel"),
            };

            var valueTb = new TextBlock
            {
                Text  = "Fetching…",
                Style = (Style)FindResource("RowValue"),
                VerticalAlignment = VerticalAlignment.Center,
            };

            var copyBtn = MakeCopyButton(valueTb);
            copyBtn.IsEnabled = false;

            row.Children.Add(label);
            row.Children.Add(valueTb);
            row.Children.Add(copyBtn);
            stack.Children.Add(row);

            card.Child = stack;
            MainPanel.Children.Add(card);

            // Fetch asynchronously so the window renders immediately.
            Task.Run(() =>
            {
                string ip = FetchExternalIp();
                Dispatcher.Invoke(() =>
                {
                    valueTb.Text      = string.IsNullOrEmpty(ip) ? "Unable to retrieve" : ip;
                    copyBtn.IsEnabled = !string.IsNullOrEmpty(ip);
                    copyBtn.Tag       = ip;
                });
            });
        }

        /// <summary>
        /// Add a single label / value / copy-button row to a parent panel.
        /// </summary>
        private void AddDataRow(Panel parent, string label, string value, bool copyable)
        {
            var row = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin      = new Thickness(0, 2, 0, 2),
            };

            row.Children.Add(new TextBlock
            {
                Text  = label,
                Style = (Style)FindResource("RowLabel"),
            });

            var valueTb = new TextBlock
            {
                Text  = string.IsNullOrEmpty(value) ? "—" : value,
                Style = (Style)FindResource("RowValue"),
            };
            row.Children.Add(valueTb);

            if (copyable && !string.IsNullOrEmpty(value))
            {
                var btn = MakeCopyButton(valueTb);
                row.Children.Add(btn);
            }

            parent.Children.Add(row);
        }

        /// <summary>
        /// Create a small "Copy" button that writes the sibling TextBlock's text
        /// to the clipboard and briefly changes its label to "✓ Copied".
        /// </summary>
        private Button MakeCopyButton(TextBlock valueTb)
        {
            var btn = new Button
            {
                Content = "Copy",
                Style   = (Style)FindResource("CopyBtn"),
                Tag     = valueTb,
            };

            btn.Click += async (s, e) =>
            {
                var b = (Button)s;
                string text = b.Tag is TextBlock tb2 ? tb2.Text : b.Tag?.ToString() ?? string.Empty;
                if (!string.IsNullOrEmpty(text) && text != "—")
                    Clipboard.SetText(text);

                b.Content = "✓";
                await Task.Delay(900);
                b.Content = "Copy";
            };

            return btn;
        }

        // ── external IP ───────────────────────────────────────────────────

        private static string FetchExternalIp()
        {
            try
            {
                // api.ipify.org returns the public IP as plain text.
                ServicePointManager.SecurityProtocol =
                    SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11;

                var request = (HttpWebRequest)WebRequest.Create("https://api.ipify.org");
                request.Timeout    = 8000;
                request.UserAgent  = "vmPing";

                using (var response = (HttpWebResponse)request.GetResponse())
                using (var reader   = new System.IO.StreamReader(response.GetResponseStream()))
                {
                    return reader.ReadToEnd().Trim();
                }
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
