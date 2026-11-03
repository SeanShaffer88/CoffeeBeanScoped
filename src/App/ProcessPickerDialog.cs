using CoffeeBean.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace CoffeeBean;

internal sealed class ProcessPickerDialog : Form
{
    private sealed class ProcessItem
    {
        public string Name { get; set; }
        public string WindowTitle { get; set; }

        public override string ToString() =>
            string.IsNullOrWhiteSpace(WindowTitle) ? Name : $"{Name} — {WindowTitle}";
    }

    private readonly TextBox searchBox = new TextBox();
    private readonly ListBox processList = new ListBox();
    private readonly List<ProcessItem> allProcesses = new List<ProcessItem>();

    public string SelectedProcessName { get; private set; }

    public ProcessPickerDialog(string currentProcessName)
    {
        Text = "Choose an app";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterScreen;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        ClientSize = new Size(440, 470);
        Font = new Font("Segoe UI", 9F);

        var prompt = new Label
        {
            AutoSize = true,
            Location = new Point(12, 10),
            Text = "Keep the PC awake only while this app is running or in front:"
        };

        searchBox.Location = new Point(12, 34);
        searchBox.Size = new Size(320, 23);
        searchBox.Text = currentProcessName ?? "";
        searchBox.TextChanged += (sender, e) => ApplyFilter();

        var refreshButton = new Button
        {
            Location = new Point(338, 33),
            Size = new Size(90, 25),
            Text = "Refresh"
        };
        refreshButton.Click += (sender, e) => LoadProcesses();

        processList.Location = new Point(12, 66);
        processList.Size = new Size(416, 330);
        processList.DoubleClick += (sender, e) => AcceptSelection();

        var browseButton = new Button
        {
            Location = new Point(12, 408),
            Size = new Size(110, 27),
            Text = "Browse for .exe…"
        };
        browseButton.Click += BrowseButton_Click;

        var okButton = new Button
        {
            DialogResult = DialogResult.OK,
            Location = new Point(262, 408),
            Size = new Size(80, 27),
            Text = "OK"
        };
        okButton.Click += (sender, e) => AcceptSelection();

        var cancelButton = new Button
        {
            DialogResult = DialogResult.Cancel,
            Location = new Point(348, 408),
            Size = new Size(80, 27),
            Text = "Cancel"
        };

        AcceptButton = okButton;
        CancelButton = cancelButton;

        Controls.Add(prompt);
        Controls.Add(searchBox);
        Controls.Add(refreshButton);
        Controls.Add(processList);
        Controls.Add(browseButton);
        Controls.Add(okButton);
        Controls.Add(cancelButton);

        LoadProcesses();
    }

    private void LoadProcesses()
    {
        allProcesses.Clear();
        var ownId = Process.GetCurrentProcess().Id;
        var seen = new Dictionary<string, ProcessItem>(StringComparer.OrdinalIgnoreCase);

        foreach (var process in Process.GetProcesses())
        {
            using (process)
            {
                try
                {
                    if (process.Id == ownId || process.Id == 0)
                    {
                        continue;
                    }

                    var name = process.ProcessName;
                    if (string.IsNullOrWhiteSpace(name) ||
                        string.Equals(name, "Idle", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var title = "";
                    try
                    {
                        title = process.MainWindowTitle ?? "";
                    }
                    catch (Exception)
                    {
                        // Some protected processes refuse this; the name alone is enough.
                    }

                    if (seen.TryGetValue(name, out var existing))
                    {
                        if (string.IsNullOrWhiteSpace(existing.WindowTitle) && !string.IsNullOrWhiteSpace(title))
                        {
                            existing.WindowTitle = title;
                        }
                    }
                    else
                    {
                        seen[name] = new ProcessItem { Name = name, WindowTitle = title };
                    }
                }
                catch (Exception)
                {
                    // A process can exit or refuse access while the list is built; skip it.
                }
            }
        }

        allProcesses.AddRange(seen.Values.OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase));
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        var filter = searchBox.Text.Trim();
        processList.BeginUpdate();
        processList.Items.Clear();
        foreach (var item in allProcesses)
        {
            if (filter.Length == 0 ||
                item.Name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0 ||
                item.WindowTitle.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                processList.Items.Add(item);
            }
        }
        processList.EndUpdate();
    }

    private void BrowseButton_Click(object sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog
        {
            Filter = "Programs (*.exe)|*.exe|All files (*.*)|*.*",
            Title = "Choose an app"
        };
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            searchBox.Text = TargetProcess.NormalizeName(dialog.FileName);
            SelectedProcessName = searchBox.Text;
        }
    }

    private void AcceptSelection()
    {
        if (processList.SelectedItem is ProcessItem item)
        {
            SelectedProcessName = item.Name;
        }
        else
        {
            SelectedProcessName = TargetProcess.NormalizeName(searchBox.Text);
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (DialogResult == DialogResult.OK && string.IsNullOrWhiteSpace(SelectedProcessName))
        {
            AcceptSelection();
            if (string.IsNullOrWhiteSpace(SelectedProcessName))
            {
                e.Cancel = true;
                MessageBox.Show(this, "Pick an app from the list, type its process name, or browse for its .exe file.",
                    "CoffeeBean", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        base.OnFormClosing(e);
    }
}
