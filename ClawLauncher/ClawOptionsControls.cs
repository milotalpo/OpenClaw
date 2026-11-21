using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Xml;

namespace ClawLauncher
{
    // "Controls" tab of the options dialog: keyboard + game controller (Xbox) bindings.
    // Bindings are stored in config.xml under Configuration/ControlOptions:
    //
    //   <KeyBindings><MoveLeft>Left</MoveLeft>...</KeyBindings>       (SDL key names)
    //   <Controller>
    //     <Enable>true</Enable>
    //     <StickDeadzone>16000</StickDeadzone>
    //     <TriggerThreshold>8000</TriggerThreshold>
    //     <Bindings><MoveLeft>dpleft</MoveLeft>...</Bindings>         (SDL game controller names)
    //   </Controller>
    public partial class ClawOptions
    {
        private class InputActionInfo
        {
            public string XmlName;
            public string DisplayName;
            public bool KeyboardRemappable;
            public string DefaultKey;
            public string DefaultKeyAlternate;
            public string DefaultController;

            public InputActionInfo(string xmlName, string displayName, bool keyboardRemappable,
                string defaultKey, string defaultKeyAlternate, string defaultController)
            {
                XmlName = xmlName;
                DisplayName = displayName;
                KeyboardRemappable = keyboardRemappable;
                DefaultKey = defaultKey;
                DefaultKeyAlternate = defaultKeyAlternate;
                DefaultController = defaultController;
            }
        }

        // Must match InputAction / s_InputActionNames in OpenClaw/Engine/GameApp/BaseGameApp.*
        private static readonly InputActionInfo[] s_InputActions = new InputActionInfo[]
        {
            new InputActionInfo("MoveLeft",   "Move left",        true,  "Left",       "A",          "dpleft"),
            new InputActionInfo("MoveRight",  "Move right",       true,  "Right",      "D",          "dpright"),
            new InputActionInfo("Up",         "Up / climb",       true,  "Up",         "W",          "dpup"),
            new InputActionInfo("Down",       "Down / duck",      true,  "Down",       "S",          "dpdown"),
            new InputActionInfo("Jump",       "Jump",             true,  "Space",      "Space",      "a"),
            new InputActionInfo("Attack",     "Attack (sword)",   true,  "Left Ctrl",  "Left Ctrl",  "x"),
            new InputActionInfo("Fire",       "Fire (pistol...)", true,  "Left Alt",   "Left Alt",   "b"),
            new InputActionInfo("ChangeAmmo", "Change weapon",    true,  "Left Shift", "E",          "y"),
            new InputActionInfo("Pause",      "Pause / menu",     false, "Escape",     "Escape",     "start"),
        };

        // SDL game controller input names (OpenClaw s_GamepadInputNames) -> display names
        private static readonly string[][] s_ControllerInputs = new string[][]
        {
            new string[] { "",              "(none)" },
            new string[] { "a",             "A" },
            new string[] { "b",             "B" },
            new string[] { "x",             "X" },
            new string[] { "y",             "Y" },
            new string[] { "leftshoulder",  "LB" },
            new string[] { "rightshoulder", "RB" },
            new string[] { "lefttrigger",   "LT" },
            new string[] { "righttrigger",  "RT" },
            new string[] { "dpup",          "D-Pad Up" },
            new string[] { "dpdown",        "D-Pad Down" },
            new string[] { "dpleft",        "D-Pad Left" },
            new string[] { "dpright",       "D-Pad Right" },
            new string[] { "start",         "Start (Menu)" },
            new string[] { "back",          "Back (View)" },
            new string[] { "leftstick",     "Left stick click" },
            new string[] { "rightstick",    "Right stick click" },
            new string[] { "guide",         "Xbox button" },
        };

        private TabPage Controls_TabPage;
        private Button[] Controls_KeyButtons;
        private ComboBox[] Controls_ControllerComboBoxes;
        private CheckBox Controls_AlternateControlsCheckBox;
        private CheckBox Controls_ControllerEnableCheckBox;
        private NumericUpDown Controls_DeadzoneUpDown;

        // Current keyboard bindings (SDL key names)
        private string[] m_KeyBindings;
        // Index of the action that is waiting for a key press, -1 when not capturing
        private int m_CapturingKeyActionIdx = -1;

