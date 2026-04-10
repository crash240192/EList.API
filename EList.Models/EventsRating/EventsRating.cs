using EList.Models.Enums;
using LinqToDB.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EList.Models.EventsRating
{
    public class EventsRating
    {
        public Guid Id { get; set; }

        public Guid AccountId { get; set; }

        public Guid EventId { get; set; }

        public string Comment { get; set; }

        public int Value { get; set; }

        public EventRatingType RatingType { get; set; }
    }
}
