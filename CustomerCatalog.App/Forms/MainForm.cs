using CustomerCatalog.Core.Data;
using CustomerCatalog.Core.Models;
using Serilog;

namespace CustomerCatalog.App.Forms;

/// <summary>
/// Główny widok – lista klientów z możliwością sortowania, filtrowania,
/// dodawania, edycji (dwuklik) i usuwania.
/// </summary>
public sealed class MainForm : Form
{
    private readonly ICustomerRepository _repository;

    private readonly DataGridView _grid = new();
    private readonly TextBox _filterBox = new();
    private readonly Label _statusLabel = new();
    private readonly BindingSource _bindingSource = new();

    private List<Customer> _allCustomers = new();
    private string _sortProperty = nameof(Customer.Name);
    private bool _sortAscending = true;

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

        // --- Górny pasek: filtr + przyciski ---
        var topPanel = new Panel { Dock = DockStyle.Top, Height = 44, Padding = new Padding(8) };

        var filterLabel = new Label
        {
            Text = "Filtruj:",
            AutoSize = true,
            Location = new Point(8, 14)
        };

        _filterBox.Location = new Point(60, 10);
        _filterBox.Width = 260;
        _filterBox.PlaceholderText = "nazwa, NIP, e-mail, telefon, adres...";
        _filterBox.TextChanged += (_, _) => RefreshView();

        var addButton = MakeButton("Dodaj", 340);
        addButton.Click += (_, _) => AddCustomer();

        var editButton = MakeButton("Edytuj", 428);
        editButton.Click += (_, _) => EditSelected();

        var deleteButton = MakeButton("Usuń", 516);
        deleteButton.Click += (_, _) => DeleteSelected();

        var refreshButton = MakeButton("Odśwież", 604);
        refreshButton.Click += (_, _) => LoadCustomers();

        topPanel.Controls.AddRange(new Control[]
        {
            filterLabel, _filterBox, addButton, editButton, deleteButton, refreshButton
        });

        // --- Grid ---
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
            MakeColumn(nameof(Customer.CreatedAt), "Utworzono", 120));

        _grid.CellDoubleClick += (_, e) =>
        {
            if (e.RowIndex >= 0)
                EditSelected();
        };
        _grid.ColumnHeaderMouseClick += OnColumnHeaderClicked;

        // --- Dolny pasek statusu ---
        var bottomPanel = new Panel { Dock = DockStyle.Bottom, Height = 26 };
        _statusLabel.Dock = DockStyle.Fill;
        _statusLabel.TextAlign = ContentAlignment.MiddleLeft;
        _statusLabel.Padding = new Padding(8, 0, 0, 0);
        bottomPanel.Controls.Add(_statusLabel);

        Controls.Add(_grid);
        Controls.Add(topPanel);
        Controls.Add(bottomPanel);
    }

    private static Button MakeButton(string text, int x) => new()
    {
        Text = text,
        Location = new Point(x, 8),
        Width = 82,
        Height = 26
    };

    private static DataGridViewTextBoxColumn MakeColumn(string propertyName, string header, int width) => new()
    {
        DataPropertyName = propertyName,
        HeaderText = header,
        Name = propertyName,
        FillWeight = width,
        SortMode = DataGridViewColumnSortMode.Programmatic
    };

    private void LoadCustomers()
    {
        try
        {
            _allCustomers = _repository.GetAll().ToList();
            RefreshView();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Nie udało się wczytać listy klientów.");
            MessageBox.Show(
                $"Nie udało się wczytać danych:\n{ex.Message}",
                "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /// <summary>Zastosowanie aktualnego filtra i sortowania oraz przepięcie źródła danych.</summary>
    private void RefreshView()
    {
        var filter = _filterBox.Text.Trim();

        IEnumerable<Customer> view = _allCustomers;

        if (filter.Length > 0)
        {
            view = view.Where(c =>
                Contains(c.Name, filter) ||
                Contains(c.Nip, filter) ||
                Contains(c.Email, filter) ||
                Contains(c.Phone, filter) ||
                Contains(c.Address, filter));
        }

        view = ApplySort(view);

        var list = view.ToList();
        _bindingSource.DataSource = list;
        UpdateSortGlyphs();
        _statusLabel.Text = $"Klientów: {list.Count} (z {_allCustomers.Count})";
    }

    private static bool Contains(string? value, string term) =>
        value is not null && value.Contains(term, StringComparison.OrdinalIgnoreCase);

    private IEnumerable<Customer> ApplySort(IEnumerable<Customer> source)
    {
        Func<Customer, object?> key = _sortProperty switch
        {
            nameof(Customer.Nip) => c => c.Nip,
            nameof(Customer.Address) => c => c.Address,
            nameof(Customer.Phone) => c => c.Phone,
            nameof(Customer.Email) => c => c.Email,
            nameof(Customer.CreatedAt) => c => c.CreatedAt,
            _ => c => c.Name
        };

        return _sortAscending
            ? source.OrderBy(key)
            : source.OrderByDescending(key);
    }

    private void OnColumnHeaderClicked(object? sender, DataGridViewCellMouseEventArgs e)
    {
        var property = _grid.Columns[e.ColumnIndex].DataPropertyName;
        if (string.IsNullOrEmpty(property))
            return;

        if (_sortProperty == property)
            _sortAscending = !_sortAscending;
        else
        {
            _sortProperty = property;
            _sortAscending = true;
        }

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
        using var form = new CustomerEditForm(new Customer());
        if (form.ShowDialog(this) != DialogResult.OK)
            return;

        try
        {
            _repository.Insert(form.Customer);
            Log.Information("Dodano klienta {Name} (Id {Id}).", form.Customer.Name, form.Customer.Id);
            LoadCustomers();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Nie udało się dodać klienta.");
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

        // Edycja na kopii – jeśli użytkownik anuluje, oryginał pozostaje nietknięty.
        using var form = new CustomerEditForm(selected.Clone());
        if (form.ShowDialog(this) != DialogResult.OK)
            return;

        try
        {
            _repository.Update(form.Customer);
            Log.Information("Zaktualizowano klienta {Name} (Id {Id}).", form.Customer.Name, form.Customer.Id);
            LoadCustomers();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Nie udało się zaktualizować klienta {Id}.", form.Customer.Id);
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
            Log.Information("Usunięto klienta {Name} (Id {Id}).", selected.Name, selected.Id);
            LoadCustomers();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Nie udało się usunąć klienta {Id}.", selected.Id);
            MessageBox.Show($"Nie udało się usunąć: {ex.Message}", "Błąd",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