        private void InitializeControlsTab()
        {
            Controls_TabPage = new TabPage("Controls");
            Controls_TabPage.BackColor = SystemColors.Control;
            Controls_TabPage.Padding = new Padding(3);

            m_KeyBindings = new string[s_InputActions.Length];
            Controls_KeyButtons = new Button[s_InputActions.Length];
            Controls_ControllerComboBoxes = new ComboBox[s_InputActions.Length];

            GroupBox bindingsGroup = new GroupBox();
            bindingsGroup.Text = "Bindings (click a key button, then press the new key)";
            bindingsGroup.Location = new Point(6, 6);
            bindingsGroup.Size = new Size(346, 300);
            Controls_TabPage.Controls.Add(bindingsGroup);

            Label headerAction = new Label();
            headerAction.Text = "Action";
            headerAction.Font = new Font(Font, FontStyle.Bold);
            headerAction.Location = new Point(10, 20);
            headerAction.AutoSize = true;
            bindingsGroup.Controls.Add(headerAction);

            Label headerKeyboard = new Label();
            headerKeyboard.Text = "Keyboard";
            headerKeyboard.Font = new Font(Font, FontStyle.Bold);
            headerKeyboard.Location = new Point(118, 20);
            headerKeyboard.AutoSize = true;
            bindingsGroup.Controls.Add(headerKeyboard);

            Label headerController = new Label();
            headerController.Text = "Xbox controller";
            headerController.Font = new Font(Font, FontStyle.Bold);
            headerController.Location = new Point(228, 20);
            headerController.AutoSize = true;
            bindingsGroup.Controls.Add(headerController);

            for (int i = 0; i < s_InputActions.Length; i++)
            {
                int y = 42 + i * 28;

                Label actionLabel = new Label();
                actionLabel.Text = s_InputActions[i].DisplayName;
                actionLabel.Location = new Point(10, y + 4);
                actionLabel.AutoSize = true;
                bindingsGroup.Controls.Add(actionLabel);

                Button keyButton = new Button();
                keyButton.Location = new Point(116, y);
                keyButton.Size = new Size(104, 24);
                keyButton.Tag = i;
                keyButton.UseVisualStyleBackColor = true;
                keyButton.Enabled = s_InputActions[i].KeyboardRemappable;
                keyButton.Click += new EventHandler(Controls_KeyButton_Click);
                bindingsGroup.Controls.Add(keyButton);
                Controls_KeyButtons[i] = keyButton;

                ComboBox controllerCombo = new ComboBox();
                controllerCombo.DropDownStyle = ComboBoxStyle.DropDownList;
                controllerCombo.Location = new Point(226, y + 1);
                controllerCombo.Size = new Size(110, 23);
                foreach (string[] input in s_ControllerInputs)
                {
                    controllerCombo.Items.Add(input[1]);
                }
                bindingsGroup.Controls.Add(controllerCombo);
                Controls_ControllerComboBoxes[i] = controllerCombo;
            }

            GroupBox optionsGroup = new GroupBox();
            optionsGroup.Text = "Options";
            optionsGroup.Location = new Point(6, 310);
            optionsGroup.Size = new Size(346, 70);
            Controls_TabPage.Controls.Add(optionsGroup);

            Controls_AlternateControlsCheckBox = new CheckBox();
            Controls_AlternateControlsCheckBox.Text = "Alternate controls (mouse)";
            Controls_AlternateControlsCheckBox.Location = new Point(10, 18);
            Controls_AlternateControlsCheckBox.AutoSize = true;
            optionsGroup.Controls.Add(Controls_AlternateControlsCheckBox);

            Controls_ControllerEnableCheckBox = new CheckBox();
            Controls_ControllerEnableCheckBox.Text = "Enable controller";
            Controls_ControllerEnableCheckBox.Location = new Point(200, 18);
            Controls_ControllerEnableCheckBox.AutoSize = true;
            optionsGroup.Controls.Add(Controls_ControllerEnableCheckBox);

            Label deadzoneLabel = new Label();
            deadzoneLabel.Text = "Left stick deadzone (%):";
            deadzoneLabel.Location = new Point(10, 44);
            deadzoneLabel.AutoSize = true;
            optionsGroup.Controls.Add(deadzoneLabel);

            Controls_DeadzoneUpDown = new NumericUpDown();
            Controls_DeadzoneUpDown.Minimum = 5;
            Controls_DeadzoneUpDown.Maximum = 95;
            Controls_DeadzoneUpDown.Location = new Point(200, 41);
            Controls_DeadzoneUpDown.Size = new Size(60, 21);
            optionsGroup.Controls.Add(Controls_DeadzoneUpDown);

            Button defaultsButton = new Button();
            defaultsButton.Text = "Reset defaults";
            defaultsButton.Location = new Point(40, 386);
            defaultsButton.Size = new Size(116, 31);
            defaultsButton.UseVisualStyleBackColor = true;
            defaultsButton.Click += new EventHandler(Controls_DefaultsButton_Click);
            Controls_TabPage.Controls.Add(defaultsButton);

            Button applyButton = new Button();
            applyButton.Text = "Apply Changes";
            applyButton.Location = new Point(200, 386);
            applyButton.Size = new Size(116, 31);
            applyButton.UseVisualStyleBackColor = true;
            applyButton.Click += new EventHandler(Controls_ApplyChangesButton_Click);
            Controls_TabPage.Controls.Add(applyButton);

            // Put "Controls" right after "Video" / "Audio" / "Assets".
            // TabPages.Insert is silently ignored until the TabControl handle exists,
            // so force its creation first.
            IntPtr tabControlHandle = tabControl1.Handle;
            tabControl1.TabPages.Insert(3, Controls_TabPage);
            if (!tabControl1.TabPages.Contains(Controls_TabPage))
            {
                tabControl1.TabPages.Add(Controls_TabPage);
            }
        }

