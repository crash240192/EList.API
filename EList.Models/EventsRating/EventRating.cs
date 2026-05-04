using EList.Common.Models;

namespace EList.Models.EventsRating
{
    public class EventRating : PagedList<EventsRatingItem>
    {
        public double? ResultRating { get; private set; }   
        public EventRating(int totalCount, double? resultRating, List<EventsRatingItem>? result, int pageIndex, int pageSize) : base(totalCount, result, pageIndex, pageSize)
        {
            ResultRating = resultRating;
        }
    }
}
