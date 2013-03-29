using System;
using System.Collections.Generic;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using System.Windows.Resources;
using System.Xml.Serialization;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Scheduler;
using Microsoft.Phone.Shell;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Media;
using SuperAlarm.Resources;

namespace SuperAlarm
{
    public partial class MainPage : PhoneApplicationPage
    {
        // Constructor
        public MainPage()
        {
            this.InitializeComponent();
        }

        private void ResetItemsList()
        {
            var settings = IsolatedStorageSettings.ApplicationSettings;
            //settings.Clear();
            //ScheduledActionService.GetActions<Alarm>().ToList().ForEach(x => ScheduledActionService.Remove(x.Name));

            var superAlarms = settings.Select(x => x.Value as SuperAlarm).ToList();

            // If there are 1 or more reminders, hide the "no reminders"
            // TextBlock. IF there are zero reminders, show the TextBlock.
            if (superAlarms.Count() > 0)
            {
                EmptyTextBlock.Visibility = Visibility.Collapsed;
                NotificationListBox.ItemsSource = superAlarms;
            }
            else
            {
                NotificationListBox.ItemsSource = null;
                EmptyTextBlock.Visibility = Visibility.Visible;
            }

        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            // Reset the ReminderListBox items when the page is navigated to.
            ResetItemsList();
        }

        private void ApplicationBarAddButton_Click(object sender, EventArgs e)
        {
            // Navigate to the AddReminder page when the add button is clicked.
            NavigationService.Navigate(new Uri("/AddNotification.xaml?mode=new", UriKind.RelativeOrAbsolute));
        }

        private void NotificationListBox_SelectionChanged_1(object sender, SelectionChangedEventArgs e)
        {
            // Get the item clicked
            object listBoxItem = (sender as ListBox).SelectedItem;

            if (listBoxItem != null)
            {
                SuperAlarm alarm = listBoxItem as SuperAlarm;

                if (alarm != null)
                {
                    // Navigate to the AddReminder page when the alarm is clicked in edit mode.
                    AddNotification.SelectedAlarm = alarm;
                    NavigationService.Navigate(new Uri("/AddNotification.xaml?mode=edit", UriKind.RelativeOrAbsolute));
                }
            }
        }

        private void ToggleSwitch_Checked_1(object sender, RoutedEventArgs e)
        {
            ToggleSwitch ts = sender as ToggleSwitch;

            var alarm = (ts.DataContext) as SuperAlarm;

            ts.Content = "ON : " + alarm.Name + Environment.NewLine + alarm.Schedule.ToString();

            // Add the alarms to the service if they were removed
            if (!alarm.IsScheduled)
            {
                // Check the start time of the alarm to see if it's in the past. If yes, then add one day to begin time
                if (alarm.StartTime < DateTime.Now)
                {
                    alarm.StartTime += TimeSpan.FromDays(1);

                    // Create a new set of alarma based on the start date
                    alarm.Alarms = alarm.CreateAlarms();
                }

                alarm.Alarms.ForEach(x => ScheduledActionService.Add(x.Create(alarm.Schedule)));

                // Mark the alarm as scheduled
                alarm.IsScheduled = true;

                IsolatedStorageSettings.ApplicationSettings[alarm.ID] = alarm;
                IsolatedStorageSettings.ApplicationSettings.Save();
            }
        }

        private void ToggleSwitch_Unchecked_1(object sender, RoutedEventArgs e)
        {
            ToggleSwitch ts = sender as ToggleSwitch;

            var alarm = (ts.DataContext) as SuperAlarm;

            ts.Content = "OFF : " + alarm.Name + Environment.NewLine + alarm.Schedule.ToString();

            // Remove the alarms from the service if scheduled
            if (alarm.IsScheduled)
            {
                alarm.Alarms.ForEach(x =>
                    {
                        if (ScheduledActionService.Find(x.Name) != null)
                        {
                            ScheduledActionService.Remove(x.Name);
                        }
                    });

                // Mark the alarm as unscheduled
                alarm.IsScheduled = false;
                IsolatedStorageSettings.ApplicationSettings[alarm.ID] = alarm;
                IsolatedStorageSettings.ApplicationSettings.Save();
            }
        }
    }
}