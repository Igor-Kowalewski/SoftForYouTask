using CustomerCatalog.Core.Data;
using CustomerCatalog.Core.Models;
using CustomerCatalog.Core.Validation;

namespace CustomerCatalog.App.Forms;

/// <summary>
/// Detail / edit view for a customer. On save it sets DialogResult.OK; the caller
/// then persists the changes to the database and returns to the main view.
/// </summary>
public sealed class CustomerEditForm : Form
{
    // Fixed row height (rather than the TableLayoutPanel's default equal-percent rows) keeps
    // each row exactly as tall as a single-line input needs, so the label next to it can be
    // reliably centered in the same space instead of drifting within extra slack.
    private const float RowHeight = 32f;

    // Deterministic (not "whatever's left over") height for the error summary row: up to 7
    // fields can be invalid at once, some of whose Polish messages wrap to 2 lines - a
    // Percent row sizing to leftover space silently clipped the last line in testing.
    private const float ErrorRowHeight = 180f;

    private static readonly Color NormalBorderColor = SystemColors.Control;
    private static readonly Color InvalidBorderColor = Color.Firebrick;

    private readonly ICustomerRepository _repository;

    private readonly TextBox _nameBox = new();
    private readonly TextBox _nipBox = new();
    private readonly TextBox _streetBox = new();
    private readonly TextBox _postalCodeBox = new();
    private readonly TextBox _cityBox = new();
    private readonly TextBox _phoneBox = new();
    private readonly TextBox _emailBox = new();

    // Each textbox sits inside a Panel whose padding forms a visible "border" that turns red
    // when the field is invalid - a plain WinForms TextBox has no BorderColor property.
    private readonly Dictionary<TextBox, Panel> _fieldBorders = new();
    private readonly Dictionary<TextBox, string> _fieldErrors = new();
    private readonly Label _errorSummaryLabel = new();

    // Carried over from the customer being edited (or defaults, for a new one) since
    // CustomerValidator.TryValidate only knows how to build the editable fields.
    private readonly int _id;
    private readonly DateTime _createdAt;

    /// <summary>The validated customer, populated once the user clicks "Save".</summary>
    public Customer Customer { get; private set; } = null!;

    /// <summary>Pass null for <paramref name="existingCustomer"/> to create a new customer, or an existing one to edit it.</summary>
    public CustomerEditForm(ICustomerRepository repository, Customer? existingCustomer)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _id = existingCustomer?.Id ?? 0;
        _createdAt = existingCustomer?.CreatedAt ?? default;

