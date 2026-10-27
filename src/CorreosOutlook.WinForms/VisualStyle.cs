using System.Drawing.Drawing2D;

namespace CorreosOutlook.WinForms;

internal static class VisualStyle
{
    internal static readonly Color Background = Color.FromArgb(246, 248, 251);
    internal static readonly Color Ink = Color.FromArgb(34, 47, 65);
    internal static readonly Color Muted = Color.FromArgb(93, 108, 125);
    internal static readonly Color Accent = Color.FromArgb(38, 98, 144);
    internal static readonly Color Surface = Color.White;

    internal static void Apply(Control root)
    {
        foreach (Control child in root.Controls)
        {
            if (child is Button button) StyleButton(button,
                button.Text.StartsWith("Crear borrador", StringComparison.Ordinal));
            else if (child is TabControl tabs)
            {
                tabs.Padding = new Point(20, 9);
                tabs.Appearance = TabAppearance.Normal;
                tabs.BackColor = Background;
            }
            else if (child is TabPage page) page.BackColor = Background;
            else if (child is Label label && label.ForeColor == SystemColors.ControlText) label.ForeColor = Ink;
            Apply(child);
        }
    }

    internal static void StyleButton(Button button, bool primary = false)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = primary ? 0 : 1;
        button.FlatAppearance.BorderColor = Color.FromArgb(207, 218, 229);
        button.BackColor = primary ? Accent : Surface;
        button.ForeColor = primary ? Color.White : Ink;
        button.Cursor = Cursors.Hand;
        button.Padding = new Padding(12, 8, 12, 8);
        button.Margin = new Padding(4, 4, 8, 4);
        button.Resize += (_, _) => Round(button);
        Round(button);

        // A short color transition provides feedback without moving controls or hiding focus.
        var resting = button.BackColor;
        var hovered = primary ? Color.FromArgb(29, 80, 121) : Color.FromArgb(231, 239, 247);
        var target = resting;
        var animation = new System.Windows.Forms.Timer { Interval = 16 };
        animation.Tick += (_, _) =>
        {
            if (button.IsDisposed) { animation.Stop(); animation.Dispose(); return; }
            var current = button.BackColor;
            int Step(int from, int to) => Math.Abs(to - from) < 4 ? to : from + (to - from) / 3;
            var next = Color.FromArgb(Step(current.R, target.R), Step(current.G, target.G), Step(current.B, target.B));
            button.BackColor = next;
            if (next == target) animation.Stop();
        };
        button.MouseEnter += (_, _) => { target = hovered; animation.Start(); };
        button.MouseLeave += (_, _) => { target = resting; animation.Start(); };
        button.EnabledChanged += (_, _) => { target = resting; button.BackColor = resting; animation.Stop(); };
        button.Disposed += (_, _) => animation.Dispose();
    }

    internal static void RoundCard(Control control)
    {
        control.Resize += (_, _) => Round(control);
        Round(control);
    }

    private static void Round(Control control)
    {
        if (control.Width < 4 || control.Height < 4) return;
        using var path = new GraphicsPath();
        var radius = Math.Min(12, control.Height / 2);
        var diameter = radius * 2;
        var rect = new Rectangle(0, 0, control.Width - 1, control.Height - 1);
        path.AddArc(rect.Left, rect.Top, diameter, diameter, 180, 90);
        path.AddArc(rect.Right - diameter, rect.Top, diameter, diameter, 270, 90);
        path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(rect.Left, rect.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        var previous = control.Region;
        control.Region = new Region(path);
        previous?.Dispose();
    }
}
