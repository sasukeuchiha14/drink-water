namespace DrinkWater.App.WindowsIntegration;

public sealed class SessionWindow : NativeWindow, IDisposable
{
    public SessionWindow()
    {
        // An invisible top-level message window receives power and shutdown events.
        CreateHandle(new CreateParams { Caption = "Drink Water session listener", ExStyle = 0x80 });
    }

    public event EventHandler? Suspending;

    public event EventHandler? Resuming;

    public event EventHandler? Closing;

    public void Dispose() => DestroyHandle();

    protected override void WndProc(ref Message message)
    {
        switch (message.Msg)
        {
            case 0x0218: // WM_POWERBROADCAST
                if ((int)message.WParam == 4)
                {
                    Suspending?.Invoke(this, EventArgs.Empty);
                }
                else if ((int)message.WParam is 7 or 18)
                {
                    Resuming?.Invoke(this, EventArgs.Empty);
                }

                message.Result = 1;
                return;
            case 0x0011: // WM_QUERYENDSESSION: allow logoff and Restart Manager shutdown.
                message.Result = 1;
                return;
            case 0x0016 when message.WParam != 0: // WM_ENDSESSION
            case 0x0010: // WM_CLOSE
                Closing?.Invoke(this, EventArgs.Empty);
                message.Result = 0;
                return;
        }

        base.WndProc(ref message);
    }
}
