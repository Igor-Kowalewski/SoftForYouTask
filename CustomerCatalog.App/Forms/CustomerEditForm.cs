using CustomerCatalog.Core.Models;
using CustomerCatalog.Core.Validation;

namespace CustomerCatalog.App.Forms;

/// <summary>
/// Detail / edit view for a customer. On save it sets DialogResult.OK; the caller
/// then persists the changes to the database and returns to the main view.
/// </summary>
public sealed class CustomerEditForm : Form
{
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

    /// <summary>Pass null to create a new customer, or an existing one to edit it.</summary>
    public CustomerEditForm(Customer? existingCustomer)
    {
        _id = existingCustomer?.Id ?? 0;
        _createdAt = existingCustomer?.CreatedAt ?? default;

        InitializeLayout(isNew: existingCustomer is null);
        if (existingCustomer is not null)
            LoadFromCustomer(existingCustomer);
    }

    private void InitializeLayout(bool isNew)
    {
        Text = isNew ? "Nowy klient" : "Edycja klienta";
        Width = 460;
        Height = 420;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 7,
            Padding = new Padding(12),
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

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

    private static void AddRow(TableLayoutPanel layout, string label, TextBox box, int row)
    {
        layout.Controls.Add(new Label
        {
            Text = label,
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(3, 8, 3, 3)
        }, 0, row);

        box.Dock = DockStyle.Fill;
        box.Margin = new Padding(3, 4, 3, 4);
        layout.Controls.Add(box, 1, row);
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
            MessageBox.Show(
                "Popraw następujące błędy:\n\n• " + string.Join("\n• ", errors),
                "Walidacja",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        validated!.Id = _id;
        validated.CreatedAt = _createdAt;
        Customer = validated;

        DialogResult = DialogResult.OK;
        Close();
    }
}
