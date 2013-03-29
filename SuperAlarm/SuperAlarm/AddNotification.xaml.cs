using System;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Navigation;
using System.Xml.Serialization;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Scheduler;
using Microsoft.Phone.Shell;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Media;

namespace SuperAlarm
{
    public partial class AddNotification : PhoneApplicationPage
    {
        private bool isEditMode = false;

        public static SuperAlarm SelectedAlarm = null;

        private static List<AlarmSound> alarmSounds = new List<AlarmSound>();


        public AddNotification()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // Create the alarms
            if (AddNotification.alarmSounds == null || AddNotification.alarmSounds.Count == 0)
            {
                for (int i = 1; i <= 6; i++)
                {
                    AlarmSound sound = new AlarmSound { Name = string.Format("alarm {0}", i), Path = string.Format("/Alarms/Alarm-0{0}.wma", i) };
                    alarmSounds.Add(sound);
                }
            }

            this.soundPicker.ItemsSource = alarmSounds;

            NavigationMode navMode = e.NavigationMode;
            string mode = this.NavigationContext.QueryString["mode"];
            this.isEditMode = string.Compare(mode, "edit") == 0;

            if (isEditMode)
            {
                this.page.Text = "edit";

                // Update the values from url params only if we are coming from home page
                if (navMode == NavigationMode.New)
                {
                    DateTime beginTime = SelectedAlarm.StartTime;
                    this.beginDatePicker.Value = beginTime.Date;
                    this.beginTimePicker.Value = beginTime;
                    this.titleTextBox.Text = SelectedAlarm.Name;

                    this.soundPicker.ItemsSource = alarmSounds;
                    var sound = alarmSounds.Where(x => x.Name == SelectedAlarm.Sound.Name).Single();
                    this.soundPicker.SelectedIndex = alarmSounds.IndexOf(sound);
                    AlarmSchedule schedule = SelectedAlarm.Schedule;

                    if (schedule.Repeats != RecurrenceInterval.None)
                    {
                        this.RepeatPicker.SelectedItem = schedule.Repeats.ToString();
                    }

                    // Based on recurrence type show the correct options
                    switch (schedule.Repeats)
                    {
                        case RecurrenceInterval.None:
                            {
                                this.OnlyOnceReadioBtn.IsChecked = true;
                                this.customStackPanel.Visibility = System.Windows.Visibility.Collapsed;
                                break;
                            }
                        default:
                            {
                                this.RepeatRadioBtn.IsChecked = true;
                                this.customStackPanel.Visibility = System.Windows.Visibility.Visible;

                                if (schedule.Occurrences != 0)
                                {
                                    this.AfterRadioBtn.IsChecked = true;
                                    this.AfterTextBox.Text = schedule.Occurrences.ToString();
                                    this.After_RadioButton_Checked(null, null);
                                }
                                else if (schedule.EndTime != null)
                                {
                                    this.EndsRadioBtn.IsChecked = true;
                                    this.endDatePicker.Value = schedule.EndTime;
                                }
                                else
                                {
                                    this.NeverRadioBtn.IsChecked = true;
                                }

                                switch (schedule.Repeats)
                                {
                                    case RecurrenceInterval.Weekly:
                                        {
                                            var days = schedule.DaysToRepeat.Select(x => x.ToString());

                                            foreach (CheckBox cb in this.weekPanel.AllChildren<CheckBox>())
                                            {
                                                if (days.Contains(cb.Tag.ToString()))
                                                {
                                                    cb.IsChecked = true;
                                                }
                                                else
                                                {
                                                    cb.IsChecked = false;
                                                }
                                            }
                                            break;
                                        }

                                    case RecurrenceInterval.Monthly:
                                        {
                                            var months = schedule.MonthsToRepeat.Select(x => x.ToString());

                                            foreach (CheckBox cb in this.monthPanel.AllChildren<CheckBox>())
                                            {
                                                if (months.Contains(cb.Tag.ToString()))
                                                {
                                                    cb.IsChecked = true;
                                                }
                                                else
                                                {
                                                    cb.IsChecked = false;
                                                }
                                            }
                                            break;
                                        }
                                }

                                break;
                            }
                    }
                }


                // Add delete button in edit mode if it's not already added.
                if (ApplicationBar.Buttons.Count == 1)
                {
                    ApplicationBarIconButton b = new ApplicationBarIconButton();
                    b.Text = "delete";
                    b.IconUri = new Uri("/Images/delete.png", UriKind.Relative);
                    b.Click -= ApplicationBarDeleteButton_Click;
                    b.Click += ApplicationBarDeleteButton_Click;
                    ApplicationBar.Buttons.Add(b);
                }
            }
        }

