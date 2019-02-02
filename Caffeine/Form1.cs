using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Caffeine
{
    public partial class MainForm : Form
    {
        [Flags]
        public enum EXECUTION_STATE : uint
        {
            ES_SYSTEM_REQUIRED = 0x00000001,
            ES_DISPLAY_REQUIRED = 0x00000002,
            ES_CONTINUOUS = 0x80000000
        }
        public SYSTEMTIMEOUTS TimeOuts => sysTimeouts;

        public struct SYSTEMTIMEOUTS
        {
            public int BATTERYIDLETIMEOUT;
            public int EXTERNALIDLETIMEOUT;
            public int WAKEUPIDLETIMEOUT;
        }

        [DllImport("USER32.DLL", CharSet = CharSet.Unicode)]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("USER32.DLL")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern EXECUTION_STATE SetThreadExecutionState(EXECUTION_STATE flags);

        [DllImport("user32.dll", SetLastError = true, EntryPoint = "SystemParametersInfo")]
        internal static extern int SystemParametersInfo(int uiAction, int uiParam, ref int pvParam, int fWinIni);

        private static System.Threading.Timer preventSleepTimer = null;
        public const int SPI_GETBATTERYIDLETIMEOUT = 252;
        public const int SPI_GETEXTERNALIDLETIMEOUT = 254;
        public const int SPI_GETWAKEUPIDLETIMEOUT = 256;
        public static int timeOutinMS = 0;
        public static int batteryIdleTimer;
        public static int externalIdleTimer;
        public static int wakeupIdleTimer;
        public static SYSTEMTIMEOUTS sysTimeouts;

        public MainForm()
        {
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            startButton_Click(sender, e);
        }

        public static void GetSystemTimeOuts()
        {
            sysTimeouts.BATTERYIDLETIMEOUT = -2;
            sysTimeouts.EXTERNALIDLETIMEOUT = -2;
            sysTimeouts.WAKEUPIDLETIMEOUT = -2;

            if (SystemParametersInfo(SPI_GETBATTERYIDLETIMEOUT, 0, ref batteryIdleTimer, 0) == 1)
                sysTimeouts.BATTERYIDLETIMEOUT = batteryIdleTimer;
            else
                sysTimeouts.BATTERYIDLETIMEOUT = -1;

            if (SystemParametersInfo(SPI_GETEXTERNALIDLETIMEOUT, 0, ref externalIdleTimer, 0) == 1)
                sysTimeouts.EXTERNALIDLETIMEOUT = externalIdleTimer;
            else
                sysTimeouts.EXTERNALIDLETIMEOUT = -1;

            if (SystemParametersInfo(SPI_GETWAKEUPIDLETIMEOUT, 0, ref wakeupIdleTimer, 0) == 1)
                sysTimeouts.WAKEUPIDLETIMEOUT = wakeupIdleTimer;
            else
                sysTimeouts.WAKEUPIDLETIMEOUT = -1;


            if (timeOutinMS < sysTimeouts.BATTERYIDLETIMEOUT)
                timeOutinMS = sysTimeouts.BATTERYIDLETIMEOUT;
            if (timeOutinMS < sysTimeouts.EXTERNALIDLETIMEOUT)
                timeOutinMS = sysTimeouts.EXTERNALIDLETIMEOUT;
            if (timeOutinMS < sysTimeouts.WAKEUPIDLETIMEOUT)
                timeOutinMS = sysTimeouts.WAKEUPIDLETIMEOUT;

            if (timeOutinMS == 0)
                timeOutinMS = 30;
        }

        public void DisableDeviceSleep()
        {
            SetThreadExecutionState(EXECUTION_STATE.ES_SYSTEM_REQUIRED | EXECUTION_STATE.ES_DISPLAY_REQUIRED | EXECUTION_STATE.ES_CONTINUOUS);
            mainTimer.Interval = timeOutinMS*1000;
            mainTimer.Start();
        }

        public static void EnableDeviceSleep()
        {
            preventSleepTimer.Dispose();
            preventSleepTimer = null;
        }

        private static void PokeDeviceToKeepAwake()
        {
            try
            {
                SetThreadExecutionState(EXECUTION_STATE.ES_SYSTEM_REQUIRED | EXECUTION_STATE.ES_DISPLAY_REQUIRED | EXECUTION_STATE.ES_CONTINUOUS);
                var handle = FindWindow("SysListView32", "FolderView");

                if (handle == IntPtr.Zero) return;
                SetForegroundWindow(handle);
                SendKeys.SendWait("%1");
            }
            catch
            {

            }
        }

        private void mainTimer_Tick(object sender, EventArgs e)
        { 
            PokeDeviceToKeepAwake();
        }

        private void startButton_Click(object sender, EventArgs e)
        {
            stopButton.Enabled = true;
            startButton.Enabled = false;
            GetSystemTimeOuts();
            timeout.Text = $"{timeOutinMS} seconds";
            DisableDeviceSleep();
        }

        private void stopButton_Click(object sender, EventArgs e)
        {
            stopButton.Enabled = false;
            startButton.Enabled = true;
            mainTimer.Stop();
            timeout.Text = @"Caffeine suspended";
        }
    }
}
