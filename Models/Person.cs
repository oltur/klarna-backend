using System;

namespace Models
{
    public class Person
    {
        public string id{ get; set; }
        public string name{ get; set; }
        public string phone{ get; set; }
        public string picture{ get; set; }
        public string email{ get; set; }
        public int birthday{ get; set; }
        public Address address { get; set; }

        public string birthdayString {
            get
            {
                DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                return epoch.AddSeconds(birthday).ToString();
            }
        }

        public int age
        {
            get
            {
                DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                DateTime zeroTime = new DateTime(1, 1, 1);
                int years = (zeroTime + (DateTime.Now - epoch.AddSeconds(birthday))).Year - 1;
                return years;
            }
        }
    }

    public class Address
    {
        public string city { get; set; }
        public string street { get; set; }
        public string country { get; set; }

    }
}
