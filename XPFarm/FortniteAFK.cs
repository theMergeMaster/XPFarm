using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace FortniteAFK.src
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }

    public class MainForm : Form
    {
        // Import Windows API functions to simulate key presses and mouse clicks
        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        [DllImport("user32.dll")]
        private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        // Constants for keybd_event
        private const int KEYEVENTF_KEYDOWN = 0x0000;
        private const int KEYEVENTF_KEYUP = 0x0002;

        // Constants for mouse_event
        private const int MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const int MOUSEEVENTF_LEFTUP = 0x0004;

        // Virtual key codes
        private const byte VK_W = 0x57;
        private const byte VK_S = 0x53;
        private const byte VK_SPACE = 0x20;
        private const byte VK_ESCAPE = 0x1B; // Default key to stop

        private bool isRunning = false;
        private Random random = new Random();
        private Thread executionThread;
        private Thread keyMonitorThread;
        private DateTime endTime = DateTime.MaxValue;
        private byte stopKey = VK_ESCAPE;
        private bool useStopKey = false;

        // List of main keys used in the script that are not suppossed to be used as stop keys
        private static readonly List<byte> invalidKeys = new List<byte> 
        {
            0x57, // W
            0x53, // S
            0x41, // A 
            0x44, // D
            0x20, // Space
            0x0002, // Left mouse button
            0x0004  // Right mouse button
        };
        private static readonly Dictionary<int, string> keyNames = new Dictionary<int, string>
        {
            { 0x08, "Backspace" },
            { 0x09, "Tab" },
            { 0x0D, "Enter" },
            { 0x10, "Shift" },
            { 0x11, "Ctrl" },
            { 0x12, "Alt" },
            { 0x13, "Pause" },
            { 0x14, "CapsLock" },
            { 0x1B, "Escape" },
            { 0x20, "Space" },
            { 0x21, "PageUp" },
            { 0x22, "PageDown" },
            { 0x23, "End" },
            { 0x24, "Home" },
            { 0x25, "ArrowLeft" },
            { 0x26, "ArrowUp" },
            { 0x27, "ArrowRight" },
            { 0x28, "ArrowDown" },
            { 0x2D, "Insert" },
            { 0x2E, "Delete" },
            { 0x5B, "Left Windows" },
            { 0x5C, "Right Windows" },
            { 0x5D, "Context Menu" },
            { 0x70, "F1" },
            { 0x71, "F2" },
            { 0x72, "F3" },
            { 0x73, "F4" },
            { 0x74, "F5" },
            { 0x75, "F6" },
            { 0x76, "F7" },
            { 0x77, "F8" },
            { 0x78, "F9" },
            { 0x79, "F10" },
            { 0x7A, "F11" },
            { 0x7B, "F12" }
        };

        // UI Controls
        private Label lblStatus;
        private TextBox txtDuration;
        private CheckBox chkUseDuration;
        private CheckBox chkUseKey;
        private Button btnSelectKey;
        private Button btnStart;
        private Button btnStop;
        private RichTextBox txtLog;
        private Label lblSelectedKey;
        private Label lblDuration;

        public MainForm()
        {
            InitializeComponent();
            FormClosing += MainForm_FormClosing;
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            StopProcess();
        }

        private void InitializeComponent()
        {
            Text = "Fortnite AFK XP Farm";
            Size = new Size(500, 500);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;

            // Establecer el icono de la aplicación en la taskbar
            var assembly = Assembly.GetExecutingAssembly();
            using (var stream = assembly.GetManifestResourceStream("FortniteAFK.icons.appicon.ico"))
            {
                Icon = new Icon(stream);
            }

            // Create controls
            Label lblTitle = new Label
            {
                Text = "Fortnite AFK XP Farm",
                Font = new Font("Arial", 16, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                Height = 40
            };

            Panel panelConfig = new Panel
            {
                Dock = DockStyle.Top,
                Height = 180,
                Padding = new Padding(10)
            };

            // Duration
            chkUseDuration = new CheckBox
            {
                Text = "Set maximum duration",
                Location = new Point(20, 20),
                AutoSize = true
            };
            chkUseDuration.CheckedChanged += (s, e) => txtDuration.Enabled = chkUseDuration.Checked;

            lblDuration = new Label
            {
                Text = "Duration (minutes):",
                Location = new Point(40, 50),
                AutoSize = true
            };

            txtDuration = new TextBox
            {
                Location = new Point(160, 47),
                Size = new Size(60, 20),
                Enabled = false,
                Text = "60"
            };

            // Stop key
            chkUseKey = new CheckBox
            {
                Text = "Use key to stop",
                Location = new Point(20, 80),
                AutoSize = true
            };
            chkUseKey.CheckedChanged += (s, e) => {
                btnSelectKey.Enabled = chkUseKey.Checked;
            };

            btnSelectKey = new Button
            {
                Text = "Select Key",
                Location = new Point(40, 110),
                Size = new Size(120, 30),
                Enabled = false
            };
            btnSelectKey.Click += BtnSelectKey_Click;

            lblSelectedKey = new Label
            {
                Text = "Default: Escape",
                Location = new Point(170, 118),
                AutoSize = true,
                ForeColor = Color.Gray
            };

            // Control buttons
            btnStart = new Button
            {
                Text = "Start",
                Location = new Point(90, 150),
                Size = new Size(100, 30)
            };
            btnStart.Click += BtnStart_Click;

            btnStop = new Button
            {
                Text = "Stop",
                Location = new Point(200, 150),
                Size = new Size(100, 30),
                Enabled = false
            };
            btnStop.Click += BtnStop_Click;

            // Status
            lblStatus = new Label
            {
                Text = "Status: Stopped",
                Location = new Point(310, 158),
                AutoSize = true,
                ForeColor = Color.Red
            };

            // Log
            Label lblLog = new Label
            {
                Text = "Activity Log:",
                Location = new Point(20, 198),
                AutoSize = true
            };

            // Console where logs are displayed
            txtLog = new RichTextBox
            {
                Location = new Point(20, 230),
                Size = new Size(440, 220),
                ReadOnly = true,
                BackColor = Color.Black,
                ForeColor = Color.LightGreen,
                Font = new Font("Consolas", 9)
            };

            // Add controls to panel
            panelConfig.Controls.Add(chkUseDuration);
            panelConfig.Controls.Add(lblDuration);
            panelConfig.Controls.Add(txtDuration);
            panelConfig.Controls.Add(chkUseKey);
            panelConfig.Controls.Add(btnSelectKey);
            panelConfig.Controls.Add(lblSelectedKey);
            panelConfig.Controls.Add(btnStart);
            panelConfig.Controls.Add(btnStop);
            panelConfig.Controls.Add(lblStatus);
            
            // Update the CheckedChanged event to also change the label color
            chkUseKey.CheckedChanged += (s, e) => {
                if (chkUseKey.Checked)
                {
                    lblSelectedKey.ForeColor = Color.Black;
                }
                else
                {
                    lblSelectedKey.ForeColor = Color.Gray;
                }
            };

            // Add controls to the form
            Controls.Add(txtLog);
            Controls.Add(lblLog);
            Controls.Add(panelConfig);
            Controls.Add(lblTitle);
        }

        private void BtnSelectKey_Click(object sender, EventArgs e)
        {
            Form keyForm = new Form
            {
                Text = "Press a key",
                Size = new Size(300, 150),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false
            };

            Label lblInstruction = new Label
            {
                Text = "Press the key you want to use to stop the process",
                Location = new Point(20, 20),
                Size = new Size(260, 40),
                TextAlign = ContentAlignment.MiddleCenter
            };

            keyForm.Controls.Add(lblInstruction);
            keyForm.KeyDown += (s, ev) =>
            {
                stopKey = (byte)ev.KeyCode;

                if (invalidKeys.Contains(stopKey))
                {
                    MessageBox.Show("This key cannot be used to stop the process.", "Invalid Key", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string stopKeyName = keyNames.ContainsKey(stopKey) ? keyNames[stopKey] : ((char)stopKey).ToString();
                lblSelectedKey.Text = $"Key: {stopKeyName}";
                lblSelectedKey.ForeColor = Color.Black;
                keyForm.DialogResult = DialogResult.OK;
            };

            keyForm.ShowDialog();
        }

        private void BtnStart_Click(object sender, EventArgs e)
        {
            // Validate duration if enabled
            if (chkUseDuration.Checked)
            {
                if (!int.TryParse(txtDuration.Text, out int durationMinutes) || durationMinutes <= 0)
                {
                    MessageBox.Show("Please enter a valid number greater than 0 for the duration.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                endTime = DateTime.Now.AddMinutes(durationMinutes);
            }
            else
            {
                endTime = DateTime.MaxValue;
            }

            // Configure key to stop
            useStopKey = chkUseKey.Checked;

            // Update interface
            chkUseDuration.Enabled = false;
            txtDuration.Enabled = false;
            chkUseKey.Enabled = false;
            btnSelectKey.Enabled = false;
            btnStart.Enabled = false;
            btnStop.Enabled = true;
            lblStatus.Text = "Status: Running";
            lblStatus.ForeColor = Color.Green;

            // Clear log
            txtLog.Clear();
            Log("Starting process...");
            Log("Click on the Fortnite window and wait 3 seconds...");

            // Start process
            isRunning = true;
            
            // Start thread to monitor the stop key if configured
            if (useStopKey)
            {
                keyMonitorThread = new Thread(() => MonitorStopKey(stopKey));
                keyMonitorThread.IsBackground = true;
                keyMonitorThread.Start();
                string stopKeyName = keyNames.ContainsKey(stopKey) ? keyNames[stopKey] : ((char)stopKey).ToString();
                Log($"Monitoring key {stopKeyName} started");
            }

            // Start main thread
            executionThread = new Thread(ExecuteProcess);
            executionThread.IsBackground = true;
            executionThread.Start();
        }

        private void BtnStop_Click(object sender, EventArgs e)
        {
            StopProcess();
        }

        private void ExecuteProcess()
        {
            // Give time for the user to click on the Fortnite window
            Thread.Sleep(3000);
            Log("Process started");

            try
            {
                while (isRunning && DateTime.Now < endTime)
                {
                    int waitTime = random.Next(3, 31); // Between 3 and 30 seconds
                    
                    PressKey(VK_W);
                    PressKey(VK_S);
                    PressKey(VK_SPACE);
                    
                    // Random click
                    if (random.Next(0, 3) == 2)
                    {
                        Log("Left click");
                        mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
                        Thread.Sleep(100);
                        mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
                    }
                    
                    PressKey(VK_W);
                    
                    Log($"Waiting {waitTime} seconds...");
                    Thread.Sleep(waitTime * 1000);
                }
            }
            catch (Exception ex)
            {
                Log($"Error: {ex.Message}");
            }

            if (DateTime.Now >= endTime)
            {
                Log("Maximum time reached.");
            }

            // Update the interface in the UI thread
            Invoke(new Action(() =>
            {
                chkUseDuration.Enabled = true;
                txtDuration.Enabled = chkUseDuration.Checked;
                chkUseKey.Enabled = true;
                btnSelectKey.Enabled = chkUseKey.Checked;
                btnStart.Enabled = true;
                btnStop.Enabled = false;
                lblStatus.Text = "Status: Stopped";
                lblStatus.ForeColor = Color.Red;
            }));

            Log("Process stopped.");
        }

        private void PressKey(byte key)
        {
            int pressTime = random.Next(2, 6) * 1000; // Between 2 and 5 seconds
            string keyName = key == VK_SPACE ? "Space" : ((char)key).ToString();
            Log($"Pressing {keyName} for {pressTime/1000} seconds");
            keybd_event(key, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);
            Thread.Sleep(pressTime);
            keybd_event(key, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        }

        private void MonitorStopKey(byte key)
        {
            while (isRunning)
            {
                if ((GetAsyncKeyState(key) & 0x8000) != 0)
                {
                    string keyName = keyNames.ContainsKey(key) ? keyNames[key] : key.ToString();
                    Log($"Key {keyName} pressed. Stopping process...");
                    Invoke(new Action(StopProcess));
                    break;
                }
                Thread.Sleep(100);
            }
        }

        private void StopProcess()
        {
            if (!isRunning) return;
            
            isRunning = false;
            Log("Stopping process...");
            
            if (executionThread != null && executionThread.IsAlive)
            {
                executionThread.Join(1000);
            }
            
            if (keyMonitorThread != null && keyMonitorThread.IsAlive)
            {
                keyMonitorThread.Join(1000);
            }
        }

        private void Log(string message)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string>(Log), message);
                return;
            }

            txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\n");
            txtLog.ScrollToCaret();
        }
    }
}