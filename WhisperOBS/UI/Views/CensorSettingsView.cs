using System.Text.RegularExpressions;

namespace WhisperOBS.UI.Views;

public sealed class CensorSettingsView : UserControl
{
    private static readonly string ConfigPath = Path.Combine(AppContext.BaseDirectory, "censor_words.txt");
    
    private static readonly HashSet<string> CensorCache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly object CacheLock = new();

    private readonly DataGridView _grid;
    private readonly TextBox _inputBox;

    static CensorSettingsView()
    {
        UpdateMemoryCache();
    }

    public CensorSettingsView()
    {
        Dock      = DockStyle.Fill;
        BackColor = Theme.Background;
        ForeColor = Theme.TextPrimary;
        Font      = Theme.FontBody;

        var mainLayout = new TableLayoutPanel
        {
            Dock        = DockStyle.Fill,
            RowCount    = 4,
            ColumnCount = 1,
            Padding     = new Padding(Theme.PadLg)
        };
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28f));  
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52f));  
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f)); 
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46f));  
        Controls.Add(mainLayout);

        mainLayout.Controls.Add(Theme.MakeEyebrow("PHRASE BLACKLIST DATATABLE"), 0, 0);

        var entryDeck = new TableLayoutPanel
        {
            Dock        = DockStyle.Fill,
            ColumnCount = 2,
            RowCount    = 1,
            Margin      = new Padding(0, 0, 0, Theme.PadSm)
        };
        entryDeck.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        entryDeck.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110f));
        mainLayout.Controls.Add(entryDeck, 0, 1);

        _inputBox = new TextBox
        {
            Dock        = DockStyle.Fill,
            BackColor   = Theme.Surface,
            ForeColor   = Theme.TextPrimary,
            Font        = Theme.FontMonoLarge,
            BorderStyle = BorderStyle.FixedSingle,
            Margin      = new Padding(0, 6, Theme.PadSm, 6)
        };
        _inputBox.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; AddPhrase(); } };
        entryDeck.Controls.Add(_inputBox, 0, 0);

        var addBtn = new ThemedButton("+ Add Phrase", ThemedButton.ButtonVariant.Primary)
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 4, 0, 4)
        };
        addBtn.Click += (s, e) => AddPhrase();
        entryDeck.Controls.Add(addBtn, 1, 0);

        _grid = new DataGridView
        {
            Dock   = DockStyle.Fill,
            Margin = new Padding(0, 0, 0, Theme.PadSm)
        };
        Theme.ApplyGridTheme(_grid);

        var textCol = new DataGridViewTextBoxColumn
        {
            Name         = "Phrase",
            HeaderText   = "BANNED STRING",
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            ReadOnly     = true
        };

        var actionCol = new DataGridViewButtonColumn
        {
            Name                        = "Delete",
            HeaderText                  = "ACTION",
            Width                       = 80,
            Text                        = "  [ ✖ ]",
            UseColumnTextForButtonValue = true,
            FlatStyle                   = FlatStyle.Flat
        };
        actionCol.DefaultCellStyle.ForeColor          = Theme.DangerRed;
        actionCol.DefaultCellStyle.SelectionForeColor = Theme.DangerRedHover;
        actionCol.DefaultCellStyle.Alignment          = DataGridViewContentAlignment.MiddleCenter;
        actionCol.DefaultCellStyle.Padding            = Padding.Empty;

        _grid.Columns.Add(textCol);
        _grid.Columns.Add(actionCol);
        _grid.CellContentClick += OnGridCellContentClick;
        mainLayout.Controls.Add(_grid, 0, 2);
        
        var footerDeck = new FlowLayoutPanel 
        { 
            Dock          = DockStyle.Fill, 
            FlowDirection = FlowDirection.RightToLeft,
            Margin        = new Padding(0, Theme.PadSm, 0, 0)
        };
        
        var saveBtn = new ThemedButton("Save Filter Changes", ThemedButton.ButtonVariant.Primary)
        {
            Width  = 160,
            Height = 36
        };
        saveBtn.Click += (s, e) => SaveWords();
        footerDeck.Controls.Add(saveBtn);
        mainLayout.Controls.Add(footerDeck, 0, 3);

        LoadWords();
    }

    private void AddPhrase()
    {
        string text = _inputBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(text)) return;

        bool exists = false;
        foreach (DataGridViewRow row in _grid.Rows)
        {
            if (string.Equals(row.Cells["Phrase"].Value?.ToString(), text, StringComparison.OrdinalIgnoreCase))
            {
                exists = true;
                break;
            }
        }

        if (!exists) _grid.Rows.Add(text);
        _inputBox.Clear();
        _inputBox.Focus();
    }

    private void OnGridCellContentClick(object sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex >= 0 && e.ColumnIndex == _grid.Columns["Delete"].Index)
        {
            _grid.Rows.RemoveAt(e.RowIndex);
        }
    }

    private void LoadWords()
    {
        _grid.Rows.Clear();
        if (File.Exists(ConfigPath))
        {
            foreach (var line in File.ReadAllLines(ConfigPath).Select(w => w.Trim()).Where(w => !string.IsNullOrWhiteSpace(w)))
            {
                _grid.Rows.Add(line);
            }
        }
    }

    private void SaveWords()
    {
        var finalItems = new List<string>();
        foreach (DataGridViewRow row in _grid.Rows)
        {
            var val = row.Cells["Phrase"].Value?.ToString()?.Trim();
            if (!string.IsNullOrWhiteSpace(val)) finalItems.Add(val);
        }
        
        File.WriteAllLines(ConfigPath, finalItems);
        UpdateMemoryCache();
    }

    private static void UpdateMemoryCache()
    {
        lock (CacheLock)
        {
            CensorCache.Clear();
            if (File.Exists(ConfigPath))
            {
                var lines = File.ReadAllLines(ConfigPath)
                                .Select(w => w.Trim())
                                .Where(w => !string.IsNullOrWhiteSpace(w));
                
                foreach (var line in lines)
                {
                    CensorCache.Add(line);
                }
            }
        }
    }

    /// <summary>
    /// Thread-safe, ultra-high-speed memory cache string pipeline evaluator.
    /// </summary>
    public static string FilterText(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return input;

        List<string> localTerms;
        lock (CacheLock)
        {
            if (CensorCache.Count == 0) return input;
            localTerms = [.. CensorCache];
        }

        string filtered = input;
        foreach (var word in localTerms)
        {
            string pattern = $@"\b{Regex.Escape(word)}\b";
            filtered = Regex.Replace(filtered, pattern, m => new string('*', m.Length), RegexOptions.IgnoreCase);
        }
        return filtered;
    }
}