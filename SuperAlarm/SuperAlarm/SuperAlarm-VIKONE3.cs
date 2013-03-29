using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Phone.Scheduler;

namespace SuperAlarm
{
    /// <summary>
    ///  A super alarm will contain one or more alarms internally.
    ///  But only one is shown to the user
    /// </summary>
    [DataContract]
    public class SuperAlarm
    {
        [DataMember]
        public bool IsScheduled { get; set; }

        [DataMember]
        public List<SimpleAlarm> Alarms { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public DateTime StartTime { get; set; }

        [DataMember]
        public DateTime EndTime { get; set; }

        [DataMember]
        public AlarmSound Sound { get; set; }

        [DataMember]
        public string ID { get; set; }

        [DataMember]
        public AlarmSchedule Schedule { get; set; }

        [DataMember]
        public string Time { get; set; }

        public SuperAlarm()
        {
        }

        /// <summary>
        /// Super alarm
        /// </summary>
        /// <param name="name"></param>
        /// <param name="startTime"></param>
        /// <param name="schedule"></param>
        /// <param name="sound"></param>
        public SuperAlarm(string name, DateTime startTime, AlarmSchedule schedule, AlarmSound sound)
        {
            this.Name = name;
            this.StartTime = startTime;
            this.Time = startTime.ToString("hh:mm tt");

            this.Sound = sound;
            this.Schedule = schedule;

            // Unique ID for the super alarm. 
            this.ID = Guid.NewGuid().ToString();

            // Create the alarms based on the schedule
            this.Alarms = this.CreateAlarms();
        }

        /// <summary>
        /// Create sub alarms based on the schedule
        /// </summary>
        /// <param name="name"></param>
        /// <param name="startTime"></param>
        /// <param name="schedule"></param>
        public List<SimpleAlarm> CreateAlarms()
        {
            var alarms = new List<SimpleAlarm>();

            if (this.Schedule != null)
            {
                switch (this.Schedule.Repeats)
                {
                    case RecurrenceInterval.None:
                    case RecurrenceInterval.Daily:
                    case RecurrenceInterval.Yearly:
                        {
                            var alarm = this.CreateAlarm(this.StartTime);

                            // Add to our list
                            alarms.Add(alarm);

                            break;
                        }
                    case RecurrenceInterval.Weekly:
                        {
                            // If this is a simple case of repeating weekly, then create the alarm
                            if (this.Schedule.DaysToRepeat == null || this.Schedule.DaysToRepeat.Count == 0)
                            {
                                var alarm = this.CreateAlarm(this.StartTime);

                                // Add to our list
                                alarms.Add(alarm);
                            }
                            else
                            {
                                foreach (DayOfWeek dayOfWeek in this.Schedule.DaysToRepeat)
                                {
                                    // Create an alarm that starts on that specific day of the week
                                    for (int i = 0; i < 7; i++)
                                    {
                                        DateTime newStart = this.StartTime.AddDays(i);

                                        if (newStart.DayOfWeek == dayOfWeek)
                                        {
                                            var alarm = this.CreateAlarm(newStart);

                                            // Add to our list
                                            alarms.Add(alarm);

                                            // If we found the day in this week, then go to next day of week.
                                            break;
                                        }
                                    }
                                }
                            }

                            break;
                        }

                    case RecurrenceInterval.Monthly:
                    case RecurrenceInterval.EndOfMonth:
                        {
                            // If this is a simple case of repeating monthly, then create the alarm
                            if (this.Schedule.MonthsToRepeat == null || this.Schedule.MonthsToRepeat.Count == 0)
                            {
                                var alarm = this.CreateAlarm(this.StartTime);

                                // Add to our list
                                alarms.Add(alarm);
                            }
                            else
                            {
                                foreach (int month in this.Schedule.MonthsToRepeat)
                                {
                                    // Create an alarm that starts on that specific day of the month
                                    for (int i = 0; i < 12; i++)
                                    {
                                        DateTime newStart = this.StartTime.AddMonths(i);

                                        if (newStart.Month == month)
                                        {
                                            var alarm = this.CreateAlarm(newStart);

                                            // Add to our list
                                            alarms.Add(alarm);

                                            // If we found the day in this week, then go to next day of week.
                                            break;
                                        }
                                    }
                                }
                            }

                            break;
                        }
                }
            }

            return alarms;
        }

        /// <summary>
        /// Creates an Alarm
        /// </summary>
        /// <param name="name"></param>
        /// <param name="startTime"></param>
        /// <param name="schedule"></param>
        /// <returns></returns>
        private SimpleAlarm CreateAlarm(DateTime startTime)
        {
            string id = string.Format("{0}_{1}", this.ID, Guid.NewGuid().ToString());

            return new SimpleAlarm(id, this.Name, startTime, this.Sound);
        }
    }

