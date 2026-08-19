namespace PNETGuard;

internal static class PnetTheme
{
    public static readonly Color Background = Color.FromArgb(12, 13, 14);
    public static readonly Color Surface = Color.FromArgb(24, 25, 27);
    public static readonly Color SurfaceAlt = Color.FromArgb(31, 33, 35);
    public static readonly Color Border = Color.FromArgb(67, 71, 74);
    public static readonly Color Text = Color.FromArgb(239, 239, 232);
    public static readonly Color Muted = Color.FromArgb(168, 171, 166);
    public static readonly Color Gold = Color.FromArgb(232, 188, 74);
    public static readonly Color Green = Color.FromArgb(42, 194, 91);
    public static readonly Color GreenDark = Color.FromArgb(19, 109, 52);
    public static readonly Color Red = Color.FromArgb(224, 57, 52);
    public static readonly Color RedDark = Color.FromArgb(113, 29, 28);

    public static Button CreateButton(string text, Color backColor)
    {
        var button = new Button
        {
            Text = text,
            AutoSize = true,
            MinimumSize = new Size(138, 42),
            Padding = new Padding(14, 6, 14, 6),
            FlatStyle = FlatStyle.Flat,
            BackColor = backColor,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 0, 10, 8),
            UseVisualStyleBackColor = false
        };
        button.FlatAppearance.BorderSize = 1;
        button.FlatAppearance.BorderColor = ControlPaint.Light(backColor, 0.15f);
        button.FlatAppearance.MouseOverBackColor = ControlPaint.Light(backColor, 0.12f);
        button.FlatAppearance.MouseDownBackColor = ControlPaint.Dark(backColor, 0.10f);
        return button;
    }

    public static void StyleTextBox(TextBox textBox)
    {
        textBox.BackColor = SurfaceAlt;
        textBox.ForeColor = Text;
        textBox.BorderStyle = BorderStyle.FixedSingle;
        textBox.Font = new Font("Segoe UI", 10f);
    }

    public static Label CreateSectionTitle(string text)
    {
        return new Label
        {
            Text = text,
            AutoSize = true,
            ForeColor = Gold,
            Font = new Font("Segoe UI", 11f, FontStyle.Bold),
            Margin = new Padding(0, 0, 0, 8)
        };
    }
}
