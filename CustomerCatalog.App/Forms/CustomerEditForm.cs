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
    private readonly TextBox _addressBox = new();
    private readonly TextBox _phoneBox = new();
    private readonly TextBox _emailBox = new();

    /// <summary>The customer being edited (populated from the fields when "Save" is clicked).</summary>
    public Customer Customer { get; }

    public CustomerEditForm(Customer customer)
    {
        Customer = customer ?? throw new ArgumentNullException(nameof(customer));

        InitializeLayout();
        LoadFromCustomer();
    }

    private void InitializeLayout()
    {
        Text = Customer.Id == 0 ? "Nowy klient" : "Edycja klienta";
        Width = 460;
        Height = 320;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 6,
            Padding = new Padding(12),
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        AddRow(layout, "Nazwa:", _nameBox, 0);
        AddRow(layout, "NIP:", _nipBox, 1);
        AddRow(layout, "Adres:", _addressBox, 2);
        AddRow(layout, "Telefon:", _phoneBox, 3);
        AddRow(layout, "E-mail:", _emailBox, 4);

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

    private void LoadFromCustomer()
    {
        _nameBox.Text = Customer.Name;
        _nipBox.Text = Customer.Nip;
        _addressBox.Text = Customer.Address;
        _phoneBox.Text = Customer.Phone;
        _emailBox.Text = Customer.Email;
    }

    private void OnSave(object? sender, EventArgs e)
    {
        // Copy the field values back into the model.
        Customer.Name = _nameBox.Text.Trim();
        Customer.Nip = NipValidator.Normalize(_nipBox.Text);
        Customer.Address = _addressBox.Text.Trim();
        Customer.Phone = _phoneBox.Text.Trim();
        Customer.Email = _emailBox.Text.Trim();

        var errors = CustomerValidator.Validate(Customer);
        if (errors.Count > 0)
        {
            MessageBox.Show(
                "Popraw następujące błędy:\n\n• " + string.Join("\n• ", errors),
                "Walidacja",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        DialogResult = DialogResult.OK;
        Close();
    }
}
