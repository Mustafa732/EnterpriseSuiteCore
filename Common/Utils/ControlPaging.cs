using System.Data;
using System.Text;
using System.Text.RegularExpressions;

namespace Common.Utils
{
    public class ControlPaging
    {
        #region Class Variables

        static int _CurrentPageIndex = 0;
        static int _PageSize = 10;
        DataTable dt = new DataTable();

        #endregion

        public ControlPaging()
        {
        }

        #region Class Properties

        public int CurrentPageIndex
        {
            get { return _CurrentPageIndex; }
            set { _CurrentPageIndex = value; }
        }

        public int PageSize
        {
            get { return _PageSize; }
            set { _PageSize = value; }
        }

        public DataTable DataSource
        {
            get { return dt; }
            set { dt = value ?? new DataTable(); }
        }

        public bool IsLastPage
        {
            get { return CurrentPageIndex >= PageCount - 1; }
        }

        public bool IsFirstPage
        {
            get { return CurrentPageIndex <= 0; }
        }

        public int PageCount
        {
            get
            {
                if (DataSource == null || DataSource.Rows.Count == 0) return 1;
                return CalculateNumberOfPages(DataSource.Rows.Count, PageSize);
            }
        }

        // OLD: PagedDataSource DoPaging
        // NEW: DataView DoPaging - Same kaam, bas.NET 8 compatible
        public DataView DoPaging
        {
            get
            {
                if (DataSource == null || DataSource.Rows.Count == 0)
                    return new DataView();

                DataTable pagedTable = DataSource.Clone();
                int start = CurrentPageIndex * PageSize;
                int end = Math.Min(start + PageSize, DataSource.Rows.Count);

                for (int i = start; i < end; i++)
                {
                    pagedTable.ImportRow(DataSource.Rows[i]);
                }

                return pagedTable.DefaultView;
            }
        }

        // Extra helper for MVC - List ke liye
        public DataTable GetPagedData()
        {
            if (DataSource == null || DataSource.Rows.Count == 0)
                return new DataTable();

            DataTable pagedTable = DataSource.Clone();
            int start = CurrentPageIndex * PageSize;
            int end = Math.Min(start + PageSize, DataSource.Rows.Count);

            for (int i = start; i < end; i++)
            {
                pagedTable.ImportRow(DataSource.Rows[i]);
            }
            return pagedTable;
        }

        #endregion

        #region Class Methods - SAME AS OLD

        public static int CalculateNumberOfPages(int totalNumberOfItems, int pageSize)
        {
            if (pageSize <= 0) return 1;
            var result = totalNumberOfItems % pageSize;
            if (result == 0)
                return totalNumberOfItems / pageSize;
            else
                return totalNumberOfItems / pageSize + 1;
        }

        public static int GetStartRange(int selectedPageIndex)
        {
            int StartIndex = 1;
            string tempindex = selectedPageIndex.ToString();

            if (selectedPageIndex > 10)
            {
                int lastdigit = int.Parse(tempindex.Substring(tempindex.Length - 1));

                if (lastdigit == 0)
                    StartIndex = selectedPageIndex - 10;
                else
                    StartIndex = selectedPageIndex - lastdigit;
            }

            return StartIndex;
        }

        public static List<int> GetListOfPages(int totalNumberOfPages)
        {
            List<int> PageList = new List<int>();
            int PageCount = 1;

            if (totalNumberOfPages < 10)
                PageList.Add(PageCount);
            else
            {
                while (totalNumberOfPages > 0)
                {
                    totalNumberOfPages -= 10;
                    PageList.Add(PageCount);
                    PageCount += 10;
                }
            }

            return PageList;
        }

        public static string ApplyPagingQueryWithCount(string query)
        {
            string TempQuery = query.ToLower();
            StringBuilder str = new StringBuilder();
            str.Append("WITH ResultSet AS\r\n");
            str.Append("(\r\n");

            string frommatch = "\\bfrom\\b";

            MatchCollection m = Regex.Matches(TempQuery, frommatch, RegexOptions.Multiline);
            if (m.Count == 0) return query;

            int IndexOf_LastOrderBy = m[m.Count - 1].Index;
            string TempQueryLastFrom = TempQuery.Substring(IndexOf_LastOrderBy);
            int IndexFirstFrom = Regex.Match(TempQuery, frommatch, RegexOptions.Multiline).Index;

            if (TempQueryLastFrom.Contains("order by"))
            {
                IndexOf_LastOrderBy = IndexOf_LastOrderBy + TempQueryLastFrom.IndexOf("order by");
                string OrderByClause = TempQueryLastFrom.Substring(TempQueryLastFrom.IndexOf("order by"));
                query = query.Substring(0, IndexOf_LastOrderBy);
                query = query.Insert(IndexFirstFrom, ", ROW_NUMBER() Over (" + OrderByClause + ") as 'Row' \r\n");
            }
            else
            {
                int AfterSelect = TempQuery.IndexOf("select") + 6;
                int FirstComma = TempQuery.IndexOf(",");
                if (FirstComma > 0)
                {
                    string FirstColumn = TempQuery.Substring(AfterSelect, FirstComma - AfterSelect).Replace("distinct", "");
                    query = query.Insert(IndexFirstFrom, ", ROW_NUMBER() Over (Order by " + FirstColumn + ") as 'Row' \r\n");
                }
            }

            str.Append(query + "\r\n");
            str.Append(")\r\n");
            str.Append("select *\r\n");
            str.Append("from ResultSet\r\n");
            str.Append("WHERE Row between\r\n");
            str.Append("(@PageIndex - 1) * @PageSize + 1 and @PageIndex*@PageSize;\r\n");
            str.Append("set @TotalRows = (select count(*)");
            str.Append("from");
            str.Append("(\r\n");
            str.Append(query + "\r\n");
            str.Append(") as Query)\r\n");

            return str.ToString();
        }

        #endregion
    }
}