        //---------------------------------------------------------------------
        // Config load / save
        //---------------------------------------------------------------------

        private static XmlNode GetOrCreateNode(XmlDocument doc, XmlNode parent, string name)
        {
            XmlNode node = parent.SelectSingleNode(name);
            if (node == null)
            {
                node = doc.CreateElement(name);
                parent.AppendChild(node);
            }
            return node;
        }

        private static string GetNodeText(XmlNode parent, string name)
        {
            if (parent == null)
            {
                return null;
            }
            XmlNode node = parent.SelectSingleNode(name);
            if (node == null)
            {
                return null;
            }
            return node.InnerText.Trim();
        }

        private void LoadControlsFromConfig(string configPath)
        {
            bool alternate = false;
            bool controllerEnabled = true;
            int deadzone = 16000;

            XmlNode controlRoot = null;
            try
            {
                XmlDocument configDoc = new XmlDocument();
                configDoc.Load(configPath);
                controlRoot = configDoc.DocumentElement.SelectSingleNode("ControlOptions");
            }
            catch (Exception)
            {
                controlRoot = null;
            }

            string text = GetNodeText(controlRoot, "UseAlternateControls");
            if (text != null)
            {
                alternate = text.ToLower() == "true";
            }

            XmlNode keyRoot = controlRoot != null ? controlRoot.SelectSingleNode("KeyBindings") : null;
            XmlNode controllerRoot = controlRoot != null ? controlRoot.SelectSingleNode("Controller") : null;
            XmlNode controllerBindingsRoot = controllerRoot != null ? controllerRoot.SelectSingleNode("Bindings") : null;

            text = GetNodeText(controllerRoot, "Enable");
            if (text != null)
            {
                controllerEnabled = text.ToLower() != "false";
            }
            text = GetNodeText(controllerRoot, "StickDeadzone");
            if (text != null)
            {
                int parsed;
                if (int.TryParse(text, out parsed))
                {
                    deadzone = parsed;
                }
            }

            for (int i = 0; i < s_InputActions.Length; i++)
            {
                InputActionInfo action = s_InputActions[i];

                string key = action.KeyboardRemappable ? GetNodeText(keyRoot, action.XmlName) : null;
                if (string.IsNullOrEmpty(key))
                {
                    key = alternate ? action.DefaultKeyAlternate : action.DefaultKey;
                }
                SetKeyBinding(i, key);

                string controllerInput = GetNodeText(controllerBindingsRoot, action.XmlName);
                if (controllerInput == null)
                {
                    controllerInput = action.DefaultController;
                }
                SetControllerBinding(i, controllerInput);
            }

            Controls_AlternateControlsCheckBox.Checked = alternate;
            Controls_ControllerEnableCheckBox.Checked = controllerEnabled;
            int deadzonePercent = (int)Math.Round(deadzone * 100.0 / 32767.0);
            deadzonePercent = Math.Max((int)Controls_DeadzoneUpDown.Minimum, Math.Min((int)Controls_DeadzoneUpDown.Maximum, deadzonePercent));
            Controls_DeadzoneUpDown.Value = deadzonePercent;
        }

