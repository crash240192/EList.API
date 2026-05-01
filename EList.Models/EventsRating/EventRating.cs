using EList.Common.Models;

namespace EList.Models.EventsRating
{
    public class EventRating : PagedList<EventsRatingItem>
    {
        public double ResultRating { get; private set; }   
        public EventRating(int total, double resultRating, List<EventsRatingItem> result, int pageIndex, int pageSize) : base(total, result, pageIndex, pageSize)
        {
            ResultRating = resultRating;
        }
    }
}
