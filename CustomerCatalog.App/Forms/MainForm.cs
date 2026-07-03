using CustomerCatalog.Core.Data;
using CustomerCatalog.Core.Models;
using CustomerCatalog.Core.Services;
using Serilog;

namespace CustomerCatalog.App.Forms;

/// <summary>
/// Main view – customer list with sorting, filtering, adding, editing
/// (double-click) and deleting.
/// </summary>
public sealed class MainForm : Form
{
    // DataGridView stays smooth with a few hundred bound rows; binding all 10,000+ seeded
    // customers at once is what made window resizing feel laggy, hence pagination.
    private const int PageSize = 200;

    private readonly ICustomerRepository _repository;

    private readonly DataGridView _grid = new();
    private readonly TextBox _filterBox = new();
    private readonly Label _statusLabel = new();
    private readonly Label _pageLabel = new();
    private readonly Button _prevPageButton = new() { Text = "◀ Poprzednia" };
    private readonly Button _nextPageButton = new() { Text = "Następna ▶" };
    private readonly BindingSource _bindingSource = new();

    private List<Customer> _allCustomers = new();
    private string _sortProperty = nameof(Customer.Name);
    private bool _sortAscending = true;
    private int _currentPage = 1;

    public MainForm(ICustomerRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));

        InitializeLayout();
        LoadCustomers();
    }

    private void InitializeLayout()
    {
        Text = "Katalog klientów";
        Width = 1000;
        Height = 600;
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(700, 400);

        Controls.Add(_grid);
        Controls.Add(BuildToolbar());
        Controls.Add(BuildStatusBar());

        ConfigureGrid();
    }

    private Control BuildToolbar()
    {
        // FlowLayoutPanel lays controls out left-to-right, so no manual coordinates are needed.
        var toolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 44,
            Padding = new Padding(8, 8, 8, 8),
            WrapContents = false
        };

        var filterLabel = new Label
        {
            Text = "Filtruj:",
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(3, 8, 3, 3)
        };

        _filterBox.Width = 260;
        _filterBox.Margin = new Padding(3, 4, 12, 3);
        _filterBox.PlaceholderText = "nazwa, NIP, e-mail, telefon, adres...";
        _filterBox.TextChanged += (_, _) => { _currentPage = 1; RefreshView(); };

        toolbar.Controls.Add(filterLabel);
        toolbar.Controls.Add(_filterBox);
        toolbar.Controls.Add(MakeButton("Dodaj", AddCustomer));
        toolbar.Controls.Add(MakeButton("Edytuj", EditSelected));
        toolbar.Controls.Add(MakeButton("Usuń", DeleteSelected));
        toolbar.Controls.Add(MakeButton("Odśwież", LoadCustomers));

        return toolbar;
    }

    private Control BuildStatusBar()
    {
        var bottomPanel = new Panel { Dock = DockStyle.Bottom, Height = 34 };

        _statusLabel.Dock = DockStyle.Fill;
        _statusLabel.TextAlign = ContentAlignment.MiddleLeft;
        _statusLabel.Padding = new Padding(8, 0, 0, 0);

        var pagingPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            FlowDirection = FlowDirection.LeftToRight,
            AutoSize = true,
            Padding = new Padding(0, 3, 8, 3)
        };

        _prevPageButton.AutoSize = true;
        _prevPageButton.Margin = new Padding(3, 0, 3, 0);
        _prevPageButton.Click += (_, _) => ChangePage(_currentPage - 1);

        _pageLabel.AutoSize = true;
        _pageLabel.TextAlign = ContentAlignment.MiddleCenter;
        _pageLabel.Margin = new Padding(8, 6, 8, 0);

        _nextPageButton.AutoSize = true;
        _nextPageButton.Margin = new Padding(3, 0, 3, 0);
        _nextPageButton.Click += (_, _) => ChangePage(_currentPage + 1);

        pagingPanel.Controls.Add(_prevPageButton);
        pagingPanel.Controls.Add(_pageLabel);
        pagingPanel.Controls.Add(_nextPageButton);

        bottomPanel.Controls.Add(_statusLabel);
        bottomPanel.Controls.Add(pagingPanel);
        return bottomPanel;
    }

    private void ChangePage(int page)
    {
        _currentPage = page;
        RefreshView();
    }

    private void ConfigureGrid()
    {
        _grid.Dock = DockStyle.Fill;
        _grid.AutoGenerateColumns = false;
        _grid.ReadOnly = true;
        _grid.AllowUserToAddRows = false;
        _grid.AllowUserToDeleteRows = false;
        _grid.MultiSelect = false;
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _grid.RowHeadersVisible = false;
        _grid.BackgroundColor = SystemColors.Window;
        _grid.DataSource = _bindingSource;

        _grid.Columns.AddRange(
            MakeColumn(nameof(Customer.Name), "Nazwa", 180),
            MakeColumn(nameof(Customer.Nip), "NIP", 90),
            MakeColumn(nameof(Customer.Address), "Adres", 220),
            MakeColumn(nameof(Customer.Phone), "Telefon", 100),
            MakeColumn(nameof(Customer.Email), "E-mail", 160),
            MakeColumn(nameof(Customer.CreatedAt), "Utworzono", 120, format: "yyyy-MM-dd HH:mm"));

        _grid.CellDoubleClick += (_, e) =>
        {
            if (e.RowIndex >= 0)
                EditSelected();
        };
        _grid.ColumnHeaderMouseClick += OnColumnHeaderClicked;
    }

    private static Button MakeButton(string text, Action onClick)
    {
        var button = new Button
        {
            Text = text,
            AutoSize = true,
            Height = 26,
            Margin = new Padding(3, 4, 3, 3),
            Padding = new Padding(6, 0, 6, 0)
        };
        button.Click += (_, _) => onClick();
        return button;
    }

    private static DataGridViewTextBoxColumn MakeColumn(string propertyName, string header, int width, string? format = null)
    {
        var column = new DataGridViewTextBoxColumn
        {
            DataPropertyName = propertyName,
            HeaderText = header,
            Name = propertyName,
            FillWeight = width,
            SortMode = DataGridViewColumnSortMode.Programmatic
        };
        if (format is not null)
            column.DefaultCellStyle.Format = format;
        return column;
    }

    private void LoadCustomers()
    {
        try
        {
            _allCustomers = _repository.GetAll().ToList();
            RefreshView();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load the customer list.");
            MessageBox.Show(
                $"Nie udało się wczytać danych:\n{ex.Message}",
                "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /// <summary>Applies the current filter and sort, then rebinds the data source to just the current page.</summary>
    private void RefreshView()
    {
        var filtered = CustomerQuery.Filter(_allCustomers, _filterBox.Text);
        var sorted = CustomerQuery.Sort(filtered, _sortProperty, _sortAscending).ToList();

        var pageCount = CustomerQuery.PageCount(sorted.Count, PageSize);
        _currentPage = Math.Clamp(_currentPage, 1, pageCount);

        _bindingSource.DataSource = CustomerQuery.Paginate(sorted, _currentPage, PageSize).ToList();
        UpdateSortGlyphs();

        _statusLabel.Text = $"Klientów: {sorted.Count} (z {_allCustomers.Count})";
        _pageLabel.Text = $"Strona {_currentPage} z {pageCount}";
        _prevPageButton.Enabled = _currentPage > 1;
        _nextPageButton.Enabled = _currentPage < pageCount;
    }

    private void OnColumnHeaderClicked(object? sender, DataGridViewCellMouseEventArgs e)
    {
        var property = _grid.Columns[e.ColumnIndex].DataPropertyName;
        if (string.IsNullOrEmpty(property))
            return;

        // Clicking the active column toggles direction; a new column starts ascending.
        if (_sortProperty == property)
            _sortAscending = !_sortAscending;
        else
        {
            _sortProperty = property;
            _sortAscending = true;
        }

        _currentPage = 1;
        RefreshView();
    }

    private void UpdateSortGlyphs()
    {
        foreach (DataGridViewColumn column in _grid.Columns)
        {
            column.HeaderCell.SortGlyphDirection = column.DataPropertyName == _sortProperty
                ? (_sortAscending ? SortOrder.Ascending : SortOrder.Descending)
                : SortOrder.None;
        }
    }

    private Customer? GetSelectedCustomer() =>
        _grid.CurrentRow?.DataBoundItem as Customer;

    private void AddCustomer()
    {
        using var form = new CustomerEditForm(_repository, null);
        if (form.ShowDialog(this) != DialogResult.OK)
            return;

        try
        {
            _repository.Insert(form.Customer);
            Log.Information("Added customer {Name} (Id {Id}).", form.Customer.Name, form.Customer.Id);
            LoadCustomers();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to add a customer.");
            MessageBox.Show($"Nie udało się zapisać: {ex.Message}", "Błąd",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void EditSelected()
    {
        var selected = GetSelectedCustomer();
        if (selected is null)
        {
            MessageBox.Show("Wybierz klienta do edycji.", "Informacja",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        // CustomerEditForm only reads from the passed-in customer to prefill its fields and
        // never mutates it, so the original stays untouched if the user cancels.
        using var form = new CustomerEditForm(_repository, selected);
        if (form.ShowDialog(this) != DialogResult.OK)
            return;

        try
        {
            _repository.Update(form.Customer);
            Log.Information("Updated customer {Name} (Id {Id}).", form.Customer.Name, form.Customer.Id);
            LoadCustomers();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to update customer {Id}.", form.Customer.Id);
            MessageBox.Show($"Nie udało się zapisać: {ex.Message}", "Błąd",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void DeleteSelected()
    {
        var selected = GetSelectedCustomer();
        if (selected is null)
        {
            MessageBox.Show("Wybierz klienta do usunięcia.", "Informacja",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var confirm = MessageBox.Show(
            $"Czy na pewno usunąć klienta \"{selected.Name}\"?",
            "Potwierdzenie",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (confirm != DialogResult.Yes)
            return;

        try
        {
            _repository.Delete(selected.Id);
            Log.Information("Deleted customer {Name} (Id {Id}).", selected.Name, selected.Id);
            LoadCustomers();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to delete customer {Id}.", selected.Id);
            MessageBox.Show($"Nie udało się usunąć: {ex.Message}", "Błąd",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