        private void SaveControlsToConfig()
        {
            XmlDocument configDoc = new XmlDocument();
            configDoc.Load(ClawLauncherForm.ClawConfigPath);
            XmlElement root = configDoc.DocumentElement;

            try
            {
                XmlNode controlRoot = GetOrCreateNode(configDoc, root, "ControlOptions");
                GetOrCreateNode(configDoc, controlRoot, "UseAlternateControls").InnerText =
                    Controls_AlternateControlsCheckBox.Checked.ToString().ToLower();

                XmlNode keyRoot = GetOrCreateNode(configDoc, controlRoot, "KeyBindings");
                XmlNode controllerRoot = GetOrCreateNode(configDoc, controlRoot, "Controller");
                GetOrCreateNode(configDoc, controllerRoot, "Enable").InnerText =
                    Controls_ControllerEnableCheckBox.Checked.ToString().ToLower();
                int deadzone = (int)Math.Round((double)Controls_DeadzoneUpDown.Value * 32767.0 / 100.0);
                GetOrCreateNode(configDoc, controllerRoot, "StickDeadzone").InnerText = deadzone.ToString();
                if (controllerRoot.SelectSingleNode("TriggerThreshold") == null)
                {
                    GetOrCreateNode(configDoc, controllerRoot, "TriggerThreshold").InnerText = "8000";
                }
                XmlNode controllerBindingsRoot = GetOrCreateNode(configDoc, controllerRoot, "Bindings");

                for (int i = 0; i < s_InputActions.Length; i++)
                {
                    InputActionInfo action = s_InputActions[i];
                    if (action.KeyboardRemappable)
                    {
                        GetOrCreateNode(configDoc, keyRoot, action.XmlName).InnerText = m_KeyBindings[i];
                    }

                    int comboIdx = Controls_ControllerComboBoxes[i].SelectedIndex;
                    string inputName = comboIdx >= 0 ? s_ControllerInputs[comboIdx][0] : "";
                    GetOrCreateNode(configDoc, controllerBindingsRoot, action.XmlName).InnerText =
                        inputName.Length > 0 ? inputName : "none";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Something went wrong: \n\n" + ex.Message + "\n\nNo changes were made in Controls section !", "Error applying changes", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            configDoc.Save(ClawLauncherForm.ClawConfigPath);
        }

        private string FindBindingConflicts()
        {
            List<string> conflicts = new List<string>();
            for (int i = 0; i < s_InputActions.Length; i++)
            {
                for (int j = i + 1; j < s_InputActions.Length; j++)
                {
                    if (string.Equals(m_KeyBindings[i], m_KeyBindings[j], StringComparison.OrdinalIgnoreCase))
                    {
                        conflicts.Add("Keyboard: \"" + m_KeyBindings[i] + "\" used by " +
                            s_InputActions[i].DisplayName + " and " + s_InputActions[j].DisplayName);
                    }

                    int ci = Controls_ControllerComboBoxes[i].SelectedIndex;
                    int cj = Controls_ControllerComboBoxes[j].SelectedIndex;
                    if (ci > 0 && ci == cj)
                    {
                        conflicts.Add("Controller: \"" + s_ControllerInputs[ci][1] + "\" used by " +
                            s_InputActions[i].DisplayName + " and " + s_InputActions[j].DisplayName);
                    }
                }
            }
            return conflicts.Count > 0 ? string.Join("\n", conflicts.ToArray()) : null;
        }

        //---------------------------------------------------------------------
        // UI helpers
        //---------------------------------------------------------------------

        private void SetKeyBinding(int actionIdx, string sdlKeyName)
        {
            m_KeyBindings[actionIdx] = sdlKeyName;
            Controls_KeyButtons[actionIdx].Text = sdlKeyName;
            Controls_KeyButtons[actionIdx].Font = new Font(Font, FontStyle.Regular);
        }

        private void SetControllerBinding(int actionIdx, string inputName)
        {
            int selectedIdx = 0;
            string lower = (inputName ?? "").ToLower();
            for (int i = 0; i < s_ControllerInputs.Length; i++)
            {
                if (s_ControllerInputs[i][0] == lower)
                {
                    selectedIdx = i;
                    break;
                }
            }
            Controls_ControllerComboBoxes[actionIdx].SelectedIndex = selectedIdx;
        }

        private void StopKeyCapture()
        {
            if (m_CapturingKeyActionIdx >= 0)
            {
                int idx = m_CapturingKeyActionIdx;
                m_CapturingKeyActionIdx = -1;
                SetKeyBinding(idx, m_KeyBindings[idx]);
            }
        }

        private void Controls_KeyButton_Click(object sender, EventArgs e)
        {
            StopKeyCapture();

            Button button = (Button)sender;
            m_CapturingKeyActionIdx = (int)button.Tag;
            button.Text = "Press a key...";
            button.Font = new Font(Font, FontStyle.Bold);
        }

        private void Controls_DefaultsButton_Click(object sender, EventArgs e)
        {
            StopKeyCapture();

            bool alternate = Controls_AlternateControlsCheckBox.Checked;
            for (int i = 0; i < s_InputActions.Length; i++)
            {
                SetKeyBinding(i, alternate ? s_InputActions[i].DefaultKeyAlternate : s_InputActions[i].DefaultKey);
                SetControllerBinding(i, s_InputActions[i].DefaultController);
            }
            Controls_ControllerEnableCheckBox.Checked = true;
            Controls_DeadzoneUpDown.Value = 49;
        }

        private void Controls_ApplyChangesButton_Click(object sender, EventArgs e)
        {
            StopKeyCapture();

            string conflicts = FindBindingConflicts();
            if (conflicts != null)
            {
                MessageBox.Show("Some actions share the same binding:\n\n" + conflicts,
                    "Controls", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            SaveControlsToConfig();
        }

        // Catches every key (including arrows, Alt, Tab...) while a key button is waiting for input
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            const int WM_KEYDOWN = 0x0100;
            const int WM_SYSKEYDOWN = 0x0104;

            if (m_CapturingKeyActionIdx >= 0 && (msg.Msg == WM_KEYDOWN || msg.Msg == WM_SYSKEYDOWN))
            {
                Keys keyCode = keyData & Keys.KeyCode;
                long lParam = msg.LParam.ToInt64();
                int scanCode = (int)((lParam >> 16) & 0xFF);
                bool extended = ((lParam >> 24) & 1) != 0;

                if (keyCode == Keys.Escape)
                {
                    // Cancel
                    StopKeyCapture();
                    return true;
                }

                string sdlName = KeysToSdlName(keyCode, scanCode, extended);
                if (sdlName != null)
                {
                    int idx = m_CapturingKeyActionIdx;
                    m_CapturingKeyActionIdx = -1;
                    SetKeyBinding(idx, sdlName);
                }
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        // Converts a WinForms key to the name SDL_GetKeyFromName() understands
        private static string KeysToSdlName(Keys key, int scanCode, bool extended)
        {
            if (key >= Keys.A && key <= Keys.Z)
            {
                return ((char)('A' + (key - Keys.A))).ToString();
            }
            if (key >= Keys.D0 && key <= Keys.D9)
            {
                return ((char)('0' + (key - Keys.D0))).ToString();
            }
            if (key >= Keys.NumPad0 && key <= Keys.NumPad9)
            {
                return "Keypad " + (key - Keys.NumPad0).ToString();
            }
            if (key >= Keys.F1 && key <= Keys.F12)
            {
                return "F" + (key - Keys.F1 + 1).ToString();
            }

            switch (key)
            {
                case Keys.Left: return "Left";
                case Keys.Right: return "Right";
                case Keys.Up: return "Up";
                case Keys.Down: return "Down";
                case Keys.Space: return "Space";
                case Keys.Enter: return extended ? "Keypad Enter" : "Return";
                case Keys.Back: return "Backspace";
                case Keys.Tab: return "Tab";
                case Keys.Insert: return "Insert";
                case Keys.Delete: return "Delete";
                case Keys.Home: return "Home";
                case Keys.End: return "End";
                case Keys.PageUp: return "PageUp";
                case Keys.PageDown: return "PageDown";
                case Keys.CapsLock: return "CapsLock";

                case Keys.ShiftKey:
                case Keys.LShiftKey:
                case Keys.RShiftKey:
                    return (key == Keys.RShiftKey || scanCode == 0x36) ? "Right Shift" : "Left Shift";
                case Keys.ControlKey:
                case Keys.LControlKey:
                case Keys.RControlKey:
                    return (key == Keys.RControlKey || extended) ? "Right Ctrl" : "Left Ctrl";
                case Keys.Menu:
                case Keys.LMenu:
                case Keys.RMenu:
                    return (key == Keys.RMenu || extended) ? "Right Alt" : "Left Alt";

                case Keys.Add: return "Keypad +";
                case Keys.Subtract: return "Keypad -";
                case Keys.Multiply: return "Keypad *";
                case Keys.Divide: return "Keypad /";
                case Keys.Decimal: return "Keypad .";

                case Keys.Oemcomma: return ",";
                case Keys.OemPeriod: return ".";
                case Keys.OemMinus: return "-";
                case Keys.Oemplus: return "=";
                case Keys.OemQuestion: return "/";
                case Keys.OemSemicolon: return ";";
                case Keys.OemQuotes: return "'";
                case Keys.OemOpenBrackets: return "[";
                case Keys.OemCloseBrackets: return "]";
                case Keys.OemPipe: return "\\";
                case Keys.Oemtilde: return "`";
            }

            // Unsupported key
            return null;
        }
    }
}