    /// <summary>
    /// Schedule of the super alarm
    /// </summary>
    [DataContract]
    public class AlarmSchedule
    {
        [DataMember]
        public RecurrenceInterval Repeats { get; set; }

        [DataMember]
        public DateTime? EndTime { get; set; }

        [DataMember]
        public int Occurrences { get; set; }

        [DataMember]
        public List<DayOfWeek> DaysToRepeat { get; set; }

        [DataMember]
        public List<int> MonthsToRepeat { get; set; }

        public AlarmSchedule()
        {
        }

        /// <summary>
        /// Alarm schedule
        /// </summary>
        /// <param name="recurrence"></param>
        /// <param name="endTime"></param>
        /// <param name="occurrences"></param>
        /// <param name="daysToRepeat"></param>
        public AlarmSchedule(RecurrenceInterval recurrence, DateTime? endTime, int occurrences, List<DayOfWeek> daysToRepeat, List<int> monthsToRepeat)
        {
            this.Repeats = recurrence;
            this.EndTime = endTime;
            this.Occurrences = occurrences;

            // Check that the days to repeat are unique and not repeating
            if (daysToRepeat.Distinct().Count() != daysToRepeat.Count)
            {
                throw new ArgumentException("Repeated days should be unique");
            }

            this.DaysToRepeat = daysToRepeat;

            // Check that the months to repeat are unique and not repeating
            if (monthsToRepeat.Distinct().Count() != monthsToRepeat.Count)
            {
                throw new ArgumentException("Repeated months should be unique");
            }

            this.MonthsToRepeat = monthsToRepeat;
        }

    }

    [DataContract]
    public class AlarmSound
    {
        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string Path { get; set; }

        public AlarmSound()
        {

        }
    }

    /// <summary>
    /// A simple alarm class that can be serialized
    /// </summary>
    [DataContract]
    public class SimpleAlarm
    {
        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string Content { get; set; }

        [DataMember]
        public DateTime Start { get; set; }

        [DataMember]
        public AlarmSound Sound { get; set; }

        [DataMember]
        public RecurrenceInterval Recurrence { get; set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        public SimpleAlarm()
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="name"></param>
        /// <param name="content"></param>
        /// <param name="start"></param>
        /// <param name="sound"></param>
        public SimpleAlarm(string name, string content, DateTime start, AlarmSound sound)
        {
            this.Name = name;
            this.Content = content;
            this.Start = start;
            this.Sound = sound;
        }

        /// <summary>
        /// Create alarm based on schedule
        /// </summary>
        /// <param name="schedule"></param>
        /// <returns></returns>
        public Alarm Create(AlarmSchedule schedule)
        {
            Alarm alarm = new Alarm(this.Name);
            alarm.Content = this.Content;
            alarm.Sound = new Uri(this.Sound.Path, UriKind.RelativeOrAbsolute);
            alarm.BeginTime = this.GetBeginTime(this.Start, schedule);

            alarm.RecurrenceType = schedule.Repeats;

            DateTime? endTime = this.GetExpirationTime(this.Start, schedule);

            // Set the expiration if available
            if (endTime != null)
            {
                alarm.ExpirationTime = endTime.Value;
            }

            return alarm;
        }

        /// <summary>
        /// Get the start time for the alarm
        /// </summary>
        /// <param name="startTime"></param>
        /// <param name="schedule"></param>
        /// <returns></returns>
        private DateTime GetBeginTime(DateTime startTime, AlarmSchedule schedule)
        {
            return startTime;
        }


        /// <summary>
        /// Gets the expiration team based on the end time or no of occurences
        /// A schedule cant have both no of occurences and 
        /// </summary>
        /// <param name="startTime"></param>
        /// <param name="schedule"></param>
        /// <returns></returns>
        private DateTime? GetExpirationTime(DateTime startTime, AlarmSchedule schedule)
        {
            if (schedule.EndTime != null)
            {
                return schedule.EndTime;
            }

            if (schedule.Occurrences != 0)
            {
                switch (schedule.Repeats)
                {
                    case RecurrenceInterval.Daily:
                        {
                            return startTime.AddDays(schedule.Occurrences);
                        }
                    case RecurrenceInterval.Weekly:
                        {
                            return startTime.AddDays(7 * schedule.Occurrences);
                        }
                    case RecurrenceInterval.Monthly:
                    case RecurrenceInterval.EndOfMonth:
                        {
                            return startTime.AddMonths(schedule.Occurrences);
                        }

                    case RecurrenceInterval.Yearly:
                        {
                            return startTime.AddYears(schedule.Occurrences);
                        }
                    default:
                        {
                            return null;
                        }
                }
            }

            return null;
        }
    }
}
