using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace PMS.Pages.Allocation
{
    public class AllocationDashbaordModel : PageModel
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<AllocationDashbaordModel> _logger;
        public AllocationDashbaordModel(IConfiguration configuration, ILogger<AllocationDashbaordModel> logger)
        {
            _configuration = configuration;
            _logger = logger;
            RevenueDataList = new List<RevenueData>();
        }
        //public List<job> joblist = new List<job>();
        public int total_aloc { get; set; }
        public int pendig_aloc { get; set; }
        public int UnalocFile { get; set; }
        public decimal Aloc_Revenew { get; set; }

        public List<RevenueData> RevenueDataList { get; set; } = new List<RevenueData>();
        public List<Aloc_categ_wise> Aloc_categWise { get; set; } = new List<Aloc_categ_wise>();
        public List<File_policy_wise> FilePolicyWise { get; set; } = new List<File_policy_wise>();
        public List<Plot_categ_wise> PlotCategWise { get; set; } = new List<Plot_categ_wise>();


        public void OnGet()
        {
            try
            {
                string connectionString = _configuration.GetConnectionString("DefaultConnection");

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    // total Aloc
                    string sqlTotalAloc = "select count (plot_id) from customer_plot where customer_plot_id in (select min(customer_plot_id) from customer_plot where (customer_plot.is_allocated = 1) AND (customer_plot.processes_type = 'Allocation') AND (customer_plot.ownership_type <> 'Pending') group by plot_id)";
                    using (SqlCommand command = new SqlCommand(sqlTotalAloc, connection))
                    {
                        SqlDataReader reader = command.ExecuteReader();

                        while (reader.Read())
                        {

                            total_aloc = reader.GetInt32(0); // Assuming the count is returned as an integer

                        }

                        reader.Close();
                    }
                    // end total aloc
                    // pending Aloc
                    string sqlPendingAloc = "select count (plot_id) from customer_plot where customer_plot_id in (select min(customer_plot_id) from customer_plot where (customer_plot.is_active = 1) AND (customer_plot.is_allocated = 0) AND (customer_plot.processes_type = 'Allocation') AND (customer_plot.is_transfer = 0) AND (customer_plot.ownership_percentage <> '0%') AND (customer_plot.ownership_type = 'Pending') group by plot_id)";
                    using (SqlCommand command = new SqlCommand(sqlPendingAloc, connection))
                    {
                        SqlDataReader reader = command.ExecuteReader();

                        while (reader.Read())
                        {

                            pendig_aloc = reader.GetInt32(0); // Assuming the count is returned as an integer

                        }

                        reader.Close();
                    }
                    // end pending aloc

                    // unAloc
                    string sqlUnalocFile = "select count (plot_id) from customer_plot where customer_plot_id in (select min(customer_plot_id) from customer_plot where (customer_plot.is_active = 1) AND (customer_plot.is_allocated = 0) AND (customer_plot.processes_type = 'Assign Plot') AND  (customer_plot.ownership_percentage <> '0%') group by plot_id)";
                    using (SqlCommand command = new SqlCommand(sqlUnalocFile, connection))
                    {
                        SqlDataReader reader = command.ExecuteReader();

                        while (reader.Read())
                        {

                            UnalocFile = reader.GetInt32(0); // Assuming the count is returned as an integer

                        }

                        reader.Close();
                    }
                    // end pending aloc

                    // Aloc_Revenew
                    string sqlAloc_Revenew = "SELECT CAST(SUM(total_amount) / 1000000 AS DECIMAL(10,3)) AS total_amount_million\r\nFROM alloc_voucher\r\nWHERE is_paid = 1 AND processing_type = 'NORMAL' AND discard_voucher = 0";
                    using (SqlCommand command = new SqlCommand(sqlAloc_Revenew, connection))
                    {
                        SqlDataReader reader = command.ExecuteReader();

                        while (reader.Read())
                        {

                            Aloc_Revenew = reader.GetDecimal(0); // Assuming the count is returned as an integer

                        }

                        reader.Close();
                    }
                    // end Aloc_Revenew

                    // Aloc_Revenew_YearWise
                    string sqlAloc_Revenew_date_wise = "SELECT        YEAR(CONVERT(date, transaction_date, 105)) AS transaction_year, SUM(total_amount) AS total_amount\r\nFROM            alloc_voucher\r\nWHERE        (is_paid = 1) AND (processing_type = 'NORMAL') AND (discard_voucher = 0)\r\nGROUP BY YEAR(CONVERT(date, transaction_date, 105))\r\nORDER BY transaction_year";
                    using (SqlCommand command = new SqlCommand(sqlAloc_Revenew_date_wise, connection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            RevenueDataList = new List<RevenueData>();
                            while (reader.Read())
                            {
                                string? transactionyear = reader["transaction_year"]?.ToString();
                                decimal totalAmount = Convert.ToDecimal(reader["total_amount"]);

                                RevenueDataList.Add(new RevenueData
                                {
                                    Transactionyear = transactionyear,
                                    TotalAmount = totalAmount
                                });
                            }
                        }
                    }
                    // end Aloc_Revenew_YearWise


                    // Aloc_Categ_wise
                    string sqlAloc_Categ_wise = "SELECT        customer_categ.customer_categ, COUNT(customer_plot.plot_id) AS plot_count\r\nFROM            customer_plot INNER JOIN\r\n                         customer_categ ON customer_plot.customer_categ_id = customer_categ.customer_categ_id\r\nWHERE        (customer_plot.customer_plot_id IN\r\n                             (SELECT        MIN(customer_plot_id) AS Expr1\r\n                               FROM            customer_plot AS customer_plot_1\r\n                               WHERE        (is_allocated = 1) AND (processes_type = 'Allocation') AND (ownership_type <> 'Pending')\r\n                               GROUP BY plot_id))\r\nGROUP BY customer_plot.customer_categ_id, customer_categ.customer_categ";
                    using (SqlCommand command = new SqlCommand(sqlAloc_Categ_wise, connection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            Aloc_categWise = new List<Aloc_categ_wise>();
                            while (reader.Read())
                            {
                                string? customerCateg = reader["customer_categ"]?.ToString();
                                int total_count = Convert.ToInt32(reader["plot_count"]);

                                Aloc_categWise.Add(new Aloc_categ_wise
                                {
                                    customer_categ = customerCateg,
                                    totalcount = total_count

                                });

                            }
                        }
                    }
                    // end Aloc_Categ_wise
                    // File_policy_wise
                    string sqlFile_policy_wise = "SELECT COUNT(plot.plot_id) AS plot_count,\r\n      \r\n\t   CASE\r\n           WHEN plot.Ballot_id is not null THEN 'PB'\r\n           WHEN plot.svc_bnf_id is not null THEN 'SB'\r\n           WHEN plot.affidavit_policy_id is not null THEN 'INT'\r\n           WHEN plot.mrkt_policy_id is not null THEN 'MKT'\r\n           WHEN plot.shop_policy_id is not null THEN 'Shop'\r\n\r\n           ELSE ''\r\n       END AS File_Categ,\r\n\t    CONCAT(ballot.Ballot_title,  marketing_policy.mrkt_policy_title,  svc_policy.svc_bnf_title,  affidavit_policy.affidavit_title,  shop_policy.shop_policy_title) AS policy,CONCAT(plot.Ballot_id,plot.svc_bnf_id,plot.mrkt_policy_id,plot.shop_policy_id,plot.affidavit_policy_id) as id\r\nFROM plot\r\nLEFT JOIN shop_policy ON plot.shop_policy_id = shop_policy.shop_policy_id\r\nLEFT JOIN affidavit_policy ON plot.affidavit_policy_id = affidavit_policy.affidavit_policy_id\r\nLEFT JOIN svc_policy ON plot.svc_bnf_id = svc_policy.svc_bnf_id\r\nLEFT JOIN marketing_policy ON plot.mrkt_policy_id = marketing_policy.mrkt_policy_id\r\nLEFT JOIN ballot ON plot.Ballot_id = ballot.Ballot_id\r\n\r\nGROUP BY plot.Ballot_id, plot.svc_bnf_id, plot.affidavit_policy_id, plot.mrkt_policy_id, plot.shop_policy_id, ballot.Ballot_title, marketing_policy.mrkt_policy_title, svc_policy.svc_bnf_title, affidavit_policy.affidavit_title, shop_policy.shop_policy_title;;";
                    using (SqlCommand command = new SqlCommand(sqlFile_policy_wise, connection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            FilePolicyWise = new List<File_policy_wise>();
                            while (reader.Read())
                            {
                                int id = Convert.ToInt32(reader["id"]);
                                int plot_count = Convert.ToInt32(reader["plot_count"]);

                                string? File_Categ = reader["File_Categ"]?.ToString();
                                string? policy = reader["policy"]?.ToString();


                                FilePolicyWise.Add(new File_policy_wise
                                {
                                    id = id,
                                    file_categ = File_Categ,
                                    policy = policy,
                                    plot_count = plot_count


                                });

                            }
                        }
                    }
                    // end File_policy_wise



                }
            }
            catch (Exception ex)
            {

                _logger.LogError(ex, "An error occurred while retrieving allocation data.");
            }
        }


        public IActionResult plotCateg(int id, string plotCateg)
        {
            try
            {
                string connectionString = _configuration.GetConnectionString("DefaultConnection");

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    // plot_categ_wise
                    string sqlplot_categ_wise = "SELECT *\r\nFROM (\r\n    SELECT COUNT(plot.plot_id) AS plot_count,\r\n        CASE\r\n            WHEN plot.Ballot_id IS NOT NULL THEN 'PB'\r\n            WHEN plot.svc_bnf_id IS NOT NULL THEN 'SB'\r\n            WHEN plot.affidavit_policy_id IS NOT NULL THEN 'INT'\r\n            WHEN plot.mrkt_policy_id IS NOT NULL THEN 'MKT'\r\n            WHEN plot.shop_policy_id IS NOT NULL THEN 'DHA Shop'\r\n            ELSE ''\r\n        END AS File_Categ,\r\n        CONCAT(ballot.Ballot_title, marketing_policy.mrkt_policy_title, svc_policy.svc_bnf_title, affidavit_policy.affidavit_title, shop_policy.shop_policy_title) AS policy,\r\n        CONCAT(plot.Ballot_id, plot.svc_bnf_id, plot.mrkt_policy_id, plot.shop_policy_id, plot.affidavit_policy_id) AS id,\r\n    plot_categ.plot_type+' '+ plot_categ.plot_categ as plot_categ\r\n    FROM\r\n        plot\r\n        LEFT JOIN shop_policy ON plot.shop_policy_id = shop_policy.shop_policy_id\r\n        LEFT JOIN affidavit_policy ON plot.affidavit_policy_id = affidavit_policy.affidavit_policy_id\r\n        LEFT JOIN svc_policy ON plot.svc_bnf_id = svc_policy.svc_bnf_id\r\n        LEFT JOIN marketing_policy ON plot.mrkt_policy_id = marketing_policy.mrkt_policy_id\r\n        LEFT JOIN ballot ON plot.Ballot_id = ballot.Ballot_id\r\n\t\tinner join plot_categ on plot.plot_categ_id = plot_categ.plot_categ_id\r\n    GROUP BY\r\n        plot.Ballot_id,\r\n        plot.svc_bnf_id,\r\n        plot.affidavit_policy_id,\r\n        plot.mrkt_policy_id,\r\n        plot.shop_policy_id,\r\n        ballot.Ballot_title,\r\n        marketing_policy.mrkt_policy_title,\r\n        svc_policy.svc_bnf_title,\r\n        affidavit_policy.affidavit_title,\r\n        shop_policy.shop_policy_title,\r\n\t\tplot_categ.plot_categ,\r\n    plot_categ.plot_type\r\n) AS subquery\r\nWHERE\r\n    id = @plotId AND\r\n    File_Categ = @fileCateg;\r\n;";
                    using (SqlCommand command = new SqlCommand(sqlplot_categ_wise, connection))
                    {
                        // Add parameters
                        command.Parameters.AddWithValue("@plotId", id); // Replace with the actual plot ID value
                        command.Parameters.AddWithValue("@fileCateg", plotCateg); // Replace with the actual file category value
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            List<Plot_categ_wise> PlotCategWise = new List<Plot_categ_wise>();
                            while (reader.Read())
                            {
                                int policyid = Convert.ToInt32(reader["id"]);
                                int plot_count = Convert.ToInt32(reader["plot_count"]);
                                string? File_Categ = reader["File_Categ"]?.ToString();
                                string? plot_Categ = reader["plot_categ"]?.ToString();
                                string? policy = reader["policy"]?.ToString();

                                PlotCategWise.Add(new Plot_categ_wise
                                {
                                    id = policyid,
                                    file_categ = File_Categ,
                                    policy = policy,
                                    plot_categ = plot_Categ,
                                    plot_count = plot_count
                                });
                            }

                            return new JsonResult(PlotCategWise); // Return the list of plot categories as a JSON response
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving allocation data.");
                return StatusCode(500); // Return an appropriate error response
            }
        }





        public class RevenueData
        {
            public string? Transactionyear { get; set; }
            public decimal TotalAmount { get; set; }
        }

        public class Aloc_categ_wise
        {
            public string? customer_categ { get; set; }
            public int totalcount { get; set; }
            public int totalSBcount { get; set; }
            public int totalMKTcount { get; set; }
            public int totalBLTcount { get; set; }
            public int totalINTcount { get; set; }

        }

        public class File_policy_wise
        {
            public int id { get; set; }

            public string? file_categ { get; set; }
            public string? policy { get; set; }
            public int plot_count { get; set; }


        }


        public class Plot_categ_wise
        {
            public int id { get; set; }

            public string? file_categ { get; set; }
            public string? policy { get; set; }
            public string? plot_categ { get; set; }

            public int plot_count { get; set; }


        }

    }
}
