using System.Windows;

namespace AnimeQuoteWall.GUI.Dialogs;

public partial class AddQuoteDialog : Window
{
    public string QuoteText { get; private set; } = string.Empty;
    public string CharacterName { get; private set; } = string.Empty;
    public string? AnimeName { get; private set; }

    public AddQuoteDialog()
    {
        InitializeComponent();
        Loaded += (_, _) => QuoteTextBox.Focus();
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        var quote = QuoteTextBox.Text?.Trim() ?? string.Empty;
        var character = CharacterTextBox.Text?.Trim() ?? string.Empty;
        var anime = AnimeTextBox.Text?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(quote) || string.IsNullOrWhiteSpace(character))
        {
            System.Windows.MessageBox.Show(this, "Please fill in both the quote and the character name.", "Missing information", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        QuoteText = quote;
        CharacterName = character;
        AnimeName = string.IsNullOrWhiteSpace(anime) ? null : anime;

        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
