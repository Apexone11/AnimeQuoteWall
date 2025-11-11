using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using AnimeQuoteWall.Core.Configuration;
using AnimeQuoteWall.Core.Models;
using AnimeQuoteWall.Core.Services;

namespace AnimeQuoteWall.GUI.Pages;

/// <summary>
/// Page for managing playlists with schedule configuration.
/// </summary>
public partial class PlaylistsPage : Page
{
    private readonly PlaylistService _playlistService;
    private readonly ScheduleService _scheduleService;
    private List<Playlist> _playlists = new();
    private Playlist? _selectedPlaylist;

    public PlaylistsPage()
    {
        InitializeComponent();
        _playlistService = new PlaylistService();
        _scheduleService = new ScheduleService();
        Loaded += async (s, e) => await LoadPlaylistsAsync();
    }

    /// <summary>
    /// Loads all playlists from storage.
    /// </summary>
    private async Task LoadPlaylistsAsync()
    {
        try
        {
            _playlists = await _playlistService.LoadAllPlaylistsAsync().ConfigureAwait(false);
            
            Dispatcher.Invoke(() =>
            {
                UpdatePlaylistList();
            });
        }
        catch (Exception ex)
        {
            Dispatcher.Invoke(() =>
            {
                System.Windows.MessageBox.Show($"Failed to load playlists: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            });
        }
    }

    /// <summary>
    /// Updates the playlist list display.
    /// </summary>
    private void UpdatePlaylistList()
    {
        if (PlaylistsListBox == null) return;

        PlaylistsListBox.ItemsSource = null;
        PlaylistsListBox.ItemsSource = _playlists;

        if (PlaylistCountRun != null)
        {
            PlaylistCountRun.Text = _playlists.Count.ToString();
        }
    }

    /// <summary>
    /// Handles the Create Playlist button click.
    /// </summary>
    private async void CreatePlaylistButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var dialog = new SimplePlaylistDialog();
            if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.PlaylistName))
            {
                var playlist = await _playlistService.CreatePlaylistAsync(dialog.PlaylistName).ConfigureAwait(false);
                
                // Open edit dialog to configure the playlist
                var editDialog = new PlaylistEditDialog(playlist, _playlistService, _scheduleService);
                if (editDialog.ShowDialog() == true)
                {
                    await LoadPlaylistsAsync().ConfigureAwait(false);
                    System.Windows.MessageBox.Show("Playlist created successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Failed to create playlist: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Handles the Edit Playlist button click.
    /// </summary>
    private async void EditPlaylistButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedPlaylist == null) return;

        try
        {
            var dialog = new PlaylistEditDialog(_selectedPlaylist, _playlistService, _scheduleService);
            if (dialog.ShowDialog() == true)
            {
                await LoadPlaylistsAsync().ConfigureAwait(false);
                System.Windows.MessageBox.Show("Playlist updated successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Failed to edit playlist: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Handles the Delete Playlist button click.
    /// </summary>
    private async void DeletePlaylistButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedPlaylist == null) return;

        var result = System.Windows.MessageBox.Show(
            $"Are you sure you want to delete the playlist '{_selectedPlaylist.Name}'?",
            "Confirm Delete",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            try
            {
                await _playlistService.DeletePlaylistAsync(_selectedPlaylist.Id).ConfigureAwait(false);
                _selectedPlaylist = null;
                await LoadPlaylistsAsync().ConfigureAwait(false);
                System.Windows.MessageBox.Show("Playlist deleted successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to delete playlist: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    /// <summary>
    /// Handles playlist list selection changes.
    /// </summary>
    private void PlaylistsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _selectedPlaylist = PlaylistsListBox?.SelectedItem as Playlist;
        
        if (EditPlaylistButton != null)
            EditPlaylistButton.IsEnabled = _selectedPlaylist != null;
        
        if (DeletePlaylistButton != null)
            DeletePlaylistButton.IsEnabled = _selectedPlaylist != null;
    }

    /// <summary>
    /// Handles the enable/disable toggle button click.
    /// </summary>
    private async void EnableToggleButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Primitives.ToggleButton button && button.Tag is Playlist playlist)
        {
            try
            {
                if (playlist.Enabled)
                {
                    await _playlistService.EnablePlaylistAsync(playlist.Id).ConfigureAwait(false);
                }
                else
                {
                    await _playlistService.DisableAllPlaylistsAsync().ConfigureAwait(false);
                }
                
                await LoadPlaylistsAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to update playlist: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                await LoadPlaylistsAsync().ConfigureAwait(false);
            }
        }
    }
}

/// <summary>
/// Simple dialog for entering a playlist name.
/// </summary>
public partial class SimplePlaylistDialog : Window
{
    public string PlaylistName { get; private set; } = "";

    public SimplePlaylistDialog()
    {
        Title = "Create New Playlist";
        Width = 400;
        Height = 200;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ResizeMode = ResizeMode.NoResize;

        var grid = new Grid { Margin = new Thickness(20) };
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var nameLabel = new TextBlock
        {
            Text = "Playlist Name:",
            FontSize = 14,
            Margin = new Thickness(0, 0, 0, 10)
        };
        Grid.SetRow(nameLabel, 0);
        grid.Children.Add(nameLabel);

        var nameTextBox = new System.Windows.Controls.TextBox
        {
            Name = "NameTextBox",
            FontSize = 14,
            Margin = new Thickness(0, 0, 0, 20),
            Padding = new Thickness(8)
        };
        Grid.SetRow(nameTextBox, 1);
        grid.Children.Add(nameTextBox);

        var buttonPanel = new StackPanel
        {
            Orientation = System.Windows.Controls.Orientation.Horizontal,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Right
        };

        var cancelButton = new System.Windows.Controls.Button
        {
            Content = "Cancel",
            MinWidth = 80,
            Height = 32,
            Margin = new Thickness(0, 0, 10, 0)
        };
        cancelButton.Click += (s, e) => { DialogResult = false; Close(); };

        var okButton = new System.Windows.Controls.Button
        {
            Content = "Create",
            MinWidth = 80,
            Height = 32
        };
        okButton.Click += (s, e) =>
        {
            var textBox = grid.Children.OfType<System.Windows.Controls.TextBox>().FirstOrDefault();
            if (textBox != null && !string.IsNullOrWhiteSpace(textBox.Text))
            {
                PlaylistName = textBox.Text.Trim();
                DialogResult = true;
                Close();
            }
            else
            {
                System.Windows.MessageBox.Show("Please enter a playlist name.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        };

        buttonPanel.Children.Add(cancelButton);
        buttonPanel.Children.Add(okButton);
        Grid.SetRow(buttonPanel, 2);
        grid.Children.Add(buttonPanel);

        Content = grid;
        nameTextBox.Focus();
    }
}

/// <summary>
/// Dialog for editing playlist details and schedule configuration.
/// </summary>
public partial class PlaylistEditDialog : Window
{
    private readonly Playlist _playlist;
    private readonly PlaylistService _playlistService;
    private readonly ScheduleService _scheduleService;

    public PlaylistEditDialog(Playlist playlist, PlaylistService playlistService, ScheduleService scheduleService)
    {
        _playlist = playlist;
        _playlistService = playlistService;
        _scheduleService = scheduleService;

        Title = $"Edit Playlist: {playlist.Name}";
        Width = 600;
        Height = 500;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ResizeMode = ResizeMode.NoResize;

        var scrollViewer = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Padding = new Thickness(20)
        };

        var stackPanel = new StackPanel();

        // Name
        var nameLabel = new TextBlock { Text = "Name:", FontSize = 14, Margin = new Thickness(0, 0, 0, 5) };
        var nameTextBox = new System.Windows.Controls.TextBox { Text = playlist.Name, FontSize = 14, Margin = new Thickness(0, 0, 0, 15), Padding = new Thickness(8) };
        stackPanel.Children.Add(nameLabel);
        stackPanel.Children.Add(nameTextBox);

        // Schedule Type
        var scheduleLabel = new TextBlock { Text = "Schedule Type:", FontSize = 14, Margin = new Thickness(0, 0, 0, 5) };
        var scheduleComboBox = new System.Windows.Controls.ComboBox
        {
            ItemsSource = new[] { "Interval", "Hourly", "Daily", "OnLaunch", "Custom" },
            SelectedItem = playlist.ScheduleType,
            FontSize = 14,
            Margin = new Thickness(0, 0, 0, 15),
            Padding = new Thickness(8)
        };
        stackPanel.Children.Add(scheduleLabel);
        stackPanel.Children.Add(scheduleComboBox);

        // Interval Seconds (for Interval type)
        var intervalLabel = new TextBlock { Text = "Interval (seconds):", FontSize = 14, Margin = new Thickness(0, 0, 0, 5) };
        var intervalTextBox = new System.Windows.Controls.TextBox
        {
            Text = playlist.IntervalSeconds.ToString(),
            FontSize = 14,
            Margin = new Thickness(0, 0, 0, 15),
            Padding = new Thickness(8)
        };
        stackPanel.Children.Add(intervalLabel);
        stackPanel.Children.Add(intervalTextBox);

        // Schedule Time (for Daily/Custom)
        var timeLabel = new TextBlock { Text = "Schedule Time (HH:mm):", FontSize = 14, Margin = new Thickness(0, 0, 0, 5) };
        var timeTextBox = new System.Windows.Controls.TextBox
        {
            Text = playlist.ScheduleTime ?? "12:00",
            FontSize = 14,
            Margin = new Thickness(0, 0, 0, 15),
            Padding = new Thickness(8)
        };
        stackPanel.Children.Add(timeLabel);
        stackPanel.Children.Add(timeTextBox);

        // Shuffle Mode
        var shuffleCheckBox = new System.Windows.Controls.CheckBox
        {
            Content = "Shuffle Mode",
            IsChecked = playlist.ShuffleMode,
            FontSize = 14,
            Margin = new Thickness(0, 0, 0, 15)
        };
        stackPanel.Children.Add(shuffleCheckBox);

        // Buttons
        var buttonPanel = new StackPanel
        {
            Orientation = System.Windows.Controls.Orientation.Horizontal,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
            Margin = new Thickness(0, 20, 0, 0)
        };

        var cancelButton = new System.Windows.Controls.Button
        {
            Content = "Cancel",
            MinWidth = 80,
            Height = 32,
            Margin = new Thickness(0, 0, 10, 0)
        };
        cancelButton.Click += (s, e) => { DialogResult = false; Close(); };

        var saveButton = new System.Windows.Controls.Button
        {
            Content = "Save",
            MinWidth = 80,
            Height = 32
        };
        saveButton.Click += async (s, e) =>
            {
                try
                {
                    _playlist.Name = nameTextBox.Text.Trim();
                    _playlist.ScheduleType = scheduleComboBox.SelectedItem?.ToString() ?? "Interval";
                    _playlist.ShuffleMode = shuffleCheckBox.IsChecked ?? false;

                    if (int.TryParse(intervalTextBox.Text, out var interval))
                        _playlist.IntervalSeconds = interval;

                    _playlist.ScheduleTime = timeTextBox.Text.Trim();

                    await _playlistService.UpdatePlaylistAsync(_playlist).ConfigureAwait(false);
                    DialogResult = true;
                    Close();
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Failed to save playlist: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };

        buttonPanel.Children.Add(cancelButton);
        buttonPanel.Children.Add(saveButton);
        stackPanel.Children.Add(buttonPanel);

        scrollViewer.Content = stackPanel;
        Content = scrollViewer;
    }
}

