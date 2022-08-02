using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32.TaskScheduler;

namespace PC_AutoShutdown {
    
    public sealed partial class MainWindow : Window {

        private readonly TextBox[] TimeSelectors_Hours;
        private readonly TextBox[] TimeSelectors_Minutes;
        
        private readonly string[] DayOfTheWeekNames = new string[] { "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"};

        public bool OutstandingChanges { get => outstandingChanges; set => SetOutstandingChanges(value); }
        private bool outstandingChanges = false;

        private readonly StoreConfig storeConfig;

        public MainWindow() {
            this.InitializeComponent();
            TimeSelectors_Hours = new TextBox[] { hh0, hh1, hh2, hh3, hh4, hh5, hh6 };
            TimeSelectors_Minutes = new TextBox[] { mm0, mm1, mm2, mm3, mm4, mm5, mm6 };

            Title = "AutoShutdown Tool";

            storeConfig = new StoreConfig();
            LoadConfigValues();

        }

        private void LoadConfigValues() {
            for(int i = 0; i < storeConfig.Stores.Length; i++) {
                cb_storeSelect.Items.Add(storeConfig.Stores[i].Name);
            }
            cb_storeSelect.SelectedIndex = 0;
        }

        private void cb_storeSelect_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            ReloadStoreTimes();
        }

        private void ReloadStoreTimes() {
            OutstandingChanges = true;
            if (storeConfig.Stores.Length <= cb_storeSelect.SelectedIndex) {
                return;
            }
            StoreConfigEntry selected = storeConfig.Stores[cb_storeSelect.SelectedIndex];
            for(int i = 0; i < TimeSelectors_Hours.Length; i++) {
                SetTimeForSelector(i, selected.ShutdownTimes[i]);
            }
        }

        private void SetTimeForSelector(int _weekDay, TimeSpan _time) {
            TimeSelectors_Hours[_weekDay].Text = _time.Hours.ToString("00");
            TimeSelectors_Minutes[_weekDay].Text = _time.Minutes.ToString("00");
        }

        private TimeSpan GetTimeFromSelector(int _weekDay) {
            if (!int.TryParse(TimeSelectors_Hours[_weekDay].Text, out int hour)) {
                return TimeSpan.Zero;
            }
            if (!int.TryParse(TimeSelectors_Minutes[_weekDay].Text, out int minute)) {
                return TimeSpan.Zero;
            }
            return new TimeSpan(0, hour, minute, 0);
        }
        
        private void btn_apply_Click(object _sender, RoutedEventArgs _e) {
            //Delete and create all 7 scheduled tasks
            using (TaskService ts = new TaskService()) {

                DateTime now = DateTime.Now;
                int weekDay = (int)now.DayOfWeek; //0 = sunday, 6 = saturday

                for(int i = 0; i < 7; i++) {
                    try {
                        ts.RootFolder.DeleteTask($"PCAutoShutdownTool-{DayOfTheWeekNames[i]}");
                    } catch (Exception) {
                        //We don't care if the task doesn't exists
                    }

                    //Only re-add tasks if desired by the toggle
                    if(cb_shutdown.IsChecked ?? false) {
                        DateTime startZero = new DateTime(now.Year, now.Month, now.Day) - new TimeSpan(24 * (weekDay - i), 0, 0);
                        DateTime startTime = startZero + GetTimeFromSelector(i);
                        
                        TaskDefinition td = ts.NewTask();
                        td.RegistrationInfo.Description = "PCAutoShutdownTool scheduled task to shutdown the computer at store closing time. Can be removed manually if desired.";
                        td.Triggers.Add(new DailyTrigger { DaysInterval = 7, Enabled = true, StartBoundary = startTime, EndBoundary = DateTime.MaxValue});

                        td.Actions.Add(new ExecAction("shutdown", "/s /t 0"));

                        ts.RootFolder.RegisterTaskDefinition($"PCAutoShutdownTool-{DayOfTheWeekNames[i]}", td);
                    }
                }
            }
            
            OutstandingChanges = false;
        }

        private void SetOutstandingChanges(bool _value) {
            outstandingChanges = _value;
            tb_statusInfo.Text = outstandingChanges ? $"Pending changes.{Environment.NewLine}Click the apply button to save them." : $"All changes saved.{Environment.NewLine}You may close this window now.";
            tb_statusInfo.Foreground = outstandingChanges ? Brushes.Red : Brushes.Green;
        }
        
        private void timeSelectors_TextChanged(object _sender, TextChangedEventArgs _e) {
            TextBox textBox = (TextBox) _sender;
            if (textBox.Text.Length > 2) {
                textBox.Text = textBox.Text.Substring(0, 2);
            }
            if (!int.TryParse(textBox.Text, out int number)) {
                textBox.Text = "00";
                return;
            }
            if (textBox.Name.StartsWith("hh")) {
                if (number > 23) {
                    number = 23;
                    textBox.Text = "23";
                }
                if (number < 0) {
                    number = 0;
                    textBox.Text = "00";
                }
            }
            if (textBox.Name.StartsWith("mm")) {
                if (number > 59) {
                    number = 59;
                    textBox.Text = "59";
                }
                if (number < 0) {
                    number = 0;
                    textBox.Text = "00";
                }
            }
            OutstandingChanges = true;
        }

        private void timeSelectors_NumberPreValidation(object _sender, TextCompositionEventArgs _e) {
            _e.Handled = (new Regex("[^0-9]+")).IsMatch(_e.Text) && _e.Text.Length <= 2;
        }

        private void timeSelectors_FocusLost(object _sender, RoutedEventArgs _e) {
            TextBox textBox = (TextBox) _sender;
            if (textBox.Text.Length < 2 && !textBox.IsFocused) {
                textBox.Text = "0" + textBox.Text;
            }
        }

        private void cb_ShutdownCheckboxChanged(object _sender, RoutedEventArgs _e) {
            OutstandingChanges = true;
        }
    }
}