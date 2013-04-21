using System.IO.IsolatedStorage;
using System.Windows.Controls.Primitives;
using Microsoft.Phone.Tasks;

namespace SuperAlarm
{
    public static class ReviewBugger
    {
        public const string ReviewKey = "numOfRuns";
        private const int numOfRunsBeforeFeedback = 5;
        private static int numOfRuns = -1;
        private static readonly IsolatedStorageSettings settings = IsolatedStorageSettings.ApplicationSettings;

        /// <summary>
        /// Check the no of times the app was launched
        /// </summary>
        public static void CheckNumOfRuns()
        {
            if (!settings.Contains(ReviewKey))
            {
                numOfRuns = 1;
                settings.Add(ReviewKey, 1);
            }
            else if (settings.Contains(ReviewKey) && (int)settings[ReviewKey] != -1)
            {
                settings.TryGetValue(ReviewKey, out numOfRuns);
                numOfRuns++;
                settings[ReviewKey] = numOfRuns;
            }

            settings.Save();
        }

        /// <summary>
        /// Did the user already made the review
        /// </summary>
        public static void DidReview()
        {
            if (settings.Contains(ReviewKey))
            {
                numOfRuns = -1;
                settings[ReviewKey] = -1;

                settings.Save();
            }
        }

        /// <summary>
        /// Is it time for review
        /// </summary>
        /// <returns></returns>
        public static bool IsTimeForReview()
        {
            bool isTime = numOfRuns % numOfRunsBeforeFeedback == 0 ? true : false;

            return isTime;
        }

        /// <summary>
        /// Prompt the user for review
        /// </summary>
        public static void PromptUser()
        {
            Popup popup = new Popup();
            popup.VerticalOffset = App.Current.Host.Content.ActualHeight / 3;
            ReviewPopupControl review = new ReviewPopupControl();
            popup.Child = review;
            popup.IsOpen = true;

            review.btnOk.Click += (s, args) =>
                {
                    MarketplaceReviewTask task = new MarketplaceReviewTask();
                    task.Show();

                    popup.IsOpen = false;
                    DidReview();
                };

            review.btnNo.Click += (s, args) =>
                {
                    numOfRuns = -1;
                    popup.IsOpen = false;
                };

            review.btnNever.Click += (s, args) =>
                {
                    DidReview();
                    popup.IsOpen = false;
                };
        }
    }
}
