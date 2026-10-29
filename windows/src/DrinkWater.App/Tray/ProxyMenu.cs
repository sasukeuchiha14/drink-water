using System.Globalization;
using System.Security.Cryptography;
using DrinkWater.App.Persistence;
using DrinkWater.Core;

namespace DrinkWater.App.Tray;

public sealed class ProxyMenu
{
    private readonly Func<ProxySettings> _current;
    private readonly Action<ProxySettings> _apply;
    private readonly ToolStripMenuItem _system = new("Use system proxy");
    private readonly ToolStripMenuItem _direct = new("No proxy (direct)");
    private readonly ToolStripMenuItem _custom = new("Custom proxy");
    private readonly ToolStripComboBox _protocolInput = new() { AutoSize = false, Width = 230, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ToolStripTextBox _host = new() { AutoSize = false, Width = 230, MaxLength = 253 };
    private readonly ToolStripTextBox _port = new() { AutoSize = false, Width = 230, MaxLength = 5 };
    private readonly ToolStripTextBox _username = new() { AutoSize = false, Width = 230, MaxLength = 256 };
    private readonly ToolStripTextBox _password = new() { AutoSize = false, Width = 230, MaxLength = 1024 };
    private readonly CheckBox _clearPassword = new() { Text = "Clear saved password", AutoSize = true, BackColor = Color.Transparent };
    private readonly Button _applyButton = new() { Text = "Apply custom proxy", AutoSize = true };
    private readonly ToolStripMenuItem _error = new() { Enabled = false, Visible = false };

    public ToolStripMenuItem Item { get; } = new("Proxy");

    public ProxyMenu(Func<ProxySettings> current, Action<ProxySettings> apply)
    {
        _current = current;
        _apply = apply;
        _password.TextBox.UseSystemPasswordChar = true;
        _protocolInput.AccessibleName = "Proxy protocol";
        _host.AccessibleName = "Proxy server";
        _port.AccessibleName = "Proxy port";
        _username.AccessibleName = "Proxy username";
        _password.AccessibleName = "Proxy password";
        _host.ToolTipText = "Hostname or IP address only, for example 127.0.0.1 or proxy.example.com.";
        _password.ToolTipText = "Leave blank to keep the saved password for this same proxy and username. Saved passwords are protected for your Windows account.";
        _protocolInput.Items.AddRange(["HTTP", "HTTPS", "SOCKS5"]);
        _protocolInput.ToolTipText = "HTTP supports HTTPS destinations. HTTPS also encrypts the connection to the proxy server.";
        _protocolInput.SelectedIndex = 0;
        _clearPassword.CheckedChanged += (_, _) =>
        {
            if (_clearPassword.Checked) _password.Text = "";
            _password.Enabled = !_clearPassword.Checked;
        };
        _applyButton.Click += (_, _) => ApplyCustom();
        _custom.DropDownItems.AddRange([Label("Protocol"), _protocolInput, Label("Server"), _host, Label("Port (1–65535)"), _port,
            Label("Username (optional)"), _username, Label("Password (blank keeps saved)"), _password,
            new ToolStripControlHost(_clearPassword), new ToolStripControlHost(_applyButton), _error]);
        _custom.DropDownOpening += (_, _) => Populate();
        _custom.DropDownClosed += (_, _) => _password.Text = "";
        _system.Click += (_, _) => { _apply(_current() with { Mode = ProxyMode.System }); Refresh(); };
        _direct.Click += (_, _) => { _apply(_current() with { Mode = ProxyMode.Direct }); Refresh(); };
        Item.DropDownItems.AddRange([_system, _direct, _custom, new ToolStripSeparator(),
            Label("Applies to app update checks."), Label("Downloads use your browser's proxy.")]);
        Item.DropDownOpening += (_, _) => Refresh();
        Refresh();
    }

    public void Refresh()
    {
        ProxyMode mode = _current().Mode;
        _system.Checked = mode == ProxyMode.System;
        _direct.Checked = mode == ProxyMode.Direct;
        _custom.Checked = mode == ProxyMode.Custom;
    }

    private static ToolStripMenuItem Label(string text) => new(text) { Enabled = false };

    private void Populate()
    {
        ProxySettings settings = _current();
        _protocolInput.SelectedIndex = (int)settings.Protocol;
        _host.Text = settings.Host;
        _port.Text = settings.Port.ToString(CultureInfo.InvariantCulture);
        _username.Text = settings.Username;
        _password.Text = "";
        _clearPassword.Checked = false;
        _password.Enabled = true;
        _error.Visible = false;
    }

    private void ApplyCustom()
    {
        try
        {
            if (!int.TryParse(_port.Text.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out int port))
                throw new ArgumentException("Enter a proxy port from 1 to 65535.");
            ProxySettings previous = _current();
            ProxySettings settings = new() { Mode = ProxyMode.Custom, Protocol = (ProxyProtocol)_protocolInput.SelectedIndex, Host = _host.Text.Trim(), Port = port, Username = _username.Text.Trim() };
            settings.Validate();
            if (_password.Text.Length > 0)
                settings = settings with { ProtectedPassword = ProxyPasswordProtection.Protect(_password.Text, settings) };
            else if (!_clearPassword.Checked && settings.Username.Length > 0 && previous.ProtectedPassword is not null)
            {
                if (settings.GetAddress() != previous.GetAddress() || settings.Username != previous.Username)
                    throw new ArgumentException("Enter the password for this proxy, or clear the saved password.");
                settings = settings with { ProtectedPassword = previous.ProtectedPassword };
            }
            _apply(settings);
            _password.Text = "";
            Refresh();
            _custom.DropDown.Close(ToolStripDropDownCloseReason.ItemClicked);
        }
        catch (ArgumentException error)
        {
            _error.Text = error.Message;
            _error.Visible = true;
        }
        catch (CryptographicException)
        {
            _error.Text = "Windows could not protect the password. Settings were not applied.";
            _error.Visible = true;
        }
        catch (FormatException)
        {
            _error.Text = "Enter a valid proxy hostname or IP address.";
            _error.Visible = true;
        }
    }
}
