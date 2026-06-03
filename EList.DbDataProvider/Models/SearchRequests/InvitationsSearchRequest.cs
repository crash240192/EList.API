namespace EList.DbDataProvider.Models.SearchRequests
{
    public class InvitationsSearchRequest
    {
        public List<Guid> InviterAccountIds { get; set; }
        public List<Guid> InvitedAccountIds { get; set; }
        public List<Guid> InviterOrgIds { get; set; }
        public List<Guid> EventIds { get; set; }
        public bool? Viewed { get; set; }
        public int? PageSize {  get; set; }
        public int? PageIndex { get; set; }
    }
}
