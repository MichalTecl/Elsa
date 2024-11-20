using System;
using System.Collections.Generic;
using System.Text;

namespace BwApiClient.Model.Enums
{
    public enum NotificationType
    {
        /// <summary>
        /// Sends an e-mail notification to the assigned salesperson (if one is assigned).
        /// </summary>
        EMAIL_SALESPERSON,

        /// <summary>
        /// Sends an e-mail notification to the website administrator.
        /// </summary>
        EMAIL_ADMIN,

        /// <summary>
        /// Sends an e-mail notification to the customer's e-mail address.
        /// </summary>
        EMAIL_CUSTOMER,

        /// <summary>
        /// Sends an e-mail notification to a specified e-mail address, which must be provided in the 'extra' field of NotificationRequest.
        /// </summary>
        EMAIL_OTHER,

        /// <summary>
        /// Triggers a notification by calling the specified URL.
        /// </summary>
        WEB_HOOK,

        /// <summary>
        /// Fires a specific system event observed by custom handlers. Requires the 'Partner-Token'.
        /// </summary>
        SYSTEM_EVENT,

        /// <summary>
        /// Renders a notification in the system's notification center under the system messages section, visible to all administrators.
        /// </summary>
        APPTRAY_SYSTEM,

        /// <summary>
        /// Renders a notification in the system's notification center under the web-shop messages section, visible only to a specific user if indicated in the 'extra' field of NotificationRequest.
        /// </summary>
        APPTRAY_SHOP
    }
}
