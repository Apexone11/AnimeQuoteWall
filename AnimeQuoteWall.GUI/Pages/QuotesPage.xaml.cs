using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using AnimeQuoteWall.Core.Configuration;
using AnimeQuoteWall.Core.Interfaces;
using AnimeQuoteWall.Core.Models;
using AnimeQuoteWall.Core.Services;

namespace AnimeQuoteWall.GUI.Pages;

/// <summary>
/// Page for managing quotes with filtering, favorites, and rating support.
/// </summary>
public partial class QuotesPage : Page
{
    private readonly IQuoteService _quoteService;
    private List<Quote> _quotes = new();
    private List<Quote> _filteredQuotes = new();
    private string _selectedCategory = "";
    private string _sortMode = "Default";
    private bool _favoritesOnly = false;

    public QuotesPage()
    {
        InitializeComponent();
        _quoteService = new QuoteService();
        Loaded += async (s, e) => await LoadQuotesAsync();
    }

    /// <summary>
    /// Loads quotes from the file and applies current filters.
    /// </summary>
    private async Task LoadQuotesAsync()
    {
        try
        {
            await _quoteService.EnsureQuotesFileAsync(AppConfiguration.QuotesFilePath).ConfigureAwait(false);
            _quotes = await _quoteService.LoadQuotesAsync(AppConfiguration.QuotesFilePath).ConfigureAwait(false);
            
            // Auto-categorize by anime name if no categories exist
            foreach (var quote in _quotes)
            {
                if (quote.Categories == null || !quote.Categories.Any())
                {
                    quote.Categories = new List<string> { quote.Anime };
                }
            }
            
            // Update category filter dropdown and apply filters on UI thread
            Dispatcher.Invoke(() =>
            {
                UpdateCategoryFilter();
                ApplyFilters();
            });
        }
        catch (Exception ex)
        {
            Dispatcher.Invoke(() =>
            {
                System.Windows.MessageBox.Show($"Failed to load quotes: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            });
        }
    }

    /// <summary>
    /// Updates the category filter dropdown with available categories.
    /// </summary>
    private void UpdateCategoryFilter()
    {
        if (CategoryFilterComboBox == null) return;

        var categories = _quotes
            .SelectMany(q => q.Categories ?? new List<string>())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(c => c)
            .ToList();

        CategoryFilterComboBox.Items.Clear();
        CategoryFilterComboBox.Items.Add(new ComboBoxItem { Content = "All Categories", Tag = "" });
        
        foreach (var category in categories)
        {
            CategoryFilterComboBox.Items.Add(new ComboBoxItem { Content = category, Tag = category });
        }
    }

    /// <summary>
    /// Applies current filters and sorting to the quotes list.
    /// </summary>
    private void ApplyFilters()
    {
        if (_quotes == null) return;

        _filteredQuotes = _quotes.ToList();

        // Filter by category
        if (!string.IsNullOrEmpty(_selectedCategory))
        {
            _filteredQuotes = _filteredQuotes.Where(q => 
                q.Categories != null && q.Categories.Contains(_selectedCategory, StringComparer.OrdinalIgnoreCase)
            ).ToList();
        }

        // Filter by favorites
        if (_favoritesOnly)
        {
            _filteredQuotes = _filteredQuotes.Where(q => q.IsFavorite).ToList();
        }

        // Apply sorting
        _filteredQuotes = _sortMode switch
        {
            "Rating" => _filteredQuotes.OrderByDescending(q => q.Rating).ThenBy(q => q.Text).ToList(),
            "Favorites" => _filteredQuotes.OrderByDescending(q => q.IsFavorite).ThenBy(q => q.Rating).ToList(),
            _ => _filteredQuotes.OrderBy(q => q.Text).ToList()
        };

        // Update UI
        Dispatcher.Invoke(() =>
        {
            QuotesListBox.ItemsSource = null;
            QuotesListBox.ItemsSource = _filteredQuotes;

            var quoteCountRun = (Run)QuoteCountTextBottom.Inlines.FirstOrDefault(i => i is Run r && r.Name == "QuoteCountRun");
            if (quoteCountRun != null)
            {
                quoteCountRun.Text = $"{_filteredQuotes.Count}";
            }
            else
            {
                QuoteCountTextBottom.Text = $"Total Quotes: {_filteredQuotes.Count}";
            }
        });
    }

    /// <summary>
    /// Handles the Add Quote button click event.
    /// Opens a dialog to add a new quote and saves it to the quotes file.
    /// </summary>
    private async void AddQuoteButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Show dialog for entering quote details
            var dialog = new SimpleQuoteDialog();
            if (dialog.ShowDialog() == true)
            {
                // Create new quote with dialog values
                var newQuote = new Quote
                {
                    Text = dialog.QuoteText,
                    Character = dialog.CharacterName,
                    Anime = dialog.AnimeName ?? "Unknown",
                    // Initialize new properties with defaults
                    Categories = new List<string> { dialog.AnimeName ?? "Unknown" },
                    Tags = new List<string>(),
                    IsFavorite = false,
                    Rating = 0
                };

                // Add to list and save
                _quotes.Add(newQuote);
                await _quoteService.SaveQuotesAsync(_quotes, AppConfiguration.QuotesFilePath).ConfigureAwait(false);
                await LoadQuotesAsync().ConfigureAwait(false);
                System.Windows.MessageBox.Show($"Quote added: \"{newQuote.Text}\"", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Failed to add quote: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Handles the Delete Quote button click event.
    /// Removes the selected quote after user confirmation.
    /// </summary>
    private async void DeleteQuoteButton_Click(object sender, RoutedEventArgs e)
    {
        if (QuotesListBox.SelectedItem is Quote selectedQuote)
        {
            // Confirm deletion with user
            var result = System.Windows.MessageBox.Show(
                $"Delete this quote?\n\n\"{selectedQuote.Text}\"\n— {selectedQuote.Character}",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    // Remove from list and save
                    _quotes.Remove(selectedQuote);
                    await _quoteService.SaveQuotesAsync(_quotes, AppConfiguration.QuotesFilePath).ConfigureAwait(false);
                    await LoadQuotesAsync().ConfigureAwait(false);
                    System.Windows.MessageBox.Show("Quote deleted.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Failed to delete quote: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        else
        {
            System.Windows.MessageBox.Show("Please select a quote to delete.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    /// <summary>
    /// Handles category filter selection change.
    /// </summary>
    private void CategoryFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CategoryFilterComboBox?.SelectedItem is ComboBoxItem item && item.Tag is string category)
        {
            _selectedCategory = category;
            ApplyFilters();
        }
    }

    /// <summary>
    /// Handles sort mode selection change.
    /// </summary>
    private void SortComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (SortComboBox?.SelectedItem is ComboBoxItem item && item.Tag is string sortMode)
        {
            _sortMode = sortMode;
            ApplyFilters();
        }
    }

    /// <summary>
    /// Handles favorites-only checkbox checked event.
    /// </summary>
    private void FavoritesOnlyCheckBox_Checked(object sender, RoutedEventArgs e)
    {
        _favoritesOnly = true;
        ApplyFilters();
    }

    /// <summary>
    /// Handles favorites-only checkbox unchecked event.
    /// </summary>
    private void FavoritesOnlyCheckBox_Unchecked(object sender, RoutedEventArgs e)
    {
        _favoritesOnly = false;
        ApplyFilters();
    }

    /// <summary>
    /// Handles favorite button click to toggle favorite status.
    /// </summary>
    private async void FavoriteButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button button && button.Tag is Quote quote)
        {
            try
            {
                quote.IsFavorite = !quote.IsFavorite;
                await _quoteService.SaveQuotesAsync(_quotes, AppConfiguration.QuotesFilePath).ConfigureAwait(false);
                ApplyFilters(); // Refresh to show updated favorite status
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to update favorite: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}

