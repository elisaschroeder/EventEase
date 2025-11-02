namespace EventEase.Models
{
    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
        public bool HasNextPage => Page < TotalPages;
        public bool HasPreviousPage => Page > 1;
        public int StartItem => (Page - 1) * PageSize + 1;
        public int EndItem => Math.Min(Page * PageSize, TotalCount);
        public bool IsFirstPage => Page == 1;
        public bool IsLastPage => Page == TotalPages;
        public int NextPage => HasNextPage ? Page + 1 : Page;
        public int PreviousPage => HasPreviousPage ? Page - 1 : Page;
    }

    public class PaginationRequest
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string SortBy { get; set; } = "Date";
        public SortDirection SortDirection { get; set; } = SortDirection.Ascending;
        public string? SearchTerm { get; set; }
        public List<string> Filters { get; set; } = new();

        public void Validate()
        {
            if (Page < 1) Page = 1;
            if (PageSize < 1) PageSize = 10;
            if (PageSize > 100) PageSize = 100; // Max page size limit
        }
    }

    public enum SortDirection
    {
        Ascending,
        Descending
    }
}