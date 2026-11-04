using System.Data;
using InventoryPurchase.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;

namespace InventoryPurchase.Controllers
{
    public class PurchaseController : Controller
    {
        private readonly string connectionString;

        public PurchaseController(IConfiguration configuration)
        {
            connectionString = configuration.GetConnectionString("DefaultConnection")!;
        }

        public IActionResult Index()
        {
            List<PurchaseHeader> list = new List<PurchaseHeader>();

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                SqlCommand cmd = new SqlCommand("sp_Purchase_GetAll", con);
                cmd.CommandType = CommandType.StoredProcedure;

                con.Open();
                SqlDataReader dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    list.Add(ReadHeader(dr));
                }
            }
            return View(list);
        }

        public IActionResult Details(int id)
        {
            var purchase = GetPurchase(id);
            if (purchase == null)
                return NotFound();

            return View(purchase);
        }

        public IActionResult Create()
        {
            var purchase = new PurchaseHeader();
            purchase.InvoiceDate = DateTime.Today;

            LoadDropdowns();
            return View("Form", purchase);
        }

        public IActionResult Edit(int id)
        {
            var purchase = GetPurchase(id);
            if (purchase == null)
                return NotFound();

            if (purchase.IsPosted)
            {
                TempData["Error"] = "Posted purchase cannot be edited";
                return RedirectToAction("Details", new { id });
            }

            LoadDropdowns();
            return View("Form", purchase);
        }

        // button = "save" -> save as draft
        // button = "post" -> save and then post to stock
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Save(PurchaseHeader purchase, string button)
        {
            // ignore empty rows
            purchase.Details = purchase.Details.Where(d => d.ProductId > 0).ToList();

            // line rules are checked in sp_Purchase_Save
            foreach (var key in ModelState.Keys.Where(k => k.StartsWith("Details")).ToList())
            {
                ModelState.Remove(key);
            }

            if (purchase.Details.Count == 0)
                ModelState.AddModelError("", "Add at least one product");

            if (!ModelState.IsValid)
            {
                LoadDropdowns();
                return View("Form", purchase);
            }

            // all lines are sent to the procedure as one table (PurchaseDetailType).
            // columns must be in the same order as the table type.
            DataTable lines = new DataTable();
            lines.Columns.Add("RowNo", typeof(int));
            lines.Columns.Add("ProductId", typeof(int));
            lines.Columns.Add("LotNo", typeof(string));
            lines.Columns.Add("ExpiryDate", typeof(DateTime));
            lines.Columns.Add("Quantity", typeof(decimal));
            lines.Columns.Add("Rate", typeof(decimal));
            lines.Columns.Add("DiscountPercent", typeof(decimal));

            int rowNo = 1;
            foreach (PurchaseDetail d in purchase.Details)
            {
                lines.Rows.Add(rowNo, d.ProductId, d.LotNo ?? "", (object?)d.ExpiryDate ?? DBNull.Value, d.Quantity, d.Rate, d.DiscountPercent);
                rowNo++;
            }

            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    SqlCommand cmd = new SqlCommand("sp_Purchase_Save", con);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@PurchaseId", purchase.PurchaseId);
                    cmd.Parameters.AddWithValue("@SupplierId", purchase.SupplierId);
                    cmd.Parameters.AddWithValue("@InvoiceNo", purchase.InvoiceNo);
                    cmd.Parameters.AddWithValue("@InvoiceDate", purchase.InvoiceDate);

                    SqlParameter details = cmd.Parameters.AddWithValue("@Details", lines);
                    details.SqlDbType = SqlDbType.Structured;
                    details.TypeName = "dbo.PurchaseDetailType";

                    con.Open();
                    // procedure returns the PurchaseId
                    purchase.PurchaseId = Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
            catch (SqlException ex)
            {
                ModelState.AddModelError("", ex.Message);
                LoadDropdowns();
                return View("Form", purchase);
            }

            if (button == "post")
            {
                return Post(purchase.PurchaseId);
            }

            TempData["Message"] = "Purchase saved as draft";
            return RedirectToAction("Details", new { id = purchase.PurchaseId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Post(int id)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    SqlCommand cmd = new SqlCommand("sp_Purchase_Post", con);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@PurchaseId", id);

                    con.Open();
                    cmd.ExecuteNonQuery();
                }
                TempData["Message"] = "Purchase posted and stock updated";
            }
            catch (SqlException ex)
            {
                TempData["Error"] = "Not posted. " + ex.Message;
            }
            return RedirectToAction("Details", new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    SqlCommand cmd = new SqlCommand("sp_Purchase_Delete", con);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@PurchaseId", id);

                    con.Open();
                    cmd.ExecuteNonQuery();
                }
                TempData["Message"] = "Draft purchase deleted";
                return RedirectToAction("Index");
            }
            catch (SqlException ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction("Details", new { id });
            }
        }

        // sp_Purchase_GetById returns 2 results: header, then lines
        private PurchaseHeader? GetPurchase(int id)
        {
            PurchaseHeader? purchase = null;

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                SqlCommand cmd = new SqlCommand("sp_Purchase_GetById", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@PurchaseId", id);

                con.Open();
                SqlDataReader dr = cmd.ExecuteReader();

                // first result - header
                if (dr.Read())
                {
                    purchase = ReadHeader(dr);
                }

                if (purchase == null)
                    return null;

                // second result - lines
                dr.NextResult();
                while (dr.Read())
                {
                    PurchaseDetail d = new PurchaseDetail();
                    d.DetailId = Convert.ToInt32(dr["DetailId"]);
                    d.PurchaseId = Convert.ToInt32(dr["PurchaseId"]);
                    d.ProductId = Convert.ToInt32(dr["ProductId"]);
                    d.ProductName = dr["ProductName"].ToString();
                    d.LotNo = dr["LotNo"].ToString();
                    d.ExpiryDate = dr["ExpiryDate"] == DBNull.Value ? null : Convert.ToDateTime(dr["ExpiryDate"]);
                    d.Quantity = Convert.ToDecimal(dr["Quantity"]);
                    d.Rate = Convert.ToDecimal(dr["Rate"]);
                    d.DiscountPercent = Convert.ToDecimal(dr["DiscountPercent"]);
                    d.GrossAmount = Convert.ToDecimal(dr["GrossAmount"]);
                    d.DiscountAmount = Convert.ToDecimal(dr["DiscountAmount"]);
                    d.TaxableAmount = Convert.ToDecimal(dr["TaxableAmount"]);
                    d.TaxId = dr["TaxId"] == DBNull.Value ? null : Convert.ToInt32(dr["TaxId"]);
                    d.TaxName = dr["TaxName"] == DBNull.Value ? null : dr["TaxName"].ToString();
                    d.TaxType = dr["TaxType"] == DBNull.Value ? null : dr["TaxType"].ToString();
                    d.TaxRate = Convert.ToDecimal(dr["TaxRate"]);
                    d.AdditionalRate = Convert.ToDecimal(dr["AdditionalRate"]);
                    d.TaxAmount = Convert.ToDecimal(dr["TaxAmount"]);
                    d.NetAmount = Convert.ToDecimal(dr["NetAmount"]);
                    purchase.Details.Add(d);
                }
            }
            return purchase;
        }

        private PurchaseHeader ReadHeader(SqlDataReader dr)
        {
            PurchaseHeader p = new PurchaseHeader();
            p.PurchaseId = Convert.ToInt32(dr["PurchaseId"]);
            p.SupplierId = Convert.ToInt32(dr["SupplierId"]);
            p.SupplierName = dr["SupplierName"].ToString();
            p.InvoiceNo = dr["InvoiceNo"].ToString()!;
            p.InvoiceDate = Convert.ToDateTime(dr["InvoiceDate"]);
            p.GrossAmount = Convert.ToDecimal(dr["GrossAmount"]);
            p.DiscountAmount = Convert.ToDecimal(dr["DiscountAmount"]);
            p.TaxAmount = Convert.ToDecimal(dr["TaxAmount"]);
            p.NetAmount = Convert.ToDecimal(dr["NetAmount"]);
            p.Status = dr["Status"].ToString()!;
            p.CreatedOn = Convert.ToDateTime(dr["CreatedOn"]);
            p.PostedOn = dr["PostedOn"] == DBNull.Value ? null : Convert.ToDateTime(dr["PostedOn"]);
            return p;
        }

        // active suppliers and active products (with tax) for the form
        private void LoadDropdowns()
        {
            List<Supplier> suppliers = new List<Supplier>();
            List<Product> products = new List<Product>();

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                con.Open();

                SqlCommand cmd = new SqlCommand("sp_Supplier_GetAll", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@OnlyActive", true);
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        Supplier s = new Supplier();
                        s.SupplierId = Convert.ToInt32(dr["SupplierId"]);
                        s.SupplierName = dr["SupplierName"].ToString()!;
                        suppliers.Add(s);
                    }
                }

                SqlCommand cmd2 = new SqlCommand("sp_Product_GetAll", con);
                cmd2.CommandType = CommandType.StoredProcedure;
                cmd2.Parameters.AddWithValue("@OnlyActive", true);
                using (SqlDataReader dr = cmd2.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        Product p = new Product();
                        p.ProductId = Convert.ToInt32(dr["ProductId"]);
                        p.ProductCode = dr["ProductCode"].ToString()!;
                        p.ProductName = dr["ProductName"].ToString()!;
                        p.CostPrice = Convert.ToDecimal(dr["CostPrice"]);
                        p.HasExpiry = Convert.ToBoolean(dr["HasExpiry"]);

                        if (dr["TaxId"] != DBNull.Value)
                        {
                            p.TaxType = dr["TaxType"].ToString();
                            p.TaxRate = Convert.ToDecimal(dr["TaxRate"]);
                            p.AdditionalRate = Convert.ToDecimal(dr["AdditionalRate"]);
                        }
                        products.Add(p);
                    }
                }
            }

            ViewBag.Suppliers = new SelectList(suppliers, "SupplierId", "SupplierName");
            ViewBag.Products = products;
        }
    }
}