        private void ApplicationBarSaveButton_Click(object sender, EventArgs e)
        {
            // Get all the values from the forum
            string title = this.titleTextBox.Text;
            DateTime date = (DateTime)beginDatePicker.Value;
            DateTime time = (DateTime)beginTimePicker.Value;

            DateTime beginTime = date + time.TimeOfDay;

            if (beginTime < DateTime.Now)
            {
                MessageBox.Show("Sorry, Can't start an alarm in the past. Please select a time in the future");

                return;
            }

            AlarmSound sound = this.soundPicker.SelectedItem as AlarmSound;
            int noOfOccurences = 0;
            RecurrenceInterval recurrence = RecurrenceInterval.None;
            List<DayOfWeek> daysOfWeek = new List<DayOfWeek>();
            List<int> monthsOfYear = new List<int>();
            DateTime? expirationTime = null;

            if (this.RepeatRadioBtn.IsChecked.Value)
            {
                // Get the value from repeat picker
                recurrence = (RecurrenceInterval)Enum.Parse(typeof(RecurrenceInterval), this.RepeatPicker.SelectedItem.ToString(), true);

                if (recurrence == RecurrenceInterval.Weekly)
                {
                    // Get the days that are checked
                    daysOfWeek = this.weekPanel.AllChildren<CheckBox>().Where(x => x.IsChecked.Value).Select(x => (DayOfWeek)Enum.Parse(typeof(DayOfWeek), x.Tag.ToString())).ToList();

                }
                else if (recurrence == RecurrenceInterval.Monthly)
                {
                    monthsOfYear = this.monthPanel.AllChildren<CheckBox>().Where(x => x.IsChecked.Value).Select(x => Int32.Parse(x.Tag.ToString())).ToList();
                }

                if (!this.NeverRadioBtn.IsChecked.Value)
                {
                    if (this.AfterRadioBtn.IsChecked.Value)
                    {
                        noOfOccurences = Convert.ToInt32(this.AfterTextBox.Text);
                    }
                    else if (this.EndsRadioBtn.IsChecked.Value)
                    {
                        expirationTime = endDatePicker.Value;
                    }
                }
            }

            // Create an alarm schedule based on the selected options
            AlarmSchedule schedule = new AlarmSchedule(recurrence, expirationTime, noOfOccurences, daysOfWeek, monthsOfYear);

            // Create the super alarm with the schedule if not it edit mode
            if (!this.isEditMode)
            {
                SelectedAlarm = new SuperAlarm(title, beginTime, schedule, sound);
            }
            else
            {
                // Delete the existing simple alarms and create new ones with the new schedule
                this.ApplicationBarDeleteButton_Click(null, null);

                SelectedAlarm.StartTime = beginTime;
                SelectedAlarm.Name = title;
                SelectedAlarm.Sound = sound;
                SelectedAlarm.Schedule = schedule;

                // Create the simple alarms
                SelectedAlarm.Alarms = SelectedAlarm.CreateAlarms();
            }

            // Create the alarm
            SelectedAlarm.Alarms.ForEach(x => ScheduledActionService.Add(x.Create(SelectedAlarm.Schedule)));
            SelectedAlarm.IsScheduled = true;

            // also save the super alarm in isolate storage with the id as the key
            var settings = IsolatedStorageSettings.ApplicationSettings;

            if (settings.Contains(SelectedAlarm.ID))
            {
                settings[SelectedAlarm.ID] = SelectedAlarm;
            }
            else
            {
                settings.Add(SelectedAlarm.ID, SelectedAlarm);
            }

            // Save the settings 
            settings.Save();

            // Navigate back to the main reminder list page.
            NavigationService.GoBack();
        }

