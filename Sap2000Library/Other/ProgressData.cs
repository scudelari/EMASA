using System.Windows.Media;

namespace Sap2000Library.Other
{
    public class ProgressData
    {
        public int? current { get; private set; }
        public int? end { get; private set; }
        public long? currentLong { get; private set; }
        public long? endLong { get; private set; }
        public string itemName { get; private set; }
        public string message { get; private set; }
        public Brush brush { get; private set; }
        public bool isIndeterminate { get; private set; }
        public ProgressDataAction action { get; private set; }

        // Can't just create
        private ProgressData() { }

        /// <summary>
        /// Sets the current progress to the message and the visual bar.
        /// </summary>
        /// <param name="current">Current iteration.</param>
        /// <param name="end">Final iteration.</param>
        /// <param name="itemName">Optional iteration descriptor that will be substitute the *** set in the StatusBar_SetMessage function.</param>
        public static ProgressData UpdateProgress(int current, int end, string itemName = null, string message = null)
        {
            return new ProgressData()
            {
                current = current,
                end = end,
                itemName = itemName,
                message = message,
                action = ProgressDataAction.UpdateProgress
            };
        }
        /// <summary>
        /// Sets the current progress to the message and the visual bar.
        /// </summary>
        /// <param name="current">Current iteration.</param>
        /// <param name="end">Final iteration.</param>
        /// <param name="itemName">Optional iteration descriptor that will be substitute the *** set in the StatusBar_SetMessage function.</param>
        /// <param name="message">The message to display.</param>
        public static ProgressData UpdateProgress(long current, long end, string itemName = null, string message = null)
        {
            return new ProgressData()
            {
                currentLong = current,
                endLong = end,
                itemName = itemName,
                message = message,
                action = ProgressDataAction.UpdateProgress
            };
        }

        /// <summary>
        /// Sets the current progress to the message the visual bar to an indeterminate state (will keep changing).
        /// </summary>
        /// <param name="itemName">Optional iteration description that will be substitute the *** set in the StatusBar_SetMessage function.</param>
        /// <param name="message">The message to display.</param>
        public static ProgressData UpdateProgressIndeterminate(string itemName = null)
        {
            return new ProgressData()
            {
                itemName = itemName,
                action = ProgressDataAction.UpdateProgressIndeterminate
            };
        }

        /// <summary>
        /// Sets the message. 
        /// You may add text that will only show for each subsequent progress reporting. In this case:
        /// * Everything enclosed by double [[ ]] will be removed from the text and added only for each iteration.
        /// * In each iteration, *** will be substituted by the itemName parameter of the StatusBar_UpdateProgress function.
        /// </summary>
        /// <param name="message">The message to display.</param>
        /// <param name="isIndeterminate">Makes the bar to keep floating.</param>
        /// <param name="brush">The brush to use.</param>
        public static ProgressData SetMessage(string message, bool isIndeterminate = false, Brush brush = null)
        {
            return new ProgressData()
            {
                message = message,
                isIndeterminate = isIndeterminate,
                brush = brush,
                action = ProgressDataAction.SetMessage
            };
        }

        /// <summary>
        /// Resets the status bar to the idle configuration.
        /// </summary>
        public static ProgressData Reset()
        {
            return new ProgressData()
            {
                action = ProgressDataAction.Reset
            };
        }

        public enum ProgressDataAction { SetMessage, Reset, UpdateProgress, UpdateProgressIndeterminate }
    }
}
