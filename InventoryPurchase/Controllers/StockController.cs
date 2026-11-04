using System.Data;
using InventoryPurchase.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace InventoryPurchase.Controllers
{
    public class StockController : Controller
    {
        private readonly string connectionString;

        public StockController(IConfiguration configuration)
        {
            connectionString = configuration.GetConnectionString("DefaultConnection")!;
        }

        // not expired lots, in FIFO order
        public IActionResult Index()
        {
            return View(GetStock("sp_Stock_Available"));
        }

        public IActionResult Expired()
        {
            return View(GetStock("sp_Stock_Expired"));
        }

        private List<Stock> GetStock(string procedureName)
        {
            List<Stock> list = new List<Stock>();

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                SqlCommand cmd = new SqlCommand(procedureName, con);
                cmd.CommandType = CommandType.StoredProcedure;

                con.Open();
                SqlDataReader dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    Stock s = new Stock();
                    s.StockId = Convert.ToInt32(dr["StockId"]);
                    s.ProductId = Convert.ToInt32(dr["ProductId"]);
                    s.ProductName = dr["ProductName"].ToString();
                    s.LotNo = dr["LotNo"].ToString()!;
                    s.ExpiryDate = dr["ExpiryDate"] == DBNull.Value ? null : Convert.ToDateTime(dr["ExpiryDate"]);
                    s.Quantity = Convert.ToDecimal(dr["Quantity"]);
                    s.PurchaseRate = Convert.ToDecimal(dr["PurchaseRate"]);
                    s.PurchaseId = Convert.ToInt32(dr["PurchaseId"]);
                    s.ReceivedOn = Convert.ToDateTime(dr["ReceivedOn"]);
                    s.FifoOrder = Convert.ToInt32(dr["FifoOrder"]);
                    list.Add(s);
                }
            }
            return list;
        }
    }
}
