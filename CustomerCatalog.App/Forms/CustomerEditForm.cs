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

    private readonly ICustomerRepository _repository;
    private readonly ErrorProvider _errorProvider = new() { BlinkStyle = ErrorBlinkStyle.NeverBlink };

    private readonly TextBox _nameBox = new();
    private readonly TextBox _nipBox = new();
    private readonly TextBox _streetBox = new();
    private readonly TextBox _postalCodeBox = new();
    private readonly TextBox _cityBox = new();
    private readonly TextBox _phoneBox = new();
    private readonly TextBox _emailBox = new();

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
        Height = 360;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;

        _errorProvider.ContainerControl = this;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 7,
            Padding = new Padding(12),
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (var i = 0; i < layout.RowCount; i++)
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, RowHeight));

        AddRow(layout, "Nazwa:", _nameBox, 0);
        AddRow(layout, "NIP:", _nipBox, 1);
        AddRow(layout, "Ulica:", _streetBox, 2);
        AddRow(layout, "Kod pocztowy:", _postalCodeBox, 3);
        AddRow(layout, "Miasto:", _cityBox, 4);
        AddRow(layout, "Telefon:", _phoneBox, 5);
        AddRow(layout, "E-mail:", _emailBox, 6);

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

    private static void AddRow(TableLayoutPanel layout, string text, TextBox box, int row)
    {
        var label = new Label
        {
            Text = text,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = new Padding(3, 2, 3, 2)
        };
        layout.Controls.Add(label, 0, row);

        box.Dock = DockStyle.Fill;
        box.Margin = new Padding(3, 2, 3, 2);
        layout.Controls.Add(box, 1, row);
    }

    /// <summary>
    /// Wires each field to validate itself (and show a red icon via ErrorProvider) as soon as
    /// the user leaves it, using the exact same rules as the final save check below - so
    /// inline feedback and the save-time gate can never disagree.
    /// </summary>
    private void HookInlineValidation()
    {
        HookField(_nameBox, () => CustomerValidator.ValidateName(_nameBox.Text));
        HookField(_nipBox, ValidateNipField);
        HookField(_streetBox, () => Address.ValidateStreet(_streetBox.Text));
        HookField(_postalCodeBox, () => Address.ValidatePostalCode(_postalCodeBox.Text));
        HookField(_cityBox, () => Address.ValidateCity(_cityBox.Text));
        HookField(_phoneBox, () => CustomerValidator.ValidatePhone(_phoneBox.Text));
        HookField(_emailBox, () => Email.Validate(_emailBox.Text));
    }

    private void HookField(TextBox box, Func<string?> validate) =>
        box.Leave += (_, _) => _errorProvider.SetError(box, validate() ?? "");

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
        var input = new CustomerInput(
            _nameBox.Text, _nipBox.Text, _streetBox.Text, _postalCodeBox.Text, _cityBox.Text, _phoneBox.Text, _emailBox.Text);

        if (!CustomerValidator.TryValidate(input, out var validated, out var errors))
        {
            ShowValidationErrors(errors);
            return;
        }

        var duplicateNipError = ValidateNipField();
        if (duplicateNipError is not null)
        {
            ShowValidationErrors(new[] { duplicateNipError });
            return;
        }

        validated!.Id = _id;
        validated.CreatedAt = _createdAt;
        Customer = validated;

        DialogResult = DialogResult.OK;
        Close();
    }

    private static void ShowValidationErrors(IEnumerable<string> errors)
    {
        MessageBox.Show(
            "Popraw następujące błędy:\n\n• " + string.Join("\n• ", errors),
            "Walidacja",
            MessageBoxButtons.OK,
            MessageBoxIcon.Warning);
    }
}
