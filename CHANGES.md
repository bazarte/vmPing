# Fork Changelog — bazarte/vmPing

This fork is based on [R-Smith/vmPing](https://github.com/R-Smith/vmPing).
All original functionality is preserved unchanged. The entries below
document additions made in this fork.

---

## 2026-04-20 — My Network Info panel

**Commit:** `feat: add My Network Info panel (Ctrl+I)`

### What was added

A new window that shows the current machine's full network configuration
in one click — local adapter details **and** the external/public IP —
without leaving vmPing.

### How to open it

| Method | Action |
|--------|--------|
| Menu | Chevron (▾) dropdown → **My Network Info** |
| Keyboard | **Ctrl + I** |

The window is single-instance: pressing Ctrl+I or clicking the menu item
while the window is already open will re-focus it instead of opening a
second copy.

### What the window shows

**Per active adapter (one card each):**

| Field | Source |
|-------|--------|
| Adapter name | `NetworkInterface.Name` |
| Description | `NetworkInterface.Description` |
| DHCP badge | `IPv4Properties.IsDhcpEnabled` |
| IPv4 Address | `UnicastAddresses` (InterNetwork family) |
| Subnet Mask | `UnicastIPAddressInformation.IPv4Mask` |
| Default Gateway | `GatewayAddresses` (InterNetwork family) |
| DNS Servers | `DnsAddresses` (InterNetwork family) |
| MAC Address | `GetPhysicalAddress()` formatted as XX-XX-XX-XX-XX-XX |

**External / Public IP:**

Fetched asynchronously from `https://api.ipify.org` (plain-text endpoint)
so the window opens instantly while the IP is being retrieved in the
background. Uses `TLS 1.2`.

**Copy buttons:**

Every value has a **Copy** button. Clicking it writes the value to the
clipboard and briefly shows **✓** as visual confirmation.

### Files changed

| File | Type | Description |
|------|------|-------------|
| `vmPing/UI/NetworkInfoWindow.xaml` | **New** | WPF window — layout, styles, and resource definitions |
| `vmPing/UI/NetworkInfoWindow.xaml.cs` | **New** | Code-behind — adapter enumeration, async external IP fetch, clipboard copy |
| `vmPing/UI/MainWindow.xaml` | Modified | Added `NetworkInfoMenu` MenuItem in the chevron dropdown |
| `vmPing/UI/MainWindow.xaml.cs` | Modified | Added `NetworkInfoCommand` RoutedCommand, `Ctrl+I` InputBinding, `NetworkInfoExecute()` handler |
| `vmPing/vmPing.csproj` | Modified | Registered both new files as `<Page>` and `<Compile>` items |

### Design decisions

- **No new NuGet dependencies.** Network info uses
  `System.Net.NetworkInformation` (already in the .NET 4.7.2 BCL) and
  the external IP fetch uses `HttpWebRequest` from `System.Net` —
  both already referenced in the project.
- **Async external IP.** The fetch runs on a `Task.Run` thread and
  marshals back to the UI thread via `Dispatcher.Invoke`, keeping the
  window responsive.
- **Single-instance guard.** `NetworkInfoWindow._OpenWindow` tracks the
  open instance. `NetworkInfoExecute` checks it before creating a new
  window, matching the same pattern used by `HelpWindow._OpenWindow` and
  `StatusHistoryWindow`.
- **Style consistency.** The window uses the same `icon.info-circle` SVG
  already defined in `ResourceDictionaries/Icons.xaml`, the same
  `Segoe UI` typeface, and the same `#4040c0` section-header blue used
  in `HelpWindow.xaml`.
