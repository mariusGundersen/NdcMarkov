namespace ConsoleApplication
{
    using System.Collections.Generic;

    public class TwitterResponse
    {
        public List<Status> statuses { get; set; }
    }

    public class Status
    {
        public string text { get; set; }

        public bool retweeted { get; set; }
    }
}