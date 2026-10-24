using System;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Ini_Edit___Translate
{
    internal sealed class OpenAiSettingsForm : Form
    {
        private readonly OpenAiSettings settings;
        private readonly ChatGptClient chat;
        private readonly ComboBox provider = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
        private readonly ComboBox accounts = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
        private readonly ComboBox models = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
        private readonly TextBox apiModel = new TextBox { Dock = DockStyle.Fill };
        private readonly TextBox key = new TextBox { Dock = DockStyle.Fill, UseSystemPasswordChar = true };
        private readonly Label status = new Label { AutoSize = true, MaximumSize = new Size(590, 0), Dock = DockStyle.Top };
        private readonly GroupBox apiGroup = new GroupBox { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Dock = DockStyle.Top };
        private readonly GroupBox chatGroup = new GroupBox { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Dock = DockStyle.Top };
        private readonly GroupBox memoryGroup = new GroupBox { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Dock = DockStyle.Top };
        private readonly TextBox memoryEmail = new TextBox { Dock = DockStyle.Fill };
        private readonly Button login, add, signOut, reload, save, cancel;
        private CancellationTokenSource pending;
        private bool loadingAccounts;
        private string operation;
        private bool loginConfirmed;
        private readonly Button copyError = ActionButton(Ui.Text("cgCopyError"));

        private static Label Caption(string text) => new Label { Text = text, AutoSize = true, Margin = new Padding(3, 7, 3, 3) };
        private static Button ActionButton(string text) => new Button { Text = text, Dock = DockStyle.Fill, Height = 34, MinimumSize = new Size(100, 34) };
        private static TableLayoutPanel Rows(int columns = 1) => new TableLayoutPanel
        {
            AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Dock = DockStyle.Top,
            ColumnCount = columns, Padding = new Padding(8), GrowStyle = TableLayoutPanelGrowStyle.AddRows
        };

        internal OpenAiSettingsForm(OpenAiSettings initial = null, ChatGptClient chatClient = null)
        {
            Text = Ui.Text("openAiTitle");
            ClientSize = new Size(650, 575);
            MinimumSize = new Size(620, 540);
            StartPosition = FormStartPosition.CenterParent;
            AutoScaleMode = AutoScaleMode.Dpi;
            settings = initial ?? OpenAiSettings.Load();
            chat = chatClient ?? new ChatGptClient();
            apiModel.Text = settings.Model;
            provider.Items.AddRange(new object[] { Ui.Text("cgProviderApi"), Ui.Text("cgProviderPlan"), Ui.Text("mmProvider") });

            var outer = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Padding = new Padding(12) };
            outer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            outer.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            outer.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
            var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
            var body = Rows();
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            body.Controls.Add(Caption(Ui.Text("cgProvider")));
            body.Controls.Add(provider);

            apiGroup.Text = Ui.Text("cgProviderApi");
            var apiRows = Rows();
            apiRows.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            apiRows.Controls.Add(Caption(Ui.Text("modelLabel"))); apiRows.Controls.Add(apiModel);
            apiRows.Controls.Add(Caption(Ui.Text("apiKeyLabel"))); apiRows.Controls.Add(key);
            apiRows.Controls.Add(new Label { Text = Ui.Text("apiPrivacy"), AutoSize = true, MaximumSize = new Size(570, 0) });
            apiGroup.Controls.Add(apiRows);
            body.Controls.Add(apiGroup);

            chatGroup.Text = Ui.Text("cgProviderPlan");
            var chatRows = Rows();
            chatRows.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            chatRows.Controls.Add(Caption(Ui.Text("cgAccount")));
            chatRows.Controls.Add(accounts);
            var actions = Rows(3);
            for (int i = 0; i < 3; i++) actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / 3));
            login = ActionButton(Ui.Text("cgContinue")); add = ActionButton(Ui.Text("cgAddAccount")); signOut = ActionButton(Ui.Text("cgSignOut"));
            actions.Controls.Add(login, 0, 0); actions.Controls.Add(add, 1, 0); actions.Controls.Add(signOut, 2, 0);
            chatRows.Controls.Add(actions);
            chatRows.Controls.Add(Caption(Ui.Text("cgModels")));
            var modelRow = Rows(2);
            modelRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            modelRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 155));
            reload = ActionButton(Ui.Text("cgLoadModels"));
            modelRow.Controls.Add(models, 0, 0); modelRow.Controls.Add(reload, 1, 0);
            chatRows.Controls.Add(modelRow);
            chatRows.Controls.Add(new Label { Text = Ui.Text("cgPrivacy"), AutoSize = true, MaximumSize = new Size(570, 0) });
            var usage = new LinkLabel { Text = Ui.Text("cgManageUsage"), AutoSize = true, Margin = new Padding(3, 8, 3, 8) };
            usage.LinkClicked += (s, e) => { try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(ChatGptClient.UsageUrl) { UseShellExecute = true }); }
                catch { status.Text = Ui.Text("cgOpenUsage"); } };
            chatRows.Controls.Add(usage);
            chatGroup.Controls.Add(chatRows);
            body.Controls.Add(chatGroup);
            memoryGroup.Text = Ui.Text("mmProvider");
            var memoryRows = Rows(); memoryRows.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            memoryRows.Controls.Add(new Label { Text = Ui.Text("mmPrivacy"), AutoSize = true, MaximumSize = new Size(570, 0) });
            memoryRows.Controls.Add(Caption(Ui.Text("mmEmail")));
            memoryEmail.Text = settings.MyMemoryEmail ?? ""; memoryRows.Controls.Add(memoryEmail);
            memoryRows.Controls.Add(new Label { Text = Ui.Text("mmLimits"), AutoSize = true, MaximumSize = new Size(570, 0) });
            memoryGroup.Controls.Add(memoryRows); body.Controls.Add(memoryGroup);
            body.Controls.Add(status);
            copyError.Visible = false;
            copyError.Click += (s, e) => { try { Clipboard.SetText(status.Text); } catch { MessageBox.Show(Ui.Text("cgCopyFailed")); } };
            body.Controls.Add(copyError);
            scroll.Controls.Add(body); outer.Controls.Add(scroll, 0, 0);

            var buttons = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
            buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50)); buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            save = ActionButton(Ui.Text("saveAction")); cancel = ActionButton(Ui.Text("cancelAction"));
            buttons.Controls.Add(save, 0, 0); buttons.Controls.Add(cancel, 1, 0);
            outer.Controls.Add(buttons, 0, 1); Controls.Add(outer);
            AcceptButton = save; CancelButton = cancel;

            provider.SelectedIndexChanged += async (s, e) =>
            {
                apiGroup.Visible = provider.SelectedIndex == 0; chatGroup.Visible = provider.SelectedIndex == 1;
                memoryGroup.Visible = provider.SelectedIndex == 2; copyError.Visible = false; SelectAccount();
                if (Visible && provider.SelectedIndex == 1 && accounts.Items.Count == 0)
                    await RunAsync(token => LoadAccountsAsync(settings.ChatGptClientId, token));
            };
            provider.SelectedIndex = settings.UseMyMemory ? 2 : settings.UseChatGpt ? 1 : 0;
            accounts.SelectedIndexChanged += (s, e) => { if (!loadingAccounts) SelectAccount(); };
            login.Click += async (s, e) => await RunAsync(async token =>
            {
                var selected = accounts.SelectedItem as ChatGptAccount;
                operation = Ui.Text("cgStageLogin");
                status.Text = Ui.Text("cgWaiting");
                var account = await chat.SignInAsync(selected?.ClientId, token);
                loginConfirmed = true;
                await LoadAccountsAsync(account.ClientId, token);
                if (account.PlanEnabled) await LoadModelsAsync(token);
            });
            add.Click += async (s, e) => await RunAsync(async token =>
            {
                operation = Ui.Text("cgStageLogin");
                status.Text = Ui.Text("cgWaiting");
                var account = await chat.SignInAsync(null, token);
                loginConfirmed = true;
                await LoadAccountsAsync(account.ClientId, token);
                if (account.PlanEnabled) await LoadModelsAsync(token);
            });
            reload.Click += async (s, e) => await RunAsync(LoadModelsAsync);
            signOut.Click += async (s, e) => await RunAsync(async token =>
            {
                var account = accounts.SelectedItem as ChatGptAccount;
                if (account == null) return;
                bool revoked = await chat.SignOutAsync(account.ClientId, token);
                await LoadAccountsAsync(account.ClientId, token);
                status.Text = Ui.Text(revoked ? "cgSignedOut" : "cgRevokeUnconfirmed");
            });
            save.Click += (s, e) =>
            {
                try
                {
                    bool plan = provider.SelectedIndex == 1;
                    bool memory = provider.SelectedIndex == 2;
                    var account = accounts.SelectedItem as ChatGptAccount;
                    var model = models.SelectedItem as ChatGptModel;
                    if (plan && (account == null || !account.SignedIn)) throw new InvalidOperationException(Ui.Text("cgLoginRequired"));
                    if (plan && !account.PlanEnabled) throw new InvalidOperationException(Ui.Text("cgNoConsent"));
                    if (plan && model == null) throw new InvalidOperationException(Ui.Text("cgSelectModel"));
                    if (!memory) settings.Model = apiModel.Text;
                    settings.UseMyMemory = memory;
                    if (memory) settings.MyMemoryEmail = MyMemoryTranslation.ValidateEmail(memoryEmail.Text);
                    else settings.UseChatGpt = plan;
                    if (plan) { settings.ChatGptClientId = account.ClientId; settings.ChatGptModel = model.Slug; }
                    settings.Save(provider.SelectedIndex == 0 ? key.Text : null);
                    DialogResult = DialogResult.OK; Close();
                }
                catch (InvalidOperationException error) { status.Text = error.Message; }
                catch { status.Text = Ui.Text("apiSaveError"); }
            };
            cancel.Click += (s, e) => { if (pending != null) pending.Cancel(); else { DialogResult = DialogResult.Cancel; Close(); } };
            Shown += async (s, e) => { if (provider.SelectedIndex != 2) await RunAsync(token => LoadAccountsAsync(settings.ChatGptClientId, token)); };
            FormClosing += (s, e) => { if (pending != null) { e.Cancel = true; pending.Cancel(); } };
            FormClosed += (s, e) => chat.Dispose();
        }

        private async Task LoadAccountsAsync(string selectedId, CancellationToken token)
        {
            var data = await chat.Vault.ReadAsync(token);
            loadingAccounts = true;
            try
            {
                accounts.Items.Clear();
                foreach (var account in data.Accounts) accounts.Items.Add(account);
                accounts.SelectedItem = data.Accounts.FirstOrDefault(a => a.ClientId == selectedId);
                if (accounts.SelectedIndex < 0 && accounts.Items.Count > 0) accounts.SelectedIndex = 0;
            }
            finally { loadingAccounts = false; }
            SelectAccount();
        }

        private void SelectAccount()
        {
            models.Items.Clear();
            var account = accounts.SelectedItem as ChatGptAccount;
            if (account != null && account.ClientId == settings.ChatGptClientId && !string.IsNullOrEmpty(settings.ChatGptModel))
            {
                models.Items.Add(new ChatGptModel { Slug = settings.ChatGptModel, DisplayName = settings.ChatGptModel + " · " + Ui.Text("cgLastModel") });
                models.SelectedIndex = 0;
            }
            status.Text = provider.SelectedIndex == 2 ? Ui.Text("mmReady") : provider.SelectedIndex == 0 ? "" : account == null || !account.SignedIn ? Ui.Text("cgLoginRequired") :
                account.PlanEnabled ? Ui.Text("cgReady") : Ui.Text("cgNoConsent");
        }

        private async Task LoadModelsAsync(CancellationToken token)
        {
            operation = Ui.Text("cgStageModels");
            var account = accounts.SelectedItem as ChatGptAccount;
            if (account == null) throw new InvalidOperationException(Ui.Text("cgLoginRequired"));
            var choices = await chat.ModelsAsync(account.ClientId, token);
            models.Items.Clear(); foreach (var model in choices) models.Items.Add(model);
            models.SelectedItem = choices.FirstOrDefault(m => m.Slug == settings.ChatGptModel) ?? choices.First();
            status.Text = Ui.Text("cgReady");
        }

        private async Task RunAsync(Func<CancellationToken, Task> work)
        {
            if (pending != null) return;
            operation = Ui.Text("cgStageSession"); loginConfirmed = false; copyError.Visible = false;
            pending = new CancellationTokenSource();
            provider.Enabled = accounts.Enabled = models.Enabled = login.Enabled = add.Enabled = signOut.Enabled = reload.Enabled = save.Enabled = false;
            try { await work(pending.Token); }
            catch (OperationCanceledException) { status.Text = Ui.Text("cgCancelled"); }
            catch (Exception error)
            {
                status.Text = (loginConfirmed ? Ui.Text("cgLoginConfirmed") + Environment.NewLine : "") +
                    operation + ": " + OpenAiErrors.SafeMessage(error);
                copyError.Visible = true;
            }
            finally
            {
                pending.Dispose(); pending = null;
                provider.Enabled = accounts.Enabled = models.Enabled = login.Enabled = add.Enabled = signOut.Enabled = reload.Enabled = save.Enabled = true;
            }
        }
    }
}
