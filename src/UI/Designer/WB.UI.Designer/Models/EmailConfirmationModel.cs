﻿namespace WB.UI.Designer.Models
{
    public class EmailConfirmationModel : IEmailNotification
    {
        public string ConfirmationToken { get; set; }
        public string UserName { get; set; }

        public string Email { get; set; }
    }
}