        InitializeLayout(isNew: existingCustomer is null);
        HookInlineValidation();
        if (existingCustomer is not null)
            LoadFromCustomer(existingCustomer);
    }

    private void InitializeLayout(bool isNew)
    {
        Text = isNew ? "Nowy klient" : "Edycja klienta";
        Width = 460;
        Height = 560;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 8,
            Padding = new Padding(12),
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120)); // wide enough for "Kod pocztowy:"
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (var i = 0; i < 7; i++)
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, RowHeight));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, ErrorRowHeight));

        AddRow(layout, "Nazwa:", _nameBox, 0);
        AddRow(layout, "NIP:", _nipBox, 1);
        AddRow(layout, "Ulica:", _streetBox, 2);
        AddRow(layout, "Kod pocztowy:", _postalCodeBox, 3);
        AddRow(layout, "Miasto:", _cityBox, 4);
        AddRow(layout, "Telefon:", _phoneBox, 5);
        AddRow(layout, "E-mail:", _emailBox, 6);

        _errorSummaryLabel.Dock = DockStyle.Fill;
        _errorSummaryLabel.ForeColor = InvalidBorderColor;
        _errorSummaryLabel.TextAlign = ContentAlignment.TopLeft;
        _errorSummaryLabel.Margin = new Padding(3, 8, 3, 3);
        _errorSummaryLabel.Text = string.Empty;
        layout.Controls.Add(_errorSummaryLabel, 0, 7);
        layout.SetColumnSpan(_errorSummaryLabel, 2);

        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            FlowDirection = FlowDirection.RightToLeft,
            Height = 48,
            Padding = new Padding(8)
        };

        var saveButton = new Button { Text = "Zapisz", Width = 90, Height = 28 };
        saveButton.Click += OnSave;

        var cancelButton = new Button
        {
            Text = "Anuluj",
            Width = 90,
            Height = 28,
            DialogResult = DialogResult.Cancel
        };

        buttonPanel.Controls.Add(saveButton);
        buttonPanel.Controls.Add(cancelButton);

        Controls.Add(layout);
        Controls.Add(buttonPanel);

        AcceptButton = saveButton;
        CancelButton = cancelButton;
    }

    private void AddRow(TableLayoutPanel layout, string text, TextBox box, int row)
    {
        var label = new Label
        {
            Text = text,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = new Padding(3, 2, 3, 2)
        };
        layout.Controls.Add(label, 0, row);

        // 2px of this panel's own background shows around the (border-less) textbox,
        // acting as a border that can be recolored without owner-drawing anything.
        var border = new Panel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(3, 2, 3, 2),
            Padding = new Padding(2),
            BackColor = NormalBorderColor
        };

        box.Name = text;
        box.Dock = DockStyle.Fill;
        box.Margin = new Padding(0);
        border.Controls.Add(box);
        _fieldBorders[box] = border;

        layout.Controls.Add(border, 1, row);
    }

    /// <summary>
    /// (field, validator) pairs - the single source of truth for both wiring inline
    /// validation (on Leave) and re-validating everything eagerly before save.
    /// </summary>
    private IEnumerable<(TextBox Box, Func<string?> Validate)> FieldValidators()
    {
        yield return (_nameBox, () => CustomerValidator.ValidateName(_nameBox.Text));
        yield return (_nipBox, ValidateNipField);
        yield return (_streetBox, () => Address.ValidateStreet(_streetBox.Text));
        yield return (_postalCodeBox, () => Address.ValidatePostalCode(_postalCodeBox.Text));
        yield return (_cityBox, () => Address.ValidateCity(_cityBox.Text));
        yield return (_phoneBox, () => CustomerValidator.ValidatePhone(_phoneBox.Text));
        yield return (_emailBox, () => Email.Validate(_emailBox.Text));
    }

    /// <summary>
    /// Validates each field as soon as the user leaves it: highlights the field's border red
    /// and keeps the always-visible error summary in sync, using the exact same rules as the
    /// final save check - so inline feedback and the save-time gate can never disagree.
    /// </summary>
    private void HookInlineValidation()
    {
        foreach (var (box, validate) in FieldValidators())
            box.Leave += (_, _) => SetFieldError(box, validate());
    }

    /// <summary>Re-runs every field's validator, e.g. right before save so fields the user never
    /// tabbed through (Leave never fired) still get highlighted if they're invalid.</summary>
    private void ValidateAllFields()
    {
        foreach (var (box, validate) in FieldValidators())
            SetFieldError(box, validate());
    }

    private void SetFieldError(TextBox box, string? message)
    {
        if (message is null)
            _fieldErrors.Remove(box);
        else
            _fieldErrors[box] = message;

        _fieldBorders[box].BackColor = message is null ? NormalBorderColor : InvalidBorderColor;
        _errorSummaryLabel.Text = _fieldErrors.Count == 0
            ? string.Empty
            : "• " + string.Join("\n• ", _fieldErrors.Values);
    }

    /// <summary>
    /// Validates the NIP field's format and, when it parses, whether another customer already
    /// uses it. Shared by inline validation (on Leave) and the final save check.
    /// </summary>
    private string? ValidateNipField()
    {
        var formatError = Nip.Validate(_nipBox.Text);
        if (formatError is not null)
            return formatError;

        var nip = Nip.Parse(_nipBox.Text);
        return _repository.ExistsByNip(nip.Value, _id)
            ? $"Klient z numerem NIP {nip.Value} już istnieje w katalogu."
            : null;
    }

    private void LoadFromCustomer(Customer customer)
    {
        _nameBox.Text = customer.Name;
        _nipBox.Text = customer.Nip.Value;
        _streetBox.Text = customer.Address.Street;
        _postalCodeBox.Text = customer.Address.PostalCode;
        _cityBox.Text = customer.Address.City;
        _phoneBox.Text = customer.Phone;
        _emailBox.Text = customer.Email.Value;
    }

    private void OnSave(object? sender, EventArgs e)
    {
        // Re-validate every field (not just the ones the user happened to Tab through) so
        // nothing invalid slips past just because its Leave event never fired.
        ValidateAllFields();

        if (_fieldErrors.Count > 0)
        {
            // Send focus to the first problem field instead of failing silently.
            FieldValidators().Select(f => f.Box).First(box => _fieldErrors.ContainsKey(box)).Focus();
            return;
        }

        var input = new CustomerInput(
            _nameBox.Text, _nipBox.Text, _streetBox.Text, _postalCodeBox.Text, _cityBox.Text, _phoneBox.Text, _emailBox.Text);

        // Should always succeed here: ValidateAllFields() above already confirmed every field
        // individually, covering the exact same rules TryValidate applies while building Customer.
        if (!CustomerValidator.TryValidate(input, out var validated, out _))
            return;

        validated!.Id = _id;
        validated.CreatedAt = _createdAt;
        Customer = validated;

        DialogResult = DialogResult.OK;
        Close();
    }
}
