using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PortChatBot.Models
{
    [Serializable]
    public class WeatherList
    {
        public string year;
        public string month;
        public string day;
        public string time;
        public string weather;
        public int rainfall;
        public int wind;
        public int humidity;
        public string ernam;
        public string erdat;
        public string erzet;
    }
}