        private void ApplicationBarDeleteButton_Click(object sender, EventArgs e)
        {
            // Delete the selected super alarm from storage and also from the service
            SelectedAlarm.Alarms.ForEach(x =>
                {
                    // Check if the alarm is available in the service and remove it
                    if (ScheduledActionService.Find(x.Name) != null)
                    {
                        ScheduledActionService.Remove(x.Name);
                    }
                });

            // Remove the alarm from the store
            var settings = IsolatedStorageSettings.ApplicationSettings;
            settings.Remove(SelectedAlarm.ID);
            settings.Save();

            // Navigate back to the main reminder list page.
            NavigationService.GoBack();
        }

        private void Never_RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (this.SummaryTextBox != null)
            {
                this.SummaryTextBox.Text = "Never Ends";
            }
        }

        private void After_RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (this.SummaryTextBox != null)
            {
                this.SummaryTextBox.Text = "Ends after " + this.AfterTextBox.Text + " occurences";
            }
        }

        private void On_RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (this.SummaryTextBox != null)
            {
                this.SummaryTextBox.Text = "Ends on " + this.endDatePicker.Value.Value.ToShortDateString();
            }
        }

        private void CustomButton_Checked(object sender, RoutedEventArgs e)
        {
            this.customStackPanel.Visibility = System.Windows.Visibility.Visible;
        }

        private void CustomButton_UnChecked(object sender, RoutedEventArgs e)
        {
            this.customStackPanel.Visibility = System.Windows.Visibility.Collapsed;
        }

        private void RepeatPicker_SelectionChanged_1(object sender, SelectionChangedEventArgs e)
        {
            if (this.weekPanel != null && this.monthPanel != null)
            {
                // Based on the selection show the option
                string recurrence = (sender as ListPicker).SelectedItem as string;
                RecurrenceInterval recurrenceType = (RecurrenceInterval)Enum.Parse(typeof(RecurrenceInterval), recurrence);

                switch (recurrenceType)
                {
                    case RecurrenceInterval.Weekly:
                        {
                            this.weekPanel.Visibility = System.Windows.Visibility.Visible;
                            this.monthPanel.Visibility = System.Windows.Visibility.Collapsed;
                            
                            break;
                        }
                    case RecurrenceInterval.Monthly:
                    case RecurrenceInterval.EndOfMonth:
                        {
                            this.weekPanel.Visibility = System.Windows.Visibility.Collapsed;
                            this.monthPanel.Visibility = System.Windows.Visibility.Visible;

                            break;
                        }
                    default:
                        {
                            this.weekPanel.Visibility = System.Windows.Visibility.Collapsed;
                            this.monthPanel.Visibility = System.Windows.Visibility.Collapsed;
                            this.RepatsOn.Visibility = System.Windows.Visibility.Collapsed;

                            break;
                        }
                }
            }
        }

        /// <summary>
        /// Handle the tap gesture so that list picker doesnt close
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void playBtn_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            // Set handled as true so the list picker hosting the button doesnt close
            e.Handled = true;

            MediaPlayer.Stop();
            Song song = Song.FromUri("blah", new Uri((sender as Button).Tag.ToString(), UriKind.Relative));
            FrameworkDispatcher.Update();
            MediaPlayer.Play(song);
        }


        /// <summary>
        /// Close the list picker
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void audioTextBlk_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            MediaPlayer.Stop();
        }

        private void endDatePicker_Loaded_1(object sender, RoutedEventArgs e)
        {
            if (this.EndsRadioBtn != null && this.EndsRadioBtn.IsChecked.HasValue && this.EndsRadioBtn.IsChecked.Value)
            {
                this.On_RadioButton_Checked(sender, e);
            }
        }


        private void AfterTextBox_TextChanged_1(object sender, TextChangedEventArgs e)
        {
            if (this.AfterRadioBtn != null && this.AfterRadioBtn.IsChecked.HasValue && this.AfterRadioBtn.IsChecked.Value)
            {
                this.After_RadioButton_Checked(sender, e);
            }
        }

    }

}