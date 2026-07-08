namespace Sales.Domain.Enums;

/// <summary>
/// The delivery channel used to send a notification to a customer.
/// </summary>
public enum NotificationChannel
{
    /// <summary>Firebase / APNs push notification to the mobile app.</summary>
    Push = 0,

    /// <summary>SMS text message to the customer's phone number.</summary>
    Sms = 1,

    /// <summary>Email to the customer's registered email address.</summary>
    Email = 2,

    /// <summary>In-app inbox message — visible inside the app notification centre.</summary>
    InApp = 3,

    /// <summary>WhatsApp message via WhatsApp Business API.</summary>
    WhatsApp = 4
}
