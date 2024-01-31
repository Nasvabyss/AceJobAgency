﻿namespace AceJobAgency.Model
{
    public class UserSession
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public string SessionId { get; set; }
        public DateTime LoginTime { get; set; }
        public bool IsActive { get; set; }
    }
}
