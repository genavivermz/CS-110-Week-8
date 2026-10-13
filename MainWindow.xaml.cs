using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace OperationsHub;

public partial class MainWindow : Window
{
    private static readonly TimeSpan FocusDuration = TimeSpan.FromMinutes(25);
    private readonly string _storagePath;
    private readonly DispatcherTimer _timer;
    private readonly ObservableCollection<InstructionLink> _instructions = new();
    private readonly ObservableCollection<NotePad> _notes = new();
    private TimeSpan _timeRemaining = FocusDuration;

    public MainWindow()
    {
        InitializeComponent();
        _storagePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "OperationsHub",
            "workspace.json");

        LoadWorkspace();
        lstInstructions.ItemsSource = _instructions;
        lstNotes.ItemsSource = _notes;
        _instructions.CollectionChanged += (_, _) => SaveWorkspace();
        _notes.CollectionChanged += (_, _) => SaveWorkspace();

        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += Timer_Tick;
        UpdateTimerDisplay();
    }

    private void LoadWorkspace()
    {
        if (!File.Exists(_storagePath))
        {
            return;
        }

        try
        {
            string json = File.ReadAllText(_storagePath);
            WorkspaceData? data = JsonSerializer.Deserialize<WorkspaceData>(json);
            if (data is null)
            {
                throw new JsonException("The saved workspace was empty.");
            }

            foreach (InstructionLink link in data.Instructions ?? [])
            {
                if (Uri.TryCreate(link.Url, UriKind.Absolute, out Uri? uri) &&
                    (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
                {
                    _instructions.Add(new InstructionLink { Url = uri.ToString() });
                }
            }

            foreach (NotePad note in data.Notes ?? [])
            {
                _notes.Add(note);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            MessageBox.Show(
                $"Your saved workspace couldn't be loaded.\n\n{ex.Message}",
                "Unable to load workspace",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private void SaveWorkspace()
    {
        try
        {
            string? directory = Path.GetDirectoryName(_storagePath);
            if (directory is null)
            {
                throw new InvalidOperationException("The workspace save location is invalid.");
            }

            Directory.CreateDirectory(directory);
            var data = new WorkspaceData
            {
                Instructions = [.. _instructions],
                Notes = [.. _notes]
            };
            string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_storagePath, json);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or InvalidOperationException)
        {
            MessageBox.Show(
                $"Your workspace couldn't be saved.\n\n{ex.Message}",
                "Unable to save workspace",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void AddInstruction_Click(object sender, RoutedEventArgs e)
    {
        AddInstructionLink();
    }

    private void InstructionLink_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            AddInstructionLink();
            e.Handled = true;
        }
    }

    private void AddInstructionLink()
    {
        string input = txtInstructionLink.Text.Trim();
        if (!Uri.TryCreate(input, UriKind.Absolute, out Uri? uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            MessageBox.Show(
                "Enter a complete web link beginning with http:// or https://.",
                "Invalid link",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        var link = new InstructionLink { Url = uri.ToString() };
        _instructions.Add(link);
        lstInstructions.SelectedItem = link;
        txtInstructionLink.Clear();
    }

    private void RemoveInstruction_Click(object sender, RoutedEventArgs e)
    {
        if (lstInstructions.SelectedItem is InstructionLink link)
        {
            _instructions.Remove(link);
        }
    }

    private void OpenInstruction_Click(object sender, RoutedEventArgs e)
    {
        if (lstInstructions.SelectedItem is InstructionLink link)
        {
            OpenInstructionLink(link);
        }
    }

    private static void OpenInstructionLink(InstructionLink link)
    {
        try
        {
            Process.Start(new ProcessStartInfo(link.Url) { UseShellExecute = true });
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            MessageBox.Show(
                $"The link couldn't be opened.\n\n{ex.Message}",
                "Unable to open link",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private void Timer_Click(object sender, RoutedEventArgs e)
    {
        if (_timer.IsEnabled)
        {
            _timer.Stop();
            btnTimerToggle.Content = "Start";
        }
        else
        {
            if (_timeRemaining <= TimeSpan.Zero)
            {
                _timeRemaining = FocusDuration;
            }

            UpdateTimerDisplay();
            txtTimerStatus.Text = "FOCUS TIMER";
            btnTimerToggle.Content = "Pause";
            _timer.Start();
        }
    }

    private void Reset_Click(object sender, RoutedEventArgs e)
    {
        _timer.Stop();
        _timeRemaining = FocusDuration;
        btnTimerToggle.Content = "Start";
        txtTimerStatus.Text = "FOCUS TIMER";
        UpdateTimerDisplay();
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        _timeRemaining -= TimeSpan.FromSeconds(1);
        UpdateTimerDisplay();

        if (_timeRemaining <= TimeSpan.Zero)
        {
            _timeRemaining = TimeSpan.Zero;
            _timer.Stop();
            btnTimerToggle.Content = "Start";
            txtTimerStatus.Text = "SESSION COMPLETE";
            UpdateTimerDisplay();
            System.Media.SystemSounds.Exclamation.Play();
        }
    }

    private void UpdateTimerDisplay()
    {
        int minutes = (int)_timeRemaining.TotalMinutes;
        int seconds = _timeRemaining.Seconds;
        txtTimer.Text = $"{minutes:00}:{seconds:00}";
    }

    private void AddNote_Click(object sender, RoutedEventArgs e)
    {
        var note = new NotePad { Title = "New note" };
        _notes.Add(note);
        lstNotes.SelectedItem = note;
        txtNoteTitle.Focus();
        txtNoteTitle.SelectAll();
    }

    private void RemoveNote_Click(object sender, RoutedEventArgs e)
    {
        if (lstNotes.SelectedItem is NotePad note)
        {
            _notes.Remove(note);
        }
    }

    private void Notes_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (lstNotes.SelectedItem is NotePad note)
        {
            txtNoteTitle.IsEnabled = true;
            txtNoteContent.IsEnabled = true;
            txtNoteTitle.Text = note.Title;
            txtNoteContent.Text = note.Content;
        }
        else
        {
            txtNoteTitle.IsEnabled = false;
            txtNoteContent.IsEnabled = false;
            txtNoteTitle.Clear();
            txtNoteContent.Clear();
        }
    }

    private void NoteEditor_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (lstNotes.SelectedItem is not NotePad note)
        {
            return;
        }

        if (sender == txtNoteTitle)
        {
            note.Title = txtNoteTitle.Text;
        }
        else if (sender == txtNoteContent)
        {
            note.Content = txtNoteContent.Text;
        }
    }

    private void NoteEditor_LostFocus(object sender, RoutedEventArgs e)
    {
        SaveWorkspace();
    }

    private void Window_Closing(object? sender, CancelEventArgs e)
    {
        SaveWorkspace();
    }
}

internal sealed class InstructionLink
{
    public string Url { get; set; } = string.Empty;
}

internal sealed class NotePad : INotifyPropertyChanged
{
    private string _title = string.Empty;
    private string _content = string.Empty;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Title
    {
        get => _title;
        set
        {
            if (_title == value)
            {
                return;
            }

            _title = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Title)));
        }
    }

    public string Content
    {
        get => _content;
        set
        {
            if (_content == value)
            {
                return;
            }

            _content = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Content)));
        }
    }
}

internal sealed class WorkspaceData
{
    public List<InstructionLink> Instructions { get; set; } = [];
    public List<NotePad> Notes { get; set; } = [];
